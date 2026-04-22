using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NetMQ;
using NetMQ.Sockets;
using TVBridge.Channels.Mt5;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.Mt5;

public sealed class Mt5ZmqClientTests : IDisposable
{
    private readonly ResponseSocket _fakeServer;
    private readonly Mt5ZmqClient _client;
    private readonly int _repPort;

    public Mt5ZmqClientTests()
    {
        _repPort = FindFreePort();
        _fakeServer = new ResponseSocket();
        _fakeServer.Bind($"tcp://127.0.0.1:{_repPort}");

        var config = new Mt5Config { RepPort = _repPort, CommandTimeoutMs = 5000 };
        _client = new Mt5ZmqClient(config, NullLogger<Mt5ZmqClient>.Instance);
    }

    [Fact]
    public async Task SendCommandAsync_PingPong_ReturnsSuccess()
    {
        // Start a fake server that responds to any request
        _ = Task.Run(() =>
        {
            var request = _fakeServer.ReceiveFrameString();
            var response = JsonSerializer.Serialize(new { success = true, data = "pong" });
            _fakeServer.SendFrame(response);
        });

        var result = await _client.SendCommandAsync("ping");

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SendCommandAsync_Timeout_ReturnsError()
    {
        var config = new Mt5Config { RepPort = FindFreePort(), CommandTimeoutMs = 500 };
        using var client = new Mt5ZmqClient(config, NullLogger<Mt5ZmqClient>.Instance);

        // No server listening, so it will timeout
        // We need a server binding but not responding
        using var slowServer = new ResponseSocket();
        slowServer.Bind($"tcp://127.0.0.1:{config.RepPort}");

        var result = await client.SendCommandAsync("ping");

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("timed out");
    }

    [Fact]
    public async Task SendCommandAsync_SerializesCommandCorrectly()
    {
        string? receivedJson = null;
        _ = Task.Run(() =>
        {
            receivedJson = _fakeServer.ReceiveFrameString();
            var response = JsonSerializer.Serialize(new { success = true, data = (object?)null });
            _fakeServer.SendFrame(response);
        });

        await _client.SendCommandAsync("get_balance");

        // Wait briefly for the background task
        await Task.Delay(100);

        receivedJson.Should().NotBeNull();
        var parsed = JsonDocument.Parse(receivedJson!);
        parsed.RootElement.GetProperty("command").GetString().Should().Be("get_balance");
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var config = new Mt5Config { RepPort = FindFreePort() };
        var client = new Mt5ZmqClient(config, NullLogger<Mt5ZmqClient>.Instance);

        var act = () => client.Dispose();

        act.Should().NotThrow();
    }

    private static int FindFreePort()
    {
        using var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        socket.Start();
        var port = ((System.Net.IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();
        return port;
    }

    public void Dispose()
    {
        _client.Dispose();
        _fakeServer.Dispose();
    }
}
