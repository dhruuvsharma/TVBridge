using TVBridge.Core;

namespace TVBridge.Channels.NinjaTrader;

/// <summary>
/// Builds NinjaTrader 8 ATI command strings from Signal data.
///
/// ATI command format:
///   PLACE;{instrument};{account};{action};{qty};{orderType};{limitPrice};{stopPrice};{tif};{oco};{orderId};{strategy};{strategyId}
///   CLOSEPOSITION;{account};{instrument}
///   CANCEL;{orderId}
///
/// See: https://ninjatrader.com/support/helpGuides/nt8/automated_trading_interface_at.htm
/// </summary>
public static class NtAtiCommandBuilder
{
    public static string BuildPlaceOrder(Signal signal, string ntSymbol, string account = "")
    {
        var action = signal.Action switch
        {
            SignalAction.Buy => "BUY",
            SignalAction.Sell => "SELL",
            _ => throw new ArgumentException($"Cannot place order for action: {signal.Action}")
        };

        var orderType = signal.OrderType switch
        {
            OrderType.Market => "MARKET",
            OrderType.Limit => "LIMIT",
            OrderType.Stop => "STOPMARKET",
            _ => "MARKET"
        };

        var qty = (int)Math.Max(1, (decimal)(signal.LotSize ?? 1));
        var limitPrice = signal.OrderType == OrderType.Limit && signal.EntryPrice.HasValue
            ? signal.EntryPrice.Value.ToString("F5")
            : "0";
        var stopPrice = signal.OrderType == OrderType.Stop && signal.EntryPrice.HasValue
            ? signal.EntryPrice.Value.ToString("F5")
            : "0";
        var tif = "GTC";

        // PLACE;instrument;account;action;qty;orderType;limitPrice;stopPrice;tif;oco;orderId;strategy;strategyId
        return $"PLACE;{ntSymbol};{account};{action};{qty};{orderType};{limitPrice};{stopPrice};{tif};;;;";
    }

    public static string BuildClosePosition(string ntSymbol, string account = "")
    {
        return $"CLOSEPOSITION;{account};{ntSymbol}";
    }

    public static string BuildCloseStrategy(string strategyId)
    {
        return $"CLOSESTRATEGY;{strategyId}";
    }
}
