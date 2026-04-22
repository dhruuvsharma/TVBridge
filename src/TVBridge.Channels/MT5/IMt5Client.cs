namespace TVBridge.Channels.Mt5;

/// <summary>
/// Interface for communicating with the MT5 Python sidecar over ZeroMQ.
/// </summary>
public interface IMt5Client : IDisposable
{
    Task<Mt5Response> SendCommandAsync(string command, object? parameters = null, CancellationToken cancellationToken = default);
    void StartAccountStateStream(Action<Mt5AccountState> onState, CancellationToken cancellationToken);
    void StopAccountStateStream();
}
