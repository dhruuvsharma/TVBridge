using System.Windows;
using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class NinjaTraderPage : UserControl
{
    public NinjaTraderPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.NinjaTraderViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
