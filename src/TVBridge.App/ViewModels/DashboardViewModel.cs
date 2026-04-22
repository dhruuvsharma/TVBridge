using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Channels.Mt5;
using TVBridge.Core;
using TVBridge.Storage.Repositories;
using TVBridge.Tunnel;

namespace TVBridge.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly SignalRepository _signalRepo;
    private readonly CloudflaredManager _tunnelManager;
    private readonly Mt5SidecarManager _mt5Manager;

    [ObservableProperty]
    private string _tunnelStatus = "Stopped";

    [ObservableProperty]
    private string? _tunnelUrl;

    [ObservableProperty]
    private string _mt5Status = "Stopped";

    [ObservableProperty]
    private int _signalCount;

    [ObservableProperty]
    private int _todaySignalCount;

    public ObservableCollection<Signal> RecentSignals { get; } = [];

    public DashboardViewModel(
        SignalRepository signalRepo,
        CloudflaredManager tunnelManager,
        Mt5SidecarManager mt5Manager)
    {
        _signalRepo = signalRepo;
        _tunnelManager = tunnelManager;
        _mt5Manager = mt5Manager;

        _tunnelManager.StatusChanged += (_, status) =>
        {
            TunnelStatus = status.ToString();
            TunnelUrl = _tunnelManager.TunnelUrl;
        };

        _mt5Manager.StatusChanged += (_, status) =>
        {
            Mt5Status = status.ToString();
        };
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        var signals = await _signalRepo.GetRecentAsync(10).ConfigureAwait(false);
        RecentSignals.Clear();
        foreach (var signal in signals)
            RecentSignals.Add(signal);
        SignalCount = signals.Count;

        TunnelStatus = _tunnelManager.Status.ToString();
        TunnelUrl = _tunnelManager.TunnelUrl;
        Mt5Status = _mt5Manager.Status.ToString();
    }
}
