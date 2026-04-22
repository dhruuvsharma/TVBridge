using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TVBridge.Core;

namespace TVBridge.Channels.Telegram;

/// <summary>
/// Output channel that sends signal notifications to Telegram via Bot API.
/// </summary>
public sealed class TelegramChannel : IOutputChannel
{
    private readonly TelegramConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TelegramChannel> _logger;

    public string Name { get; }
    public string ChannelType => "Telegram";
    public int ChannelId { get; }

    public TelegramChannel(
        TelegramConfig config,
        IHttpClientFactory httpClientFactory,
        ILogger<TelegramChannel> logger,
        string name = "Telegram",
        int channelId = 0)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        Name = name;
        ChannelId = channelId;
    }

    public async Task<ChannelResult> SendAsync(Signal signal, bool dryRun, CancellationToken cancellationToken = default)
    {
        var message = SignalMessageFormatter.Format(signal, _config.MessageTemplate);

        if (dryRun)
            return ChannelResult.DryRun($"Telegram → {_config.ChatId}: {message}");

        try
        {
            var httpClient = _httpClientFactory.CreateClient("Telegram");
            var client = new TelegramBotClient(_config.BotToken, httpClient);

            await client.SendMessage(
                chatId: _config.ChatId,
                text: message,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Telegram message sent for signal {AlertId} to {ChatId}",
                signal.AlertId, _config.ChatId);

            return ChannelResult.Ok($"Telegram message sent to {_config.ChatId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message for signal {AlertId}", signal.AlertId);
            return ChannelResult.Fail($"Telegram error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.BotToken) || string.IsNullOrWhiteSpace(_config.ChatId))
            return false;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("Telegram");
            var client = new TelegramBotClient(_config.BotToken, httpClient);
            var me = await client.GetMe(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Telegram bot validated: @{Username}", me.Username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram config validation failed");
            return false;
        }
    }
}
