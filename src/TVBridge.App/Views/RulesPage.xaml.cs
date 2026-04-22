using System.Windows.Controls;

namespace TVBridge.App.Views;

public partial class RulesPage : UserControl
{
    public RulesPage()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.RulesViewModel vm)
            await vm.LoadCommand.ExecuteAsync(null);
    }
}
