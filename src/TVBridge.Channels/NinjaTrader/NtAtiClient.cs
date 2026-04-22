using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TVBridge.Channels.NinjaTrader;

/// <summary>
/// TCP client for NinjaTrader 8 ATI (Automated Trading Interface).
/// ATI protocol: send a command line, receive a response line.
/// Default port 36973.
/// </summary>
public sealed class NtAtiClient : IAtiClient
{
    private readonly NinjaTraderConfig _config;
    private readonly ILogger<NtAtiClient> _logger;

    private TcpClient? _tcp;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private bool _disposed;

    public bool IsConnected => _tcp?.Connected == true;

    public NtAtiClient(NinjaTraderConfig config, ILogger<NtAtiClient> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        try
        {
            Disconnect();
            _tcp = new TcpClient();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_config.ConnectTimeoutMs);

            await _tcp.ConnectAsync(_config.Host, _config.Port, cts.Token).ConfigureAwait(false);

            var stream = _tcp.GetStream();
            _reader = new StreamReader(stream, Encoding.UTF8);
            _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            _logger.LogInformation("Connected to NinjaTrader ATI at {Host}:{Port}", _config.Host, _config.Port);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to NinjaTrader ATI at {Host}:{Port}", _config.Host, _config.Port);
            Disconnect();
            return false;
        }
    }

    public async Task<AtiResponse> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_writer is null || _reader is null || !IsConnected)
            return new AtiResponse(false, "Not connected to NinjaTrader ATI");

        try
        {
            _logger.LogDebug("ATI TX: {Command}", command);

            await _writer.WriteLineAsync(command.AsMemory(), cancellationToken).ConfigureAwait(false);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_config.CommandTimeoutMs);

            var response = await _reader.ReadLineAsync(cts.Token).ConfigureAwait(false);

            _logger.LogDebug("ATI RX: {Response}", response);

            if (response is null)
            {
                Disconnect();
                return new AtiResponse(false, "Connection closed by NinjaTrader");
            }

            // ATI responses: "0" = success, anything else = error message
            return response == "0"
                ? new AtiResponse(true, "OK")
                : new AtiResponse(false, response);
        }
        catch (OperationCanceledException)
        {
            return new AtiResponse(false, "Command timed out");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ATI command error");
            Disconnect();
            return new AtiResponse(false, ex.Message);
        }
    }

    public void Disconnect()
    {
        _writer?.Dispose();
        _writer = null;
        _reader?.Dispose();
        _reader = null;
        _tcp?.Dispose();
        _tcp = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Disconnect();
    }
}
