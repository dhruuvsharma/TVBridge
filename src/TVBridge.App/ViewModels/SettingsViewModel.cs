using System.Security.Cryptography;
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
    private string _webhookSecret = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _webhookPort = 5555;

    [ObservableProperty]
    private string _tvTemplate = string.Empty;

    public SettingsViewModel(SettingsRepository settings)
    {
        _settings = settings;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        GlobalDryRun = await _settings.GetAsync("global_dry_run").ConfigureAwait(false) == "true";
        LogLevel = await _settings.GetAsync("log_level").ConfigureAwait(false) ?? "Information";
        WebhookSecret = await _settings.GetAsync("webhook_secret").ConfigureAwait(false) ?? "";

        var portStr = await _settings.GetAsync("webhook_port").ConfigureAwait(false);
        if (int.TryParse(portStr, out var port))
            WebhookPort = port;

        GenerateTvTemplate();
        StatusMessage = "Settings loaded";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settings.SetAsync("global_dry_run", GlobalDryRun.ToString().ToLowerInvariant()).ConfigureAwait(false);
        await _settings.SetAsync("log_level", LogLevel).ConfigureAwait(false);
        await _settings.SetAsync("webhook_port", WebhookPort.ToString()).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(WebhookSecret))
            await _settings.SetAsync("webhook_secret", WebhookSecret).ConfigureAwait(false);

        StatusMessage = "Settings saved";
    }

    [RelayCommand]
    private async Task GenerateSecretAsync()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        WebhookSecret = Convert.ToBase64String(bytes);
        await _settings.SetAsync("webhook_secret", WebhookSecret).ConfigureAwait(false);
        GenerateTvTemplate();
        StatusMessage = "New webhook secret generated and saved";
    }

    [RelayCommand]
    private void GenerateTvTemplate()
    {
        var secret = string.IsNullOrEmpty(WebhookSecret) ? "YOUR_SECRET_HERE" : WebhookSecret;
        TvTemplate = "{\n" +
            "  \"alert_id\": \"{{strategy.order.id}}\",\n" +
            "  \"strategy_id\": \"{{strategy.name}}\",\n" +
            "  \"account_tag\": \"default\",\n" +
            "  \"symbol\": \"{{ticker}}\",\n" +
            "  \"action\": \"{{strategy.order.action}}\",\n" +
            "  \"order_type\": \"MARKET\",\n" +
            "  \"entry_price\": {{strategy.order.price}},\n" +
            "  \"stop_loss\": null,\n" +
            "  \"take_profit\": null,\n" +
            "  \"lot_size\": 0.01,\n" +
            "  \"risk_percent\": null,\n" +
            "  \"timeframe\": \"{{interval}}\",\n" +
            "  \"timestamp\": \"{{timenow}}\",\n" +
            "  \"comment\": \"{{strategy.order.comment}}\",\n" +
            $"  \"secret\": \"{secret}\"\n" +
            "}";
    }
}
