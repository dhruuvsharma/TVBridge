using System.Windows;
using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class TelegramPage : UserControl
{
    public TelegramPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.TelegramViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
