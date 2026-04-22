using TVBridge.Channels.NinjaTrader;

namespace TVBridge.Channels.Tests.NinjaTrader;

/// <summary>
/// Fake IAtiClient for unit testing without a real TCP connection.
/// </summary>
internal sealed class FakeAtiClient : IAtiClient
{
    private readonly Queue<AtiResponse> _responses = new();
    private bool _connected;

    public string? LastCommand { get; private set; }
    public List<string> AllCommands { get; } = [];
    public bool IsConnected => _connected;

    public void SetConnected(bool connected) => _connected = connected;

    public void EnqueueResponse(AtiResponse response) => _responses.Enqueue(response);

    public Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
    {
        _connected = true;
        return Task.FromResult(true);
    }

    public void Disconnect() => _connected = false;

    public Task<AtiResponse> SendCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        LastCommand = command;
        AllCommands.Add(command);

        var response = _responses.Count > 0
            ? _responses.Dequeue()
            : new AtiResponse(true, "OK");

        return Task.FromResult(response);
    }

    public void Dispose() => _connected = false;
}
