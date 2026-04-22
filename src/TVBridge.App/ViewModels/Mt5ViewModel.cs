using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Channels.Mt5;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class Mt5ViewModel : ObservableObject
{
    private readonly Mt5Channel _channel;
    private readonly Mt5SidecarManager _sidecarManager;
    private readonly SettingsRepository _settings;

    [ObservableProperty]
    private string _sidecarStatus = "Stopped";

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _testResult = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public Mt5ViewModel(
        Mt5Channel channel,
        Mt5SidecarManager sidecarManager,
        SettingsRepository settings)
    {
        _channel = channel;
        _sidecarManager = sidecarManager;
        _settings = settings;

        _sidecarManager.StatusChanged += (_, status) =>
        {
            SidecarStatus = status.ToString();
        };
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        SidecarStatus = _sidecarManager.Status.ToString();
        var enabledStr = await _settings.GetAsync("channel_MT5_enabled").ConfigureAwait(false);
        IsEnabled = enabledStr != "false";
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        var valid = await _channel.ValidateConfigAsync().ConfigureAwait(false);
        TestResult = valid ? "MT5 connection valid" : "MT5 connection failed — is terminal running?";
    }

    [RelayCommand]
    private async Task TestSendAsync()
    {
        var testSignal = new Signal
        {
            AlertId = "test-mt5-" + Guid.NewGuid().ToString()[..8],
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
        await _settings.SetAsync("channel_MT5_enabled", IsEnabled.ToString().ToLowerInvariant()).ConfigureAwait(false);
        StatusMessage = IsEnabled ? "MT5 channel enabled" : "MT5 channel disabled";
    }
}
