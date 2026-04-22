using FluentAssertions;
using TVBridge.Channels.NinjaTrader;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.NinjaTrader;

public sealed class NtAtiCommandBuilderTests
{
    private static Signal MakeSignal(
        SignalAction action = SignalAction.Buy,
        OrderType orderType = OrderType.Market,
        decimal? lotSize = 1,
        decimal? entryPrice = null) => new()
    {
        AlertId = "a1",
        StrategyId = "s1",
        AccountTag = "demo",
        Symbol = "EURUSD",
        Action = action,
        OrderType = orderType,
        LotSize = lotSize,
        EntryPrice = entryPrice,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "s"
    };

    [Fact]
    public void BuildPlaceOrder_MarketBuy_CorrectFormat()
    {
        var signal = MakeSignal(SignalAction.Buy, OrderType.Market, lotSize: 2);

        var cmd = NtAtiCommandBuilder.BuildPlaceOrder(signal, "EUR/USD");

        cmd.Should().StartWith("PLACE;EUR/USD;;BUY;2;MARKET;0;0;GTC;;;;");
    }

    [Fact]
    public void BuildPlaceOrder_LimitSell_IncludesPrice()
    {
        var signal = MakeSignal(SignalAction.Sell, OrderType.Limit, lotSize: 5, entryPrice: 1.25000m);

        var cmd = NtAtiCommandBuilder.BuildPlaceOrder(signal, "GBP/USD");

        cmd.Should().Contain("SELL;5;LIMIT;1.25000;0;GTC");
    }

    [Fact]
    public void BuildPlaceOrder_StopBuy_IncludesStopPrice()
    {
        var signal = MakeSignal(SignalAction.Buy, OrderType.Stop, entryPrice: 150.500m);

        var cmd = NtAtiCommandBuilder.BuildPlaceOrder(signal, "USD/JPY");

        cmd.Should().Contain("BUY;1;STOPMARKET;0;150.50000;GTC");
    }

    [Fact]
    public void BuildPlaceOrder_NullLotSize_DefaultsTo1()
    {
        var signal = MakeSignal(lotSize: null);

        var cmd = NtAtiCommandBuilder.BuildPlaceOrder(signal, "EUR/USD");

        cmd.Should().Contain(";BUY;1;MARKET;");
    }

    [Fact]
    public void BuildClosePosition_CorrectFormat()
    {
        var cmd = NtAtiCommandBuilder.BuildClosePosition("EUR/USD", "Sim101");

        cmd.Should().Be("CLOSEPOSITION;Sim101;EUR/USD");
    }

    [Fact]
    public void BuildClosePosition_EmptyAccount()
    {
        var cmd = NtAtiCommandBuilder.BuildClosePosition("NQ 09-25");

        cmd.Should().Be("CLOSEPOSITION;;NQ 09-25");
    }

    [Fact]
    public void BuildPlaceOrder_CloseAction_Throws()
    {
        var signal = MakeSignal(SignalAction.Close);

        var act = () => NtAtiCommandBuilder.BuildPlaceOrder(signal, "EUR/USD");

        act.Should().Throw<ArgumentException>();
    }
}
