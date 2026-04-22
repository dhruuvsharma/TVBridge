using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
