using Microsoft.Extensions.Logging;

namespace TVBridge.Core;

public sealed class SignalPipeline
{
    private readonly ILogger<SignalPipeline> _logger;
    private readonly SignalValidator _validator;
    private readonly RuleEvaluator _ruleEvaluator;
    private readonly IReadOnlyList<IOutputChannel> _channels;

    public SignalPipeline(
        ILogger<SignalPipeline> logger,
        SignalValidator validator,
        RuleEvaluator ruleEvaluator,
        IEnumerable<IOutputChannel> channels)
    {
        _logger = logger;
        _validator = validator;
        _ruleEvaluator = ruleEvaluator;
        _channels = channels.ToList();
    }

    public async Task<PipelineResult> ProcessAsync(
        Signal signal,
        IReadOnlyList<Rule> rules,
        bool globalDryRun,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing signal {AlertId} for {Symbol} {Action}",
            signal.AlertId, signal.Symbol, signal.Action);

        // Validate
        var validation = _validator.Validate(signal);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Signal {AlertId} validation failed: {Errors}",
                signal.AlertId, string.Join("; ", validation.Errors));
            return PipelineResult.ValidationFailed(validation);
        }

        foreach (var warning in validation.Warnings)
        {
            _logger.LogWarning("Signal {AlertId}: {Warning}", signal.AlertId, warning);
        }

        // Evaluate rules
        var matches = _ruleEvaluator.Evaluate(signal, rules, globalDryRun);
        if (matches.Count == 0)
        {
            _logger.LogInformation("Signal {AlertId} matched no rules, skipping", signal.AlertId);
            return PipelineResult.NoMatchingRules();
        }

        // Dispatch to channels
        var results = new List<ChannelDispatchResult>();
        foreach (var match in matches)
        {
            foreach (var destinationId in match.DestinationIds)
            {
                var channel = _channels.FirstOrDefault(c => c.ChannelId == destinationId);
                if (channel is null)
                {
                    _logger.LogWarning("Channel ID {ChannelId} not found for rule '{RuleName}'",
                        destinationId, match.Rule.Name);
                    results.Add(new ChannelDispatchResult(destinationId, match.Rule.Name, false, "Channel not found"));
                    continue;
                }

                try
                {
                    var adjustedSignal = match.LotMultiplier != 1.0m && signal.LotSize.HasValue
                        ? signal with { LotSize = signal.LotSize * match.LotMultiplier }
                        : signal;

                    var channelResult = await channel.SendAsync(adjustedSignal, match.DryRun, cancellationToken)
                        .ConfigureAwait(false);

                    results.Add(new ChannelDispatchResult(
                        destinationId, match.Rule.Name, channelResult.Success, channelResult.Message));

                    _logger.LogInformation(
                        "Signal {AlertId} → channel {ChannelName}: {Result}",
                        signal.AlertId, channel.Name, channelResult.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Signal {AlertId} → channel ID {ChannelId} failed", signal.AlertId, destinationId);
                    results.Add(new ChannelDispatchResult(destinationId, match.Rule.Name, false, ex.Message));
                }
            }
        }

        return PipelineResult.Dispatched(matches.Count, results);
    }
}

public sealed record PipelineResult
{
    public bool Success { get; init; }
    public string Status { get; init; } = string.Empty;
    public int MatchedRuleCount { get; init; }
    public List<ChannelDispatchResult> DispatchResults { get; init; } = [];
    public ValidationResult? Validation { get; init; }

    public static PipelineResult ValidationFailed(ValidationResult validation) => new()
    {
        Success = false,
        Status = "ValidationFailed",
        Validation = validation
    };

    public static PipelineResult NoMatchingRules() => new()
    {
        Success = true,
        Status = "NoMatchingRules"
    };

    public static PipelineResult Dispatched(int matchedRules, List<ChannelDispatchResult> results) => new()
    {
        Success = results.All(r => r.Success),
        Status = "Dispatched",
        MatchedRuleCount = matchedRules,
        DispatchResults = results
    };
}

public sealed record ChannelDispatchResult(
    int ChannelId,
    string RuleName,
    bool Success,
    string Message);
