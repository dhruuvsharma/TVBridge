using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Tunnel;

public sealed partial class CloudflaredManager : IAsyncDisposable
{
    private readonly TunnelConfig _config;
    private readonly CloudflaredDownloader _downloader;
    private readonly ILogger<CloudflaredManager> _logger;

    private Process? _process;
    private CancellationTokenSource? _monitorCts;
    private int _restartCount;

    public TunnelStatus Status { get; private set; } = TunnelStatus.Stopped;
    public string? TunnelUrl { get; private set; }
    public string? LastError { get; private set; }

    public event EventHandler<TunnelStatus>? StatusChanged;

    public CloudflaredManager(
        TunnelConfig config,
        CloudflaredDownloader downloader,
        ILogger<CloudflaredManager> logger)
    {
        _config = config;
        _downloader = downloader;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (Status == TunnelStatus.Running)
        {
            _logger.LogWarning("Tunnel is already running");
            return;
        }

        _restartCount = 0;

        SetStatus(TunnelStatus.Downloading);
        string exePath;
        try
        {
            exePath = await _downloader.EnsureAvailableAsync(_config.CloudflaredPath, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download cloudflared");
            SetStatus(TunnelStatus.Error, $"Download failed: {ex.Message}");
            return;
        }

        await LaunchProcessAsync(exePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _monitorCts?.Cancel();

        if (_process is { HasExited: false })
        {
            _logger.LogInformation("Stopping cloudflared tunnel...");
            try
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping cloudflared process");
            }
        }

        CleanupProcess();
        TunnelUrl = null;
        SetStatus(TunnelStatus.Stopped);
    }

    private Task LaunchProcessAsync(string exePath, CancellationToken cancellationToken)
    {
        SetStatus(TunnelStatus.Starting);

        if (!File.Exists(exePath))
        {
            SetStatus(TunnelStatus.Error, $"cloudflared.exe not found at {exePath}");
            return Task.CompletedTask;
        }

        var args = $"tunnel --url http://localhost:{_config.LocalPort} --no-autoupdate";

        _logger.LogInformation("Starting cloudflared: {ExePath} {Args}", exePath, args);

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        try
        {
            _process.Start();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch cloudflared process");
            SetStatus(TunnelStatus.Error, $"Failed to launch cloudflared: {ex.Message}");
            CleanupProcess();
            return Task.CompletedTask;
        }

        _monitorCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // cloudflared logs to stderr
        _ = Task.Run(() => ReadOutputAsync(_process.StandardError, _monitorCts.Token), _monitorCts.Token);
        _ = Task.Run(() => ReadOutputAsync(_process.StandardOutput, _monitorCts.Token), _monitorCts.Token);
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

                _logger.LogInformation("[cloudflared] {Line}", line);
                ParseTunnelUrl(line);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading cloudflared output");
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

            _logger.LogWarning("cloudflared process exited with code {ExitCode}", _process.ExitCode);

            if (_restartCount < _config.MaxRestartAttempts)
            {
                _restartCount++;
                var delay = _config.RestartDelaySeconds * _restartCount;
                _logger.LogInformation("Restarting cloudflared in {Delay}s (attempt {Attempt}/{Max})",
                    delay, _restartCount, _config.MaxRestartAttempts);

                await Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken).ConfigureAwait(false);

                var exePath = _config.CloudflaredPath;
                if (File.Exists(exePath))
                {
                    CleanupProcess();
                    await LaunchProcessAsync(exePath, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                SetStatus(TunnelStatus.Error, $"Process exited after {_config.MaxRestartAttempts} restart attempts");
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    internal void ParseTunnelUrl(string line)
    {
        // cloudflared outputs URL like: "https://xxx-yyy-zzz.trycloudflare.com"
        var match = TunnelUrlPattern().Match(line);
        if (match.Success)
        {
            TunnelUrl = match.Value;
            _logger.LogInformation("Tunnel URL: {Url}", TunnelUrl);
            SetStatus(TunnelStatus.Running);
        }
    }

    private void SetStatus(TunnelStatus status, string? error = null)
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

    [GeneratedRegex(@"https://[a-zA-Z0-9\-]+\.trycloudflare\.com")]
    private static partial Regex TunnelUrlPattern();
}
