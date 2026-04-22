using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Channels.Telegram;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class TelegramViewModel : ObservableObject
{
    private readonly TelegramChannel _channel;
    private readonly SettingsRepository _settings;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _botToken = string.Empty;

    [ObservableProperty]
    private string _chatId = string.Empty;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public TelegramViewModel(TelegramChannel channel, SettingsRepository settings)
    {
        _channel = channel;
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var enabledStr = await _settings.GetAsync("channel_Telegram_enabled").ConfigureAwait(false);
        IsEnabled = enabledStr != "false";
        BotToken = await _settings.GetAsync("telegram_bot_token").ConfigureAwait(false) ?? "";
        ChatId = await _settings.GetAsync("telegram_chat_id").ConfigureAwait(false) ?? "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settings.SetAsync("telegram_bot_token", BotToken).ConfigureAwait(false);
        await _settings.SetAsync("telegram_chat_id", ChatId).ConfigureAwait(false);
        StatusMessage = "Telegram settings saved";
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var valid = await _channel.ValidateConfigAsync().ConfigureAwait(false);
        TestResult = valid ? "Telegram configuration valid" : "Invalid — check bot token and chat ID";
    }

    [RelayCommand]
    private async Task TestSendAsync()
    {
        var testSignal = new Signal
        {
            AlertId = "test-tg-" + Guid.NewGuid().ToString()[..8],
            StrategyId = "test", AccountTag = "test",
            Symbol = "EURUSD", Action = SignalAction.Buy,
            OrderType = OrderType.Market, LotSize = 0.01m,
            Timeframe = "1H", Timestamp = DateTimeOffset.UtcNow, Secret = ""
        };
        var result = await _channel.SendAsync(testSignal, dryRun: true).ConfigureAwait(false);
        TestResult = result.Message;
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync()
    {
        IsEnabled = !IsEnabled;
        await _settings.SetAsync("channel_Telegram_enabled", IsEnabled.ToString().ToLowerInvariant()).ConfigureAwait(false);
        StatusMessage = IsEnabled ? "Telegram enabled" : "Telegram disabled";
    }
}
