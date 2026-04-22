using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;
using TVBridge.Core;

namespace TVBridge.Channels.Mt5;

/// <summary>
/// ZeroMQ client for communicating with the MT5 Python sidecar.
/// REQ socket for commands, SUB socket for account state stream.
/// </summary>
public sealed class Mt5ZmqClient : IMt5Client
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly Mt5Config _config;
    private readonly ILogger<Mt5ZmqClient> _logger;
    private readonly object _reqLock = new();

    private RequestSocket? _reqSocket;
    private CancellationTokenSource? _subCts;
    private Task? _subTask;
    private bool _disposed;

    public Mt5ZmqClient(Mt5Config config, ILogger<Mt5ZmqClient> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<Mt5Response> SendCommandAsync(
        string command,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return Task.Run(() =>
        {
            lock (_reqLock)
            {
                var socket = EnsureReqSocket();
                var request = new Mt5Request(command, parameters);
                var json = JsonSerializer.Serialize(request, JsonOptions);

                _logger.LogDebug("ZMQ REQ: {Json}", json);

                socket.SendFrame(json);

                var timeout = TimeSpan.FromMilliseconds(_config.CommandTimeoutMs);
                if (!socket.TryReceiveFrameString(timeout, out var reply))
                {
                    // Socket is in a bad state after timeout; recreate it
                    DisposeReqSocket();
                    return new Mt5Response(false, Error: $"Command '{command}' timed out after {_config.CommandTimeoutMs}ms");
                }

                _logger.LogDebug("ZMQ REP: {Reply}", reply);

                var response = JsonSerializer.Deserialize<Mt5Response>(reply!, JsonOptions);
                return response ?? new Mt5Response(false, Error: "Failed to deserialize response");
            }
        }, cancellationToken);
    }

    public void StartAccountStateStream(Action<Mt5AccountState> onState, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        StopAccountStateStream();

        _subCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _subCts.Token;

        _subTask = Task.Run(() =>
        {
            using var subSocket = new SubscriberSocket();
            subSocket.Connect($"tcp://127.0.0.1:{_config.PubPort}");
            subSocket.Subscribe("account_state");

            _logger.LogInformation("Subscribed to account state on port {Port}", _config.PubPort);

            while (!token.IsCancellationRequested)
            {
                if (!subSocket.TryReceiveFrameString(TimeSpan.FromSeconds(2), out var msg))
                    continue;

                if (msg is null || !msg.StartsWith("account_state "))
                    continue;

                try
                {
                    var json = msg["account_state ".Length..];
                    var state = JsonSerializer.Deserialize<Mt5AccountState>(json, JsonOptions);
                    if (state is not null)
                        onState(state);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse account state message");
                }
            }
        }, token);
    }

    public void StopAccountStateStream()
    {
        _subCts?.Cancel();
        try { _subTask?.Wait(TimeSpan.FromSeconds(3)); }
        catch (AggregateException) { /* expected on cancellation */ }
        _subCts?.Dispose();
        _subCts = null;
        _subTask = null;
    }

    private RequestSocket EnsureReqSocket()
    {
        if (_reqSocket is null)
        {
            _reqSocket = new RequestSocket();
            _reqSocket.Connect($"tcp://127.0.0.1:{_config.RepPort}");
            _logger.LogInformation("Connected REQ socket to port {Port}", _config.RepPort);
        }
        return _reqSocket;
    }

    private void DisposeReqSocket()
    {
        _reqSocket?.Dispose();
        _reqSocket = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        StopAccountStateStream();
        DisposeReqSocket();
    }
}
