namespace TVBridge.Channels.NinjaTrader;

public sealed class NinjaTraderConfig
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 36973;
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int CommandTimeoutMs { get; set; } = 10_000;

    /// <summary>
    /// Symbol mapping from TradingView names to NinjaTrader instrument names.
    /// E.g. "EURUSD" → "EUR/USD", "ES1!" → "ES 09-25", "NQ1!" → "NQ 09-25"
    /// </summary>
    public Dictionary<string, string> SymbolMap { get; set; } = new()
    {
        ["EURUSD"] = "EUR/USD",
        ["GBPUSD"] = "GBP/USD",
        ["USDJPY"] = "USD/JPY",
        ["AUDUSD"] = "AUD/USD",
        ["USDCAD"] = "USD/CAD",
        ["USDCHF"] = "USD/CHF",
        ["NZDUSD"] = "NZD/USD"
    };
}
