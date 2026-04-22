using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class ChannelsPage : UserControl
{
    public ChannelsPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ChannelsViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
