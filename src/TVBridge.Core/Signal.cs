using System.Text.Json.Serialization;

namespace TVBridge.Core;

public sealed record Signal
{
    public required string AlertId { get; init; }
    public required string StrategyId { get; init; }
    public required string AccountTag { get; init; }
    public required string Symbol { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required SignalAction Action { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required OrderType OrderType { get; init; }

    public decimal? EntryPrice { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public decimal? LotSize { get; init; }
    public decimal? RiskPercent { get; init; }
    public required string Timeframe { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? Comment { get; init; }
    public required string Secret { get; init; }

    // DB fields
    public int Id { get; init; }
    public DateTimeOffset ReceivedAt { get; init; }
    public bool Processed { get; init; }
}
