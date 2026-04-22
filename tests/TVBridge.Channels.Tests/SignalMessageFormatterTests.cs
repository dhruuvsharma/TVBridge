using FluentAssertions;
using TVBridge.Channels;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests;

public sealed class SignalMessageFormatterTests
{
    private static readonly Signal TestSignal = new()
    {
        AlertId = "alert-1",
        StrategyId = "macd-cross",
        AccountTag = "demo",
        Symbol = "EURUSD",
        Action = SignalAction.Buy,
        OrderType = OrderType.Market,
        LotSize = 0.10m,
        EntryPrice = 1.12345m,
        StopLoss = 1.11000m,
        TakeProfit = 1.14000m,
        Timeframe = "1H",
        Timestamp = new DateTimeOffset(2025, 6, 15, 14, 30, 0, TimeSpan.Zero),
        Secret = "s",
        Comment = "test trade"
    };

    [Fact]
    public void Format_ReplacesAllPlaceholders()
    {
        var template = "{Action} {Symbol} {OrderType} {LotSize} {EntryPrice} {StopLoss} {TakeProfit} {StrategyId} {AccountTag} {Timeframe} {Timestamp} {Comment} {AlertId}";

        var result = SignalMessageFormatter.Format(TestSignal, template);

        result.Should().Contain("Buy");
        result.Should().Contain("EURUSD");
        result.Should().Contain("Market");
        result.Should().Contain("0.10");
        result.Should().Contain("1.12345");
        result.Should().Contain("1.11000");
        result.Should().Contain("1.14000");
        result.Should().Contain("macd-cross");
        result.Should().Contain("demo");
        result.Should().Contain("1H");
        result.Should().Contain("2025-06-15 14:30:00");
        result.Should().Contain("test trade");
        result.Should().Contain("alert-1");
    }

    [Fact]
    public void Format_NullOptionals_ShowsDash()
    {
        var signal = TestSignal with { LotSize = null, StopLoss = null, TakeProfit = null, EntryPrice = null, Comment = null };
        var template = "{LotSize} {EntryPrice} {StopLoss} {TakeProfit} {Comment}";

        var result = SignalMessageFormatter.Format(signal, template);

        result.Should().Contain("—"); // em dash for null numerics
        result.Should().Contain("Market"); // null EntryPrice shows "Market"
    }

    [Fact]
    public void Format_CustomTemplate_Works()
    {
        var template = "Signal: {Action} on {Symbol}";

        var result = SignalMessageFormatter.Format(TestSignal, template);

        result.Should().Be("Signal: Buy on EURUSD");
    }

    [Fact]
    public void Format_EmptyTemplate_ReturnsEmpty()
    {
        var result = SignalMessageFormatter.Format(TestSignal, "");

        result.Should().BeEmpty();
    }
}
