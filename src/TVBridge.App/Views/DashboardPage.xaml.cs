using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.DashboardViewModel vm)
            await vm.RefreshCommand.ExecuteAsync(null);
    }
}
