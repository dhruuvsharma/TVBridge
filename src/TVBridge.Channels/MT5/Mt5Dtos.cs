using System.Text.Json.Serialization;

namespace TVBridge.Channels.Mt5;

public sealed record Mt5Request(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("params")] object? Params = null);

public sealed record Mt5Response(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("data")] System.Text.Json.JsonElement? Data = null,
    [property: JsonPropertyName("error")] string? Error = null);

public sealed record Mt5OrderResult(
    [property: JsonPropertyName("ticket")] long Ticket,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("action")] string Action,
    [property: JsonPropertyName("volume")] decimal Volume,
    [property: JsonPropertyName("price")] decimal Price);

public sealed record Mt5Position(
    [property: JsonPropertyName("ticket")] long Ticket,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("volume")] decimal Volume,
    [property: JsonPropertyName("open_price")] decimal OpenPrice,
    [property: JsonPropertyName("current_price")] decimal CurrentPrice,
    [property: JsonPropertyName("profit")] decimal Profit,
    [property: JsonPropertyName("sl")] decimal? Sl = null,
    [property: JsonPropertyName("tp")] decimal? Tp = null,
    [property: JsonPropertyName("time")] DateTimeOffset? Time = null,
    [property: JsonPropertyName("comment")] string? Comment = null);

public sealed record Mt5AccountBalance(
    [property: JsonPropertyName("balance")] decimal Balance,
    [property: JsonPropertyName("equity")] decimal Equity,
    [property: JsonPropertyName("margin")] decimal Margin,
    [property: JsonPropertyName("free_margin")] decimal FreeMargin,
    [property: JsonPropertyName("profit")] decimal Profit);

public sealed record Mt5HistoryDeal(
    [property: JsonPropertyName("ticket")] long Ticket,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("volume")] decimal Volume,
    [property: JsonPropertyName("price")] decimal Price,
    [property: JsonPropertyName("profit")] decimal Profit,
    [property: JsonPropertyName("time")] DateTimeOffset Time);

public sealed record Mt5AccountState(
    [property: JsonPropertyName("positions")] List<Mt5Position> Positions,
    [property: JsonPropertyName("balance")] Mt5AccountBalance Balance,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp);
