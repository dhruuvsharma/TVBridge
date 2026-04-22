using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Tunnel;

namespace TVBridge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedPage = "Dashboard";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string? _tunnelUrl;

    [ObservableProperty]
    private string _tunnelStatusText = "Tunnel: Stopped";

    public DashboardViewModel Dashboard { get; }
    public SignalsViewModel Signals { get; }
    public RulesViewModel Rules { get; }
    public ChannelsViewModel Channels { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel(
        DashboardViewModel dashboard,
        SignalsViewModel signals,
        RulesViewModel rules,
        ChannelsViewModel channels,
        SettingsViewModel settings)
    {
        Dashboard = dashboard;
        Signals = signals;
        Rules = rules;
        Channels = channels;
        Settings = settings;
    }

    [RelayCommand]
    private void NavigateTo(string page)
    {
        SelectedPage = page;
    }

    public void UpdateTunnelStatus(TunnelStatus status, string? url)
    {
        TunnelUrl = url;
        TunnelStatusText = status switch
        {
            TunnelStatus.Stopped => "Tunnel: Stopped",
            TunnelStatus.Starting => "Tunnel: Starting...",
            TunnelStatus.Running => $"Tunnel: {url ?? "Running"}",
            TunnelStatus.Error => "Tunnel: Error",
            TunnelStatus.Downloading => "Tunnel: Downloading cloudflared...",
            _ => "Tunnel: Unknown"
        };
        StatusText = TunnelStatusText;
    }
}
