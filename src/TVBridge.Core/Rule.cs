namespace TVBridge.Core;

public sealed record Rule
{
    public int Id { get; init; }
    public required string Name { get; init; }

    // Match conditions — null means wildcard (match anything)
    public string? StrategyId { get; init; }
    public string? Symbol { get; init; }
    public string? Action { get; init; }
    public string? AccountTag { get; init; }
    public string? Timeframe { get; init; }

    // Routing
    public required string DestinationIds { get; init; } // Comma-separated channel IDs (SQLite-friendly)
    public int Priority { get; init; }
    public bool ContinueOnMatch { get; init; }

    // Overrides
    public bool? DryRunOverride { get; init; }
    public decimal LotMultiplier { get; init; } = 1.0m;

    public bool Enabled { get; init; } = true;

    public List<int> GetDestinationIdList() =>
        string.IsNullOrWhiteSpace(DestinationIds)
            ? []
            : DestinationIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
}
