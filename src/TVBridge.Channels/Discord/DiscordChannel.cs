using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Channels.Discord;

/// <summary>
/// Output channel that sends signal notifications to Discord via webhook.
/// </summary>
public sealed class DiscordChannel : IOutputChannel
{
    private readonly DiscordConfig _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DiscordChannel> _logger;

    public string Name { get; }
    public string ChannelType => "Discord";
    public int ChannelId { get; }

    public DiscordChannel(
        DiscordConfig config,
        IHttpClientFactory httpClientFactory,
        ILogger<DiscordChannel> logger,
        string name = "Discord",
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
        var description = SignalMessageFormatter.Format(signal, _config.MessageTemplate);

        if (dryRun)
            return ChannelResult.DryRun($"Discord webhook: {description}");

        try
        {
            var httpClient = _httpClientFactory.CreateClient("Discord");

            var embed = new DiscordEmbed
            {
                Title = $"{signal.Action} {signal.Symbol}",
                Description = description,
                Color = signal.Action is SignalAction.Buy ? 0x2ECC71 : signal.Action is SignalAction.Sell ? 0xE74C3C : 0x3498DB,
                Timestamp = signal.Timestamp
            };

            var payload = new DiscordWebhookPayload
            {
                Username = _config.Username,
                Embeds = [embed]
            };

            var response = await httpClient.PostAsJsonAsync(
                _config.WebhookUrl, payload, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Discord webhook returned {Status}: {Body}",
                    response.StatusCode, body);
                return ChannelResult.Fail($"Discord webhook returned {response.StatusCode}");
            }

            _logger.LogInformation("Discord message sent for signal {AlertId}", signal.AlertId);
            return ChannelResult.Ok("Discord message sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Discord message for signal {AlertId}", signal.AlertId);
            return ChannelResult.Fail($"Discord error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_config.WebhookUrl))
            return false;

        try
        {
            var httpClient = _httpClientFactory.CreateClient("Discord");
            var response = await httpClient.GetAsync(_config.WebhookUrl, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Discord config validation failed");
            return false;
        }
    }
}

internal sealed record DiscordWebhookPayload
{
    [JsonPropertyName("username")]
    public string? Username { get; init; }

    [JsonPropertyName("embeds")]
    public List<DiscordEmbed>? Embeds { get; init; }
}

internal sealed record DiscordEmbed
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("color")]
    public int Color { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }
}
