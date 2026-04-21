using FluentAssertions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class SmokeTests
{
    [Fact]
    public void Signal_Record_CanBeCreated()
    {
        var signal = new Signal
        {
            AlertId = "test-123",
            StrategyId = "strat-1",
            AccountTag = "default",
            Symbol = "EURUSD",
            Action = SignalAction.Buy,
            OrderType = OrderType.Market,
            Timeframe = "1H",
            Timestamp = DateTimeOffset.UtcNow,
            Secret = "secret123"
        };

        signal.Should().NotBeNull();
        signal.AlertId.Should().Be("test-123");
        signal.Action.Should().Be(SignalAction.Buy);
    }

    [Fact]
    public void Rule_Record_CanBeCreated()
    {
        var rule = new Rule
        {
            Name = "Test Rule",
            DestinationIds = "1,2"
        };

        rule.Should().NotBeNull();
        rule.Name.Should().Be("Test Rule");
        rule.Enabled.Should().BeTrue();
        rule.LotMultiplier.Should().Be(1.0m);
    }
}
