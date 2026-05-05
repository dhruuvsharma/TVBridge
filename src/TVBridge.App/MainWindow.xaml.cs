using System.Windows;
using System.Windows.Input;
using TVBridge.App.ViewModels;

namespace TVBridge.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Mt5Anchor.Hide();
        TelegramAnchor.Hide();
        DiscordAnchor.Hide();
        NtAnchor.Hide();
        RulesAnchor.Hide();
        SettingsAnchor.Hide();
    }

    private void CopyTunnelUrl_Click(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.TunnelUrl is not null)
        {
            Clipboard.SetText(vm.TunnelUrl);
            vm.StatusText = "Tunnel URL copied to clipboard!";
        }
    }
}
