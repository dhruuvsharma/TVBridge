namespace TVBridge.Channels.NinjaTrader;

/// <summary>
/// Interface for the NinjaTrader 8 ATI (Automated Trading Interface) TCP client.
/// </summary>
public interface IAtiClient : IDisposable
{
    Task<AtiResponse> SendCommandAsync(string command, CancellationToken cancellationToken = default);
    Task<bool> ConnectAsync(CancellationToken cancellationToken = default);
    void Disconnect();
    bool IsConnected { get; }
}

/// <summary>
/// Response from an ATI command.
/// </summary>
public sealed record AtiResponse(bool Success, string Message);
