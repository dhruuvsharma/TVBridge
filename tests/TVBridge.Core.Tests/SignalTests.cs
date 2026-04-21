using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class SignalTests
{
    private static Signal CreateTestSignal() => new()
    {
        AlertId = "550e8400-e29b-41d4-a716-446655440000",
        StrategyId = "MA_CROSS",
        AccountTag = "live-1",
        Symbol = "EURUSD",
        Action = SignalAction.Buy,
        OrderType = OrderType.Market,
        EntryPrice = null,
        StopLoss = 1.0850m,
        TakeProfit = 1.1050m,
        LotSize = 0.1m,
        RiskPercent = null,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.Parse("2026-04-22T12:00:00Z"),
        Comment = "Test signal",
        Secret = "webhook-secret"
    };

    [Fact]
    public void Signal_AllRequiredProperties_SetCorrectly()
    {
        var signal = CreateTestSignal();

        signal.AlertId.Should().NotBeNullOrEmpty();
        signal.StrategyId.Should().Be("MA_CROSS");
        signal.AccountTag.Should().Be("live-1");
        signal.Symbol.Should().Be("EURUSD");
        signal.Action.Should().Be(SignalAction.Buy);
        signal.OrderType.Should().Be(OrderType.Market);
    }

    [Fact]
    public void Signal_NullableProperties_DefaultToNull()
    {
        var signal = new Signal
        {
            AlertId = "test",
            StrategyId = "s",
            AccountTag = "a",
            Symbol = "BTCUSD",
            Action = SignalAction.Sell,
            OrderType = OrderType.Limit,
            Timeframe = "4H",
            Timestamp = DateTimeOffset.UtcNow,
            Secret = "s"
        };

        signal.EntryPrice.Should().BeNull();
        signal.StopLoss.Should().BeNull();
        signal.TakeProfit.Should().BeNull();
        signal.LotSize.Should().BeNull();
        signal.RiskPercent.Should().BeNull();
        signal.Comment.Should().BeNull();
    }

    [Fact]
    public void Signal_JsonRoundTrip_PreservesValues()
    {
        var original = CreateTestSignal();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

        var json = JsonSerializer.Serialize(original, options);
        var deserialized = JsonSerializer.Deserialize<Signal>(json, options);

        deserialized.Should().NotBeNull();
        deserialized!.AlertId.Should().Be(original.AlertId);
        deserialized.Action.Should().Be(original.Action);
        deserialized.StopLoss.Should().Be(original.StopLoss);
    }

    [Fact]
    public void Signal_JsonSerialization_UsesStringEnums()
    {
        var signal = CreateTestSignal();
        var json = JsonSerializer.Serialize(signal);

        json.Should().Contain("\"Buy\"");
        json.Should().Contain("\"Market\"");
    }

    [Theory]
    [InlineData(SignalAction.Buy)]
    [InlineData(SignalAction.Sell)]
    [InlineData(SignalAction.Close)]
    [InlineData(SignalAction.Modify)]
    public void SignalAction_AllValues_Exist(SignalAction action)
    {
        Enum.IsDefined(action).Should().BeTrue();
    }

    [Theory]
    [InlineData(OrderType.Market)]
    [InlineData(OrderType.Limit)]
    [InlineData(OrderType.Stop)]
    public void OrderType_AllValues_Exist(OrderType orderType)
    {
        Enum.IsDefined(orderType).Should().BeTrue();
    }
}
