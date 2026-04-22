using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Channels.Mt5;

/// <summary>
/// Manages the Python MT5 sidecar process lifecycle: start, stop, monitor, auto-restart.
/// </summary>
public sealed partial class Mt5SidecarManager : IAsyncDisposable
{
    private readonly Mt5Config _config;
    private readonly ILogger<Mt5SidecarManager> _logger;

    private Process? _process;
    private CancellationTokenSource? _monitorCts;
    private TaskCompletionSource? _readyTcs;
    private int _restartCount;

    public Mt5SidecarStatus Status { get; private set; } = Mt5SidecarStatus.Stopped;
    public string? LastError { get; private set; }

    public event EventHandler<Mt5SidecarStatus>? StatusChanged;

    public Mt5SidecarManager(Mt5Config config, ILogger<Mt5SidecarManager> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Status == Mt5SidecarStatus.Running)
        {
            _logger.LogWarning("MT5 sidecar is already running");
            return;
        }

        await LaunchProcessAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _monitorCts?.Cancel();

        if (_process is { HasExited: false })
        {
            _logger.LogInformation("Stopping MT5 sidecar...");
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping MT5 sidecar process");
            }
        }

        CleanupProcess();
        SetStatus(Mt5SidecarStatus.Stopped);
    }

    /// <summary>
    /// Waits until the sidecar prints its READY line, or the timeout expires.
    /// </summary>
    public async Task WaitForReadyAsync(CancellationToken cancellationToken = default)
    {
        if (Status == Mt5SidecarStatus.Running)
            return;

        if (_readyTcs is null)
            return;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            await _readyTcs.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Timed out waiting for MT5 sidecar to become ready");
        }
    }

    private Task LaunchProcessAsync(CancellationToken cancellationToken)
    {
        SetStatus(Mt5SidecarStatus.Starting);
        _restartCount = 0;
        _readyTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var args = $"\"{_config.SidecarPath}\" --rep-port {_config.RepPort} --pub-port {_config.PubPort}";
        _logger.LogInformation("Starting MT5 sidecar: {Python} {Args}", _config.PythonPath, args);

        var psi = new ProcessStartInfo
        {
            FileName = _config.PythonPath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.Start();

        _monitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _ = Task.Run(() => ReadOutputAsync(_process.StandardOutput, _monitorCts.Token), _monitorCts.Token);
        _ = Task.Run(() => ReadOutputAsync(_process.StandardError, _monitorCts.Token), _monitorCts.Token);
        _ = Task.Run(() => MonitorProcessAsync(_monitorCts.Token), _monitorCts.Token);

        return Task.CompletedTask;
    }

    private async Task ReadOutputAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                    break;

                _logger.LogDebug("[mt5-sidecar] {Line}", line);
                ParseReadyLine(line);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading MT5 sidecar output");
        }
    }

    internal void ParseReadyLine(string line)
    {
        var match = ReadyPattern().Match(line);
        if (match.Success)
        {
            _logger.LogInformation("MT5 sidecar is ready (REP={RepPort} PUB={PubPort})",
                match.Groups[1].Value, match.Groups[2].Value);
            SetStatus(Mt5SidecarStatus.Running);
            _readyTcs?.TrySetResult();
        }
    }

    private async Task MonitorProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_process is null) return;

            await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
                return;

            _logger.LogWarning("MT5 sidecar exited with code {ExitCode}", _process.ExitCode);

            if (_restartCount < _config.MaxRestartAttempts)
            {
                _restartCount++;
                var delay = _config.RestartDelaySeconds * _restartCount;
                _logger.LogInformation("Restarting MT5 sidecar in {Delay}s (attempt {Attempt}/{Max})",
                    delay, _restartCount, _config.MaxRestartAttempts);

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);

                CleanupProcess();
                await LaunchProcessAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                SetStatus(Mt5SidecarStatus.Error, $"Process exited after {_config.MaxRestartAttempts} restart attempts");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private void SetStatus(Mt5SidecarStatus status, string? error = null)
    {
        Status = status;
        LastError = error;
        StatusChanged?.Invoke(this, status);
    }

    private void CleanupProcess()
    {
        _process?.Dispose();
        _process = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _monitorCts?.Dispose();
    }

    [GeneratedRegex(@"READY rep=(\d+) pub=(\d+)")]
    private static partial Regex ReadyPattern();
}
