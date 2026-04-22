using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Channels.NinjaTrader;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class NinjaTraderViewModel : ObservableObject
{
    private readonly NinjaTraderChannel _channel;
    private readonly SettingsRepository _settings;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _host = "127.0.0.1";

    [ObservableProperty]
    private int _port = 36973;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public NinjaTraderViewModel(NinjaTraderChannel channel, SettingsRepository settings)
    {
        _channel = channel;
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var enabledStr = await _settings.GetAsync("channel_NinjaTrader_enabled").ConfigureAwait(false);
        IsEnabled = enabledStr != "false";
        Host = await _settings.GetAsync("nt_host").ConfigureAwait(false) ?? "127.0.0.1";
        var portStr = await _settings.GetAsync("nt_port").ConfigureAwait(false);
        if (int.TryParse(portStr, out var p)) Port = p;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settings.SetAsync("nt_host", Host).ConfigureAwait(false);
        await _settings.SetAsync("nt_port", Port.ToString()).ConfigureAwait(false);
        StatusMessage = "NinjaTrader settings saved";
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var valid = await _channel.ValidateConfigAsync().ConfigureAwait(false);
        TestResult = valid ? "NinjaTrader ATI connected" : "Cannot connect — is NT8 running with ATI enabled?";
    }

    [RelayCommand]
    private async Task TestSendAsync()
    {
        var testSignal = new Signal
        {
            AlertId = "test-nt-" + Guid.NewGuid().ToString()[..8],
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
        await _settings.SetAsync("channel_NinjaTrader_enabled", IsEnabled.ToString().ToLowerInvariant()).ConfigureAwait(false);
        StatusMessage = IsEnabled ? "NinjaTrader enabled" : "NinjaTrader disabled";
    }
}
