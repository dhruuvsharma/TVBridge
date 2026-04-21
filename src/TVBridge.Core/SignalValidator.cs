namespace TVBridge.Core;

public sealed class SignalValidator
{
    private static readonly TimeSpan MaxFutureTimestamp = TimeSpan.FromMinutes(5);

    public ValidationResult Validate(Signal signal)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Required string fields
        if (string.IsNullOrWhiteSpace(signal.AlertId))
            errors.Add("alert_id is required.");
        if (string.IsNullOrWhiteSpace(signal.StrategyId))
            errors.Add("strategy_id is required.");
        if (string.IsNullOrWhiteSpace(signal.AccountTag))
            errors.Add("account_tag is required.");
        if (string.IsNullOrWhiteSpace(signal.Symbol))
            errors.Add("symbol is required.");
        if (string.IsNullOrWhiteSpace(signal.Timeframe))
            errors.Add("timeframe is required.");
        if (string.IsNullOrWhiteSpace(signal.Secret))
            errors.Add("secret is required.");

        // LIMIT/STOP orders must have entry_price
        if (signal.OrderType is OrderType.Limit or OrderType.Stop && signal.EntryPrice is null)
            errors.Add($"entry_price is required for {signal.OrderType} orders.");

        // lot_size and risk_percent are mutually exclusive
        if (signal.LotSize.HasValue && signal.RiskPercent.HasValue)
            errors.Add("lot_size and risk_percent are mutually exclusive; set only one.");

        // Numeric range checks
        if (signal.LotSize is <= 0)
            errors.Add("lot_size must be positive.");
        if (signal.RiskPercent is <= 0 or > 100)
            errors.Add("risk_percent must be between 0 and 100.");
        if (signal.StopLoss is <= 0)
            errors.Add("stop_loss must be positive.");
        if (signal.TakeProfit is <= 0)
            errors.Add("take_profit must be positive.");

        // Timestamp sanity
        if (signal.Timestamp > DateTimeOffset.UtcNow.Add(MaxFutureTimestamp))
            warnings.Add("timestamp is more than 5 minutes in the future.");

        return new ValidationResult { Errors = errors, Warnings = warnings };
    }
}
