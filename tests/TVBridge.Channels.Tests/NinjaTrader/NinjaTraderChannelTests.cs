using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Channels.NinjaTrader;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.NinjaTrader;

public sealed class NinjaTraderChannelTests
{
    private static readonly NinjaTraderConfig Config = new()
    {
        SymbolMap = new Dictionary<string, string> { ["EURUSD"] = "EUR/USD", ["GBPUSD"] = "GBP/USD" }
    };

    private static Signal MakeSignal(
        SignalAction action = SignalAction.Buy,
        string symbol = "EURUSD") => new()
    {
        AlertId = "nt-1",
        StrategyId = "strat",
        AccountTag = "demo",
        Symbol = symbol,
        Action = action,
        OrderType = OrderType.Market,
        LotSize = 1,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "s"
    };

    [Fact]
    public async Task SendAsync_DryRun_ReturnsDescription()
    {
        var client = new FakeAtiClient();
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(), dryRun: true);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("[DRY RUN]");
        result.Message.Should().Contain("EUR/USD");
    }

    [Fact]
    public async Task SendAsync_Buy_SendsPlaceCommand()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(SignalAction.Buy), dryRun: false);

        result.Success.Should().BeTrue();
        client.LastCommand.Should().StartWith("PLACE;EUR/USD;");
        client.LastCommand.Should().Contain("BUY");
    }

    [Fact]
    public async Task SendAsync_Sell_SendsPlaceCommand()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(SignalAction.Sell), dryRun: false);

        result.Success.Should().BeTrue();
        client.LastCommand.Should().Contain("SELL");
    }

    [Fact]
    public async Task SendAsync_Close_SendsClosePositionCommand()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(SignalAction.Close), dryRun: false);

        result.Success.Should().BeTrue();
        client.LastCommand.Should().StartWith("CLOSEPOSITION;");
        client.LastCommand.Should().Contain("EUR/USD");
    }

    [Fact]
    public async Task SendAsync_Modify_ReturnsFail()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(SignalAction.Modify), dryRun: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("does not support");
    }

    [Fact]
    public async Task SendAsync_AtiError_ReturnsFail()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        client.EnqueueResponse(new AtiResponse(false, "Order rejected"));
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(), dryRun: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Order rejected");
    }

    [Fact]
    public async Task SendAsync_NotConnected_AutoConnects()
    {
        var client = new FakeAtiClient();
        client.SetConnected(false);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var result = await channel.SendAsync(MakeSignal(), dryRun: false);

        result.Success.Should().BeTrue();
        client.IsConnected.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_MapsSymbol()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        await channel.SendAsync(MakeSignal(symbol: "GBPUSD"), dryRun: false);

        client.LastCommand.Should().Contain("GBP/USD");
    }

    [Fact]
    public async Task SendAsync_UnmappedSymbol_UsesOriginal()
    {
        var client = new FakeAtiClient();
        client.SetConnected(true);
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        await channel.SendAsync(MakeSignal(symbol: "XAUUSD"), dryRun: false);

        client.LastCommand.Should().Contain("XAUUSD");
    }

    [Fact]
    public async Task ValidateConfigAsync_ReturnsConnectResult()
    {
        var client = new FakeAtiClient();
        var channel = new NinjaTraderChannel(client, Config, NullLogger<NinjaTraderChannel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeTrue();
    }
}
