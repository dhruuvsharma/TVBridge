using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Channels.Mt5;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.Mt5;

public sealed class Mt5ChannelTests
{
    private static readonly Signal TestSignal = new()
    {
        AlertId = "test-1",
        StrategyId = "strat-1",
        AccountTag = "demo",
        Symbol = "EURUSD",
        Action = SignalAction.Buy,
        OrderType = OrderType.Market,
        LotSize = 0.1m,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "test-secret"
    };

    [Fact]
    public async Task SendAsync_DryRun_ReturnsDescription()
    {
        var client = new FakeMt5Client();
        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: true);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("[DRY RUN]");
        result.Message.Should().Contain("EURUSD");
    }

    [Fact]
    public async Task SendAsync_Buy_SendsPlaceOrderCommand()
    {
        var orderResult = new Mt5OrderResult(123, "EURUSD", "Buy", 0.1m, 1.1234m);
        var client = new FakeMt5Client();
        client.SetResponse("place_order", new Mt5Response(true, JsonSerializer.SerializeToElement(orderResult)));
        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: false);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("ticket=123");
        client.LastCommand.Should().Be("place_order");
    }

    [Fact]
    public async Task SendAsync_Sell_SendsPlaceOrderCommand()
    {
        var signal = TestSignal with { Action = SignalAction.Sell };
        var orderResult = new Mt5OrderResult(456, "EURUSD", "Sell", 0.1m, 1.1230m);
        var client = new FakeMt5Client();
        client.SetResponse("place_order", new Mt5Response(true, JsonSerializer.SerializeToElement(orderResult)));
        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);

        var result = await channel.SendAsync(signal, dryRun: false);

        result.Success.Should().BeTrue();
        client.LastCommand.Should().Be("place_order");
    }

    [Fact]
    public async Task SendAsync_Close_LooksUpPositionsThenCloses()
    {
        var positions = new List<Mt5Position>
        {
            new(1, "EURUSD", "Buy", 0.1m, 1.1m, 1.2m, 100m, Time: DateTimeOffset.UtcNow.AddHours(-1)),
            new(2, "EURUSD", "Buy", 0.2m, 1.15m, 1.2m, 50m, Time: DateTimeOffset.UtcNow)
        };

        var client = new FakeMt5Client();
        client.SetResponse("get_positions", new Mt5Response(true, JsonSerializer.SerializeToElement(positions)));
        var closeResult = new Mt5OrderResult(1, "EURUSD", "Close", 0.1m, 1.2m);
        client.SetResponse("close", new Mt5Response(true, JsonSerializer.SerializeToElement(closeResult)));

        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);
        var signal = TestSignal with { Action = SignalAction.Close };

        var result = await channel.SendAsync(signal, dryRun: false);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("ticket=1"); // FIFO: oldest position
    }

    [Fact]
    public async Task SendAsync_Close_NoPositions_ReturnsFail()
    {
        var client = new FakeMt5Client();
        client.SetResponse("get_positions", new Mt5Response(true, JsonSerializer.SerializeToElement(new List<Mt5Position>())));

        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);
        var signal = TestSignal with { Action = SignalAction.Close };

        var result = await channel.SendAsync(signal, dryRun: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No open positions");
    }

    [Fact]
    public async Task SendAsync_PlaceOrder_Failure_ReturnsFail()
    {
        var client = new FakeMt5Client();
        client.SetResponse("place_order", new Mt5Response(false, Error: "Insufficient margin"));

        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Insufficient margin");
    }

    [Fact]
    public async Task ValidateConfigAsync_Ping_Success()
    {
        var client = new FakeMt5Client();
        client.SetResponse("ping", new Mt5Response(true, JsonSerializer.SerializeToElement("pong")));

        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateConfigAsync_Ping_Failure()
    {
        var client = new FakeMt5Client();
        client.SetResponse("ping", new Mt5Response(false, Error: "timeout"));

        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_Modify_LooksUpPositionsThenModifies()
    {
        var positions = new List<Mt5Position>
        {
            new(10, "EURUSD", "Buy", 0.1m, 1.1m, 1.2m, 100m, Time: DateTimeOffset.UtcNow)
        };

        var client = new FakeMt5Client();
        client.SetResponse("get_positions", new Mt5Response(true, JsonSerializer.SerializeToElement(positions)));
        var modifyResult = new Mt5OrderResult(10, "EURUSD", "Modify", 0.1m, 1.1m);
        client.SetResponse("modify", new Mt5Response(true, JsonSerializer.SerializeToElement(modifyResult)));

        var channel = new Mt5Channel(client, NullLogger<Mt5Channel>.Instance);
        var signal = TestSignal with { Action = SignalAction.Modify, StopLoss = 1.05m, TakeProfit = 1.25m };

        var result = await channel.SendAsync(signal, dryRun: false);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("ticket=10");
    }
}

/// <summary>
/// Fake IMt5Client for unit testing without ZeroMQ.
/// </summary>
internal sealed class FakeMt5Client : IMt5Client
{
    private readonly Dictionary<string, Mt5Response> _responses = new();

    public string? LastCommand { get; private set; }
    public object? LastParams { get; private set; }

    public void SetResponse(string command, Mt5Response response)
    {
        _responses[command] = response;
    }

    public Task<Mt5Response> SendCommandAsync(string command, object? parameters = null, CancellationToken cancellationToken = default)
    {
        LastCommand = command;
        LastParams = parameters;

        if (_responses.TryGetValue(command, out var response))
            return Task.FromResult(response);

        return Task.FromResult(new Mt5Response(false, Error: $"No fake response for '{command}'"));
    }

    public void StartAccountStateStream(Action<Mt5AccountState> onState, CancellationToken cancellationToken) { }
    public void StopAccountStateStream() { }
    public void Dispose() { }
}
