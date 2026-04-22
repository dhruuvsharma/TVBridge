using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Channels.Discord;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class DiscordViewModel : ObservableObject
{
    private readonly DiscordChannel _channel;
    private readonly SettingsRepository _settings;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _webhookUrl = string.Empty;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public DiscordViewModel(DiscordChannel channel, SettingsRepository settings)
    {
        _channel = channel;
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var enabledStr = await _settings.GetAsync("channel_Discord_enabled").ConfigureAwait(false);
        IsEnabled = enabledStr != "false";
        WebhookUrl = await _settings.GetAsync("discord_webhook_url").ConfigureAwait(false) ?? "";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settings.SetAsync("discord_webhook_url", WebhookUrl).ConfigureAwait(false);
        StatusMessage = "Discord settings saved";
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var valid = await _channel.ValidateConfigAsync().ConfigureAwait(false);
        TestResult = valid ? "Discord webhook valid" : "Invalid — check webhook URL";
    }

    [RelayCommand]
    private async Task TestSendAsync()
    {
        var testSignal = new Signal
        {
            AlertId = "test-dc-" + Guid.NewGuid().ToString()[..8],
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
        await _settings.SetAsync("channel_Discord_enabled", IsEnabled.ToString().ToLowerInvariant()).ConfigureAwait(false);
        StatusMessage = IsEnabled ? "Discord enabled" : "Discord disabled";
    }
}
