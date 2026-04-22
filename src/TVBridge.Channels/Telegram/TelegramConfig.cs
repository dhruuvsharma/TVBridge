namespace TVBridge.Channels.Telegram;

public sealed class TelegramConfig
{
    public string BotToken { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string MessageTemplate { get; set; } = DefaultTemplate;

    public const string DefaultTemplate =
        """
        📊 *{Action} {Symbol}*
        Order: {OrderType}
        Lot: {LotSize}
        Entry: {EntryPrice}
        SL: {StopLoss}
        TP: {TakeProfit}
        Strategy: {StrategyId}
        Time: {Timestamp}
        """;
}
