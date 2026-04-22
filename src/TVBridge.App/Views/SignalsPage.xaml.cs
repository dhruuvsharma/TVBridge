using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class SignalsPage : UserControl
{
    public SignalsPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SignalsViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
