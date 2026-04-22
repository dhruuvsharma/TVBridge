using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Channels.Discord;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.Discord;

public sealed class DiscordChannelTests
{
    private static readonly Signal TestSignal = new()
    {
        AlertId = "dc-1",
        StrategyId = "strat",
        AccountTag = "demo",
        Symbol = "GBPUSD",
        Action = SignalAction.Sell,
        OrderType = OrderType.Limit,
        LotSize = 0.5m,
        EntryPrice = 1.25m,
        Timeframe = "4H",
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "s"
    };

    [Fact]
    public async Task SendAsync_DryRun_ReturnsDescription()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        var factory = new FakeHttpClientFactory();
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: true);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("[DRY RUN]");
        result.Message.Should().Contain("GBPUSD");
    }

    [Fact]
    public async Task SendAsync_Success_ReturnsOk()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        var factory = new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.NoContent));
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: false);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Discord message sent");
    }

    [Fact]
    public async Task SendAsync_PostsEmbedWithCorrectColor()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var factory = new FakeHttpClientFactory(handler);
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        await channel.SendAsync(TestSignal, dryRun: false);

        // Sell should use red color 0xE74C3C = 15158332
        handler.LastRequestBody.Should().Contain("15158332");
        handler.LastRequestBody.Should().Contain("Sell GBPUSD");
    }

    [Fact]
    public async Task SendAsync_BuySignal_GreenColor()
    {
        var signal = TestSignal with { Action = SignalAction.Buy };
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var factory = new FakeHttpClientFactory(handler);
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        await channel.SendAsync(signal, dryRun: false);

        // Buy should use green color 0x2ECC71 = 3066993
        handler.LastRequestBody.Should().Contain("3066993");
    }

    [Fact]
    public async Task SendAsync_WebhookError_ReturnsFail()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        var factory = new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.TooManyRequests)
        {
            Content = new StringContent("rate limited")
        });
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("TooManyRequests");
    }

    [Fact]
    public async Task ValidateConfigAsync_EmptyUrl_ReturnsFalse()
    {
        var config = new DiscordConfig { WebhookUrl = "" };
        var factory = new FakeHttpClientFactory();
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateConfigAsync_ValidUrl_ReturnsTrue()
    {
        var config = new DiscordConfig { WebhookUrl = "https://discord.com/api/webhooks/123/abc" };
        var factory = new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_UsesCustomTemplate()
    {
        var config = new DiscordConfig
        {
            WebhookUrl = "https://discord.com/api/webhooks/123/abc",
            MessageTemplate = "ALERT: {Action} {Symbol}"
        };
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var factory = new FakeHttpClientFactory(handler);
        var channel = new DiscordChannel(config, factory, NullLogger<DiscordChannel>.Instance);

        await channel.SendAsync(TestSignal, dryRun: false);

        handler.LastRequestBody.Should().Contain("ALERT: Sell GBPUSD");
    }
}
