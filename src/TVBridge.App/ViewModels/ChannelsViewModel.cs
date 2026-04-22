using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class ChannelsViewModel : ObservableObject
{
    private readonly IEnumerable<IOutputChannel> _channels;
    private readonly SettingsRepository _settings;

    [ObservableProperty]
    private ChannelViewModel? _selectedChannel;

    [ObservableProperty]
    private string _testResult = string.Empty;

    public ObservableCollection<ChannelViewModel> Channels { get; } = [];

    public ChannelsViewModel(IEnumerable<IOutputChannel> channels, SettingsRepository settings)
    {
        _channels = channels;
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        Channels.Clear();
        foreach (var channel in _channels)
        {
            var enabledStr = await _settings.GetAsync($"channel_{channel.ChannelType}_enabled").ConfigureAwait(false);
            var enabled = enabledStr != "false"; // default to enabled
            Channels.Add(new ChannelViewModel
            {
                Name = channel.Name,
                ChannelType = channel.ChannelType,
                ChannelId = channel.ChannelId,
                IsEnabled = enabled,
                Channel = channel
            });
        }
    }

    [RelayCommand]
    private async Task TestSendAsync()
    {
        if (SelectedChannel?.Channel is null)
        {
            TestResult = "Select a channel first";
            return;
        }

        var testSignal = new Signal
        {
            AlertId = "test-" + Guid.NewGuid().ToString()[..8],
            StrategyId = "test-strategy",
            AccountTag = "test",
            Symbol = "EURUSD",
            Action = SignalAction.Buy,
            OrderType = OrderType.Market,
            LotSize = 0.01m,
            Timeframe = "1H",
            Timestamp = DateTimeOffset.UtcNow,
            Secret = ""
        };

        var result = await SelectedChannel.Channel.SendAsync(testSignal, dryRun: true).ConfigureAwait(false);
        TestResult = result.Message;
    }

    [RelayCommand]
    private async Task ValidateAsync()
    {
        if (SelectedChannel?.Channel is null)
        {
            TestResult = "Select a channel first";
            return;
        }

        var valid = await SelectedChannel.Channel.ValidateConfigAsync().ConfigureAwait(false);
        TestResult = valid ? "Configuration valid" : "Configuration invalid — check settings";
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync()
    {
        if (SelectedChannel is null) return;
        SelectedChannel.IsEnabled = !SelectedChannel.IsEnabled;
        await _settings.SetAsync(
            $"channel_{SelectedChannel.ChannelType}_enabled",
            SelectedChannel.IsEnabled.ToString().ToLowerInvariant()).ConfigureAwait(false);
    }
}

public sealed partial class ChannelViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _channelType = string.Empty;

    [ObservableProperty]
    private int _channelId;

    [ObservableProperty]
    private bool _isEnabled;

    public IOutputChannel? Channel { get; init; }
}
