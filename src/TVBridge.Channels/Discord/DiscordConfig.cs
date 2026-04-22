namespace TVBridge.Channels.Discord;

public sealed class DiscordConfig
{
    public string WebhookUrl { get; set; } = string.Empty;
    public string Username { get; set; } = "TVBridge";
    public string MessageTemplate { get; set; } = DefaultTemplate;

    public const string DefaultTemplate =
        """
        **{Action} {Symbol}**
        Order: {OrderType}
        Lot: {LotSize}
        Entry: {EntryPrice}
        SL: {StopLoss}
        TP: {TakeProfit}
        Strategy: {StrategyId}
        Time: {Timestamp}
        """;
}
