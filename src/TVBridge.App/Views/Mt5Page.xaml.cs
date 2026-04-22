using System.Windows;
using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class Mt5Page : UserControl
{
    public Mt5Page()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.Mt5ViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
