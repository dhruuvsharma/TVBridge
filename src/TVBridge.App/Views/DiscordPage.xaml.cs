using System.Windows;
using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class DiscordPage : UserControl
{
    public DiscordPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DiscordViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
