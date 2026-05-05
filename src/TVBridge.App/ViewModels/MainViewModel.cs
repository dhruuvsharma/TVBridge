using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.App.Services;
using TVBridge.Tunnel;

namespace TVBridge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private string? _tunnelUrl;

    [ObservableProperty]
    private string _tunnelStatusText = "Tunnel: Stopped";

    [ObservableProperty]
    private bool _isDarkTheme = true;

    public DashboardViewModel Dashboard { get; }
    public Mt5ViewModel Mt5 { get; }
    public TelegramViewModel Telegram { get; }
    public DiscordViewModel Discord { get; }
    public NinjaTraderViewModel NinjaTrader { get; }
    public RulesViewModel Rules { get; }
    public SettingsViewModel Settings { get; }
    public ThemeService ThemeService { get; }

    public MainViewModel(
        DashboardViewModel dashboard,
        Mt5ViewModel mt5,
        TelegramViewModel telegram,
        DiscordViewModel discord,
        NinjaTraderViewModel ninjaTrader,
        RulesViewModel rules,
        SettingsViewModel settings,
        ThemeService themeService)
    {
        Dashboard = dashboard;
        Mt5 = mt5;
        Telegram = telegram;
        Discord = discord;
        NinjaTrader = ninjaTrader;
        Rules = rules;
        Settings = settings;
        ThemeService = themeService;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ThemeService.ToggleTheme();
        IsDarkTheme = ThemeService.IsDarkTheme;
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
