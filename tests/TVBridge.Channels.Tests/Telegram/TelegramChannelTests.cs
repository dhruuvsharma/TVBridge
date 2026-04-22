using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Channels.Telegram;
using TVBridge.Core;
using Xunit;

namespace TVBridge.Channels.Tests.Telegram;

public sealed class TelegramChannelTests
{
    private static readonly Signal TestSignal = new()
    {
        AlertId = "tg-1",
        StrategyId = "strat",
        AccountTag = "demo",
        Symbol = "EURUSD",
        Action = SignalAction.Buy,
        OrderType = OrderType.Market,
        LotSize = 0.1m,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.UtcNow,
        Secret = "s"
    };

    [Fact]
    public async Task SendAsync_DryRun_ReturnsDescription()
    {
        var config = new TelegramConfig { BotToken = "token", ChatId = "123" };
        var factory = new FakeHttpClientFactory();
        var channel = new TelegramChannel(config, factory, NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: true);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("[DRY RUN]");
        result.Message.Should().Contain("123");
    }

    [Fact]
    public async Task SendAsync_Success_ReturnsOk()
    {
        var config = new TelegramConfig { BotToken = "123:ABC", ChatId = "456" };
        // Telegram Bot API returns JSON with ok:true and a result object
        var responseBody = """{"ok":true,"result":{"message_id":1,"chat":{"id":456},"date":0,"text":"test"}}""";
        var factory = new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
        });
        var channel = new TelegramChannel(config, factory, NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: false);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Telegram message sent");
    }

    [Fact]
    public async Task SendAsync_HttpError_ReturnsFail()
    {
        var config = new TelegramConfig { BotToken = "bad", ChatId = "456" };
        var factory = new FakeHttpClientFactory(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("""{"ok":false,"description":"Unauthorized"}""", System.Text.Encoding.UTF8, "application/json")
        });
        var channel = new TelegramChannel(config, factory, NullLogger<TelegramChannel>.Instance);

        var result = await channel.SendAsync(TestSignal, dryRun: false);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Telegram error");
    }

    [Fact]
    public async Task ValidateConfigAsync_EmptyToken_ReturnsFalse()
    {
        var config = new TelegramConfig { BotToken = "", ChatId = "123" };
        var factory = new FakeHttpClientFactory();
        var channel = new TelegramChannel(config, factory, NullLogger<TelegramChannel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateConfigAsync_EmptyChatId_ReturnsFalse()
    {
        var config = new TelegramConfig { BotToken = "token", ChatId = "" };
        var factory = new FakeHttpClientFactory();
        var channel = new TelegramChannel(config, factory, NullLogger<TelegramChannel>.Instance);

        var valid = await channel.ValidateConfigAsync();

        valid.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_UsesCustomTemplate()
    {
        var config = new TelegramConfig
        {
            BotToken = "123:ABC",
            ChatId = "456",
            MessageTemplate = "TRADE: {Action} {Symbol}"
        };
        var responseBody = """{"ok":true,"result":{"message_id":1,"chat":{"id":456},"date":0,"text":"test"}}""";
        var handler = new FakeHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
        });
        var factory = new FakeHttpClientFactory(handler);
        var channel = new TelegramChannel(config, factory, NullLogger<TelegramChannel>.Instance);

        await channel.SendAsync(TestSignal, dryRun: false);

        handler.LastRequestBody.Should().Contain("TRADE: Buy EURUSD");
    }
}
