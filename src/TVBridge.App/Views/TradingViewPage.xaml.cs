using System.Windows;
using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class TradingViewPage : UserControl
{
    public TradingViewPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.TradingViewViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
