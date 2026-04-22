using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;

    [ObservableProperty]
    private bool _globalDryRun;

    [ObservableProperty]
    private string _logLevel = "Information";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public SettingsViewModel(SettingsRepository settings)
    {
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        GlobalDryRun = await _settings.GetAsync("global_dry_run").ConfigureAwait(false) == "true";
        LogLevel = await _settings.GetAsync("log_level").ConfigureAwait(false) ?? "Information";
        StatusMessage = "Settings loaded";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settings.SetAsync("global_dry_run", GlobalDryRun.ToString().ToLowerInvariant()).ConfigureAwait(false);
        await _settings.SetAsync("log_level", LogLevel).ConfigureAwait(false);
        StatusMessage = "Settings saved";
    }
}
