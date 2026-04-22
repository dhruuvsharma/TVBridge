namespace TVBridge.Channels.NinjaTrader;

/// <summary>
/// Maps TradingView symbol names to NinjaTrader instrument names.
/// Falls back to the original symbol if no mapping is found.
/// </summary>
public sealed class NtSymbolMapper
{
    private readonly Dictionary<string, string> _map;

    public NtSymbolMapper(Dictionary<string, string> symbolMap)
    {
        _map = new Dictionary<string, string>(symbolMap, StringComparer.OrdinalIgnoreCase);
    }

    public string Map(string tradingViewSymbol)
    {
        return _map.TryGetValue(tradingViewSymbol, out var ntSymbol) ? ntSymbol : tradingViewSymbol;
    }
}
