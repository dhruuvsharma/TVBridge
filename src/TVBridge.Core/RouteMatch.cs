namespace TVBridge.Core;

public sealed record RouteMatch(
    Rule Rule,
    List<int> DestinationIds,
    bool DryRun,
    decimal LotMultiplier);
