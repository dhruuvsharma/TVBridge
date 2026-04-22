using TVBridge.Core;

namespace TVBridge.Channels;

/// <summary>
/// Formats a Signal into a human-readable message using a template string.
/// Supports placeholders: {Action}, {Symbol}, {OrderType}, {LotSize}, {EntryPrice},
/// {StopLoss}, {TakeProfit}, {StrategyId}, {AccountTag}, {Timeframe}, {Timestamp}, {Comment}, {AlertId}
/// </summary>
public static class SignalMessageFormatter
{
    public static string Format(Signal signal, string template)
    {
        return template
            .Replace("{Action}", signal.Action.ToString())
            .Replace("{Symbol}", signal.Symbol)
            .Replace("{OrderType}", signal.OrderType.ToString())
            .Replace("{LotSize}", signal.LotSize?.ToString("F2") ?? "—")
            .Replace("{EntryPrice}", signal.EntryPrice?.ToString("F5") ?? "Market")
            .Replace("{StopLoss}", signal.StopLoss?.ToString("F5") ?? "—")
            .Replace("{TakeProfit}", signal.TakeProfit?.ToString("F5") ?? "—")
            .Replace("{StrategyId}", signal.StrategyId)
            .Replace("{AccountTag}", signal.AccountTag)
            .Replace("{Timeframe}", signal.Timeframe)
            .Replace("{Timestamp}", signal.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))
            .Replace("{Comment}", signal.Comment ?? "")
            .Replace("{AlertId}", signal.AlertId);
    }
}
