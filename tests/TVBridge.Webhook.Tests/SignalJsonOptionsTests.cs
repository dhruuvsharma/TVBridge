using System.Text.Json;
using FluentAssertions;
using TVBridge.Core;
using TVBridge.Webhook;
using Xunit;

namespace TVBridge.Webhook.Tests;

public sealed class SignalJsonOptionsTests
{
    [Fact]
    public void Deserialize_SnakeCaseJson_ParsesCorrectly()
    {
        var json = """
        {
            "alert_id": "test-123",
            "strategy_id": "MA_CROSS",
            "account_tag": "default",
            "symbol": "EURUSD",
            "action": "Buy",
            "order_type": "Market",
            "entry_price": null,
            "stop_loss": 1.0850,
            "take_profit": 1.1050,
            "lot_size": 0.1,
            "risk_percent": null,
            "timeframe": "1H",
            "timestamp": "2026-04-22T12:00:00Z",
            "comment": "Test",
            "secret": "my-secret"
        }
        """;

        var signal = JsonSerializer.Deserialize<Signal>(json, SignalJsonOptions.Default);

        signal.Should().NotBeNull();
        signal!.AlertId.Should().Be("test-123");
        signal.StrategyId.Should().Be("MA_CROSS");
        signal.Action.Should().Be(SignalAction.Buy);
        signal.OrderType.Should().Be(OrderType.Market);
        signal.StopLoss.Should().Be(1.0850m);
        signal.LotSize.Should().Be(0.1m);
        signal.RiskPercent.Should().BeNull();
    }

    [Fact]
    public void Serialize_Signal_ProducesSnakeCaseJson()
    {
        var signal = new Signal
        {
            AlertId = "test",
            StrategyId = "s",
            AccountTag = "a",
            Symbol = "EURUSD",
            Action = SignalAction.Sell,
            OrderType = OrderType.Limit,
            EntryPrice = 1.1000m,
            Timeframe = "4H",
            Timestamp = DateTimeOffset.Parse("2026-04-22T12:00:00Z"),
            Secret = "secret"
        };

        var json = JsonSerializer.Serialize(signal, SignalJsonOptions.Default);

        json.Should().Contain("\"alert_id\"");
        json.Should().Contain("\"strategy_id\"");
        json.Should().Contain("\"order_type\"");
    }

    [Fact]
    public void Deserialize_CaseInsensitive_Works()
    {
        var json = """
        {
            "Alert_Id": "test",
            "Strategy_Id": "s",
            "Account_Tag": "a",
            "Symbol": "X",
            "Action": "Buy",
            "Order_Type": "Market",
            "Timeframe": "1H",
            "Timestamp": "2026-04-22T12:00:00Z",
            "Secret": "s"
        }
        """;

        var signal = JsonSerializer.Deserialize<Signal>(json, SignalJsonOptions.Default);
        signal.Should().NotBeNull();
        signal!.AlertId.Should().Be("test");
    }
}
