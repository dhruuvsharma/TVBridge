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
