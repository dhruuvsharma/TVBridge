using FluentAssertions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class RuleTests
{
    [Fact]
    public void Rule_Defaults_AreCorrect()
    {
        var rule = new Rule
        {
            Name = "Default Rule",
            DestinationIds = "1"
        };

        rule.Priority.Should().Be(0);
        rule.ContinueOnMatch.Should().BeFalse();
        rule.DryRunOverride.Should().BeNull();
        rule.LotMultiplier.Should().Be(1.0m);
        rule.Enabled.Should().BeTrue();
    }

    [Fact]
    public void Rule_WildcardConditions_AreNull()
    {
        var rule = new Rule
        {
            Name = "Wildcard Rule",
            DestinationIds = "1,2"
        };

        rule.StrategyId.Should().BeNull();
        rule.Symbol.Should().BeNull();
        rule.Action.Should().BeNull();
        rule.AccountTag.Should().BeNull();
        rule.Timeframe.Should().BeNull();
    }

    [Fact]
    public void Rule_GetDestinationIdList_ParsesCorrectly()
    {
        var rule = new Rule
        {
            Name = "Multi-dest",
            DestinationIds = "1,2,3"
        };

        var ids = rule.GetDestinationIdList();
        ids.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void Rule_GetDestinationIdList_SingleId()
    {
        var rule = new Rule
        {
            Name = "Single-dest",
            DestinationIds = "5"
        };

        rule.GetDestinationIdList().Should().BeEquivalentTo([5]);
    }

    [Fact]
    public void Rule_GetDestinationIdList_EmptyString_ReturnsEmpty()
    {
        var rule = new Rule
        {
            Name = "No-dest",
            DestinationIds = ""
        };

        rule.GetDestinationIdList().Should().BeEmpty();
    }

    [Fact]
    public void Rule_AllConditionsSet()
    {
        var rule = new Rule
        {
            Name = "Specific Rule",
            StrategyId = "MA_CROSS",
            Symbol = "EURUSD",
            Action = "BUY",
            AccountTag = "live-1",
            Timeframe = "1H",
            DestinationIds = "1",
            Priority = 10,
            ContinueOnMatch = true,
            DryRunOverride = false,
            LotMultiplier = 2.0m,
            Enabled = true
        };

        rule.StrategyId.Should().Be("MA_CROSS");
        rule.Priority.Should().Be(10);
        rule.ContinueOnMatch.Should().BeTrue();
        rule.DryRunOverride.Should().BeFalse();
        rule.LotMultiplier.Should().Be(2.0m);
    }
}
