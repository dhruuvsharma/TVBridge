using Microsoft.Extensions.Logging;

namespace TVBridge.Core;

public sealed class RuleEvaluator
{
    private readonly ILogger<RuleEvaluator> _logger;

    public RuleEvaluator(ILogger<RuleEvaluator> logger)
    {
        _logger = logger;
    }

    public List<RouteMatch> Evaluate(Signal signal, IReadOnlyList<Rule> rules, bool globalDryRun)
    {
        var matches = new List<RouteMatch>();

        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            if (!rule.Enabled)
                continue;

            if (!IsMatch(signal, rule))
                continue;

            var effectiveDryRun = rule.DryRunOverride ?? globalDryRun;
            var effectiveLotMultiplier = rule.LotMultiplier;

            var match = new RouteMatch(
                rule,
                rule.GetDestinationIdList(),
                effectiveDryRun,
                effectiveLotMultiplier);

            matches.Add(match);

            _logger.LogInformation(
                "Signal {AlertId} matched rule '{RuleName}' (priority {Priority}, dryRun={DryRun})",
                signal.AlertId, rule.Name, rule.Priority, effectiveDryRun);

            if (!rule.ContinueOnMatch)
            {
                _logger.LogDebug("Rule '{RuleName}' stops further evaluation", rule.Name);
                break;
            }
        }

        if (matches.Count == 0)
        {
            _logger.LogWarning("Signal {AlertId} matched no rules", signal.AlertId);
        }

        return matches;
    }

    private static bool IsMatch(Signal signal, Rule rule)
    {
        return MatchesCondition(signal.StrategyId, rule.StrategyId)
            && MatchesCondition(signal.Symbol, rule.Symbol)
            && MatchesCondition(signal.Action.ToString(), rule.Action)
            && MatchesCondition(signal.AccountTag, rule.AccountTag)
            && MatchesCondition(signal.Timeframe, rule.Timeframe);
    }

    private static bool MatchesCondition(string signalValue, string? ruleCondition)
    {
        // null = wildcard, matches anything
        if (ruleCondition is null)
            return true;

        // Prefix match: "EUR*" matches "EURUSD"
        if (ruleCondition.EndsWith('*'))
        {
            var prefix = ruleCondition[..^1];
            return signalValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        // Exact match (case-insensitive)
        return string.Equals(signalValue, ruleCondition, StringComparison.OrdinalIgnoreCase);
    }
}
