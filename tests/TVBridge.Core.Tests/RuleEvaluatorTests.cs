using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace TVBridge.Core.Tests;

public sealed class RuleEvaluatorTests
{
    private readonly RuleEvaluator _evaluator = new(NullLogger<RuleEvaluator>.Instance);

    private static Signal CreateSignal(
        string strategyId = "MA_CROSS",
        string symbol = "EURUSD",
        SignalAction action = SignalAction.Buy,
        string accountTag = "default",
        string timeframe = "1H") => new()
    {
        AlertId = "test-1",
        StrategyId = strategyId,
        AccountTag = accountTag,
        Symbol = symbol,
        Action = action,
        OrderType = OrderType.Market,
        Timeframe = timeframe,
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "s"
    };

    private static Rule CreateRule(
        string name = "R1",
        string? strategyId = null,
        string? symbol = null,
        string? action = null,
        string? accountTag = null,
        string? timeframe = null,
        int priority = 0,
        bool continueOnMatch = false,
        bool? dryRunOverride = null,
        decimal lotMultiplier = 1.0m) => new()
    {
        Name = name,
        StrategyId = strategyId,
        Symbol = symbol,
        Action = action,
        AccountTag = accountTag,
        Timeframe = timeframe,
        DestinationIds = "1",
        Priority = priority,
        ContinueOnMatch = continueOnMatch,
        DryRunOverride = dryRunOverride,
        LotMultiplier = lotMultiplier
    };

    [Fact]
    public void Evaluate_WildcardRule_MatchesAnything()
    {
        var rules = new[] { CreateRule() };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_ExactMatch_Matches()
    {
        var rules = new[] { CreateRule(strategyId: "MA_CROSS", symbol: "EURUSD") };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_ExactMatch_CaseInsensitive()
    {
        var rules = new[] { CreateRule(symbol: "eurusd") };
        var matches = _evaluator.Evaluate(CreateSignal(symbol: "EURUSD"), rules, false);
        matches.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_ExactMismatch_DoesNotMatch()
    {
        var rules = new[] { CreateRule(symbol: "GBPUSD") };
        var matches = _evaluator.Evaluate(CreateSignal(symbol: "EURUSD"), rules, false);
        matches.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_PrefixMatch_Works()
    {
        var rules = new[] { CreateRule(symbol: "EUR*") };
        var matches = _evaluator.Evaluate(CreateSignal(symbol: "EURUSD"), rules, false);
        matches.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_PrefixMismatch_DoesNotMatch()
    {
        var rules = new[] { CreateRule(symbol: "GBP*") };
        var matches = _evaluator.Evaluate(CreateSignal(symbol: "EURUSD"), rules, false);
        matches.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_PriorityOrdering_LowerFirst()
    {
        var rules = new[]
        {
            CreateRule("R2", priority: 10, continueOnMatch: true),
            CreateRule("R1", priority: 1, continueOnMatch: true)
        };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches.Should().HaveCount(2);
        matches[0].Rule.Name.Should().Be("R1");
        matches[1].Rule.Name.Should().Be("R2");
    }

    [Fact]
    public void Evaluate_ContinueOnMatchFalse_StopsAfterFirst()
    {
        var rules = new[]
        {
            CreateRule("R1", priority: 1, continueOnMatch: false),
            CreateRule("R2", priority: 2)
        };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches.Should().HaveCount(1);
        matches[0].Rule.Name.Should().Be("R1");
    }

    [Fact]
    public void Evaluate_ContinueOnMatchTrue_KeepsGoing()
    {
        var rules = new[]
        {
            CreateRule("R1", priority: 1, continueOnMatch: true),
            CreateRule("R2", priority: 2)
        };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches.Should().HaveCount(2);
    }

    [Fact]
    public void Evaluate_DryRunOverride_True_OverridesGlobal()
    {
        var rules = new[] { CreateRule(dryRunOverride: true) };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, globalDryRun: false);
        matches[0].DryRun.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_DryRunOverride_False_OverridesGlobal()
    {
        var rules = new[] { CreateRule(dryRunOverride: false) };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, globalDryRun: true);
        matches[0].DryRun.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_NoDryRunOverride_UsesGlobal()
    {
        var rules = new[] { CreateRule(dryRunOverride: null) };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, globalDryRun: true);
        matches[0].DryRun.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_LotMultiplier_PassedThrough()
    {
        var rules = new[] { CreateRule(lotMultiplier: 2.5m) };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches[0].LotMultiplier.Should().Be(2.5m);
    }

    [Fact]
    public void Evaluate_DisabledRule_Skipped()
    {
        var rules = new[] { CreateRule() with { Enabled = false } };
        var matches = _evaluator.Evaluate(CreateSignal(), rules, false);
        matches.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_NoRules_ReturnsEmpty()
    {
        var matches = _evaluator.Evaluate(CreateSignal(), Array.Empty<Rule>(), false);
        matches.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ActionMatch()
    {
        var rules = new[] { CreateRule(action: "Buy") };
        var matches = _evaluator.Evaluate(CreateSignal(action: SignalAction.Buy), rules, false);
        matches.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_ActionMismatch()
    {
        var rules = new[] { CreateRule(action: "Sell") };
        var matches = _evaluator.Evaluate(CreateSignal(action: SignalAction.Buy), rules, false);
        matches.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_MultipleConditions_AllMustMatch()
    {
        var rules = new[] { CreateRule(strategyId: "MA_CROSS", symbol: "EURUSD", action: "Buy") };
        var matches = _evaluator.Evaluate(
            CreateSignal(strategyId: "MA_CROSS", symbol: "EURUSD", action: SignalAction.Buy),
            rules, false);
        matches.Should().HaveCount(1);
    }

    [Fact]
    public void Evaluate_MultipleConditions_OneFailsNoMatch()
    {
        var rules = new[] { CreateRule(strategyId: "MA_CROSS", symbol: "GBPUSD", action: "Buy") };
        var matches = _evaluator.Evaluate(
            CreateSignal(strategyId: "MA_CROSS", symbol: "EURUSD", action: SignalAction.Buy),
            rules, false);
        matches.Should().BeEmpty();
    }
}
