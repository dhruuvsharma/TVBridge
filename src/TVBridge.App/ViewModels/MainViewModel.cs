using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TVBridge.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedPage = "Dashboard";

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private void NavigateTo(string page)
    {
        SelectedPage = page;
    }
}
