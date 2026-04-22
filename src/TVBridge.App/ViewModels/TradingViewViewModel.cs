using System.Collections.ObjectModel;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class TradingViewViewModel : ObservableObject
{
    private readonly SettingsRepository _settings;
    private readonly SignalRepository _signalRepo;
    private readonly SignalPipeline _pipeline;
    private readonly RuleRepository _ruleRepo;

    [ObservableProperty]
    private string _webhookSecret = string.Empty;

    [ObservableProperty]
    private int _webhookPort = 5555;

    [ObservableProperty]
    private string _tvTemplate = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private Signal? _selectedSignal;

    public ObservableCollection<Signal> Signals { get; } = [];

    public TradingViewViewModel(
        SettingsRepository settings,
        SignalRepository signalRepo,
        SignalPipeline pipeline,
        RuleRepository ruleRepo)
    {
        _settings = settings;
        _signalRepo = signalRepo;
        _pipeline = pipeline;
        _ruleRepo = ruleRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        WebhookSecret = await _settings.GetAsync("webhook_secret").ConfigureAwait(false) ?? "";
        var portStr = await _settings.GetAsync("webhook_port").ConfigureAwait(false);
        if (int.TryParse(portStr, out var port))
            WebhookPort = port;
        GenerateTvTemplate();
        await LoadSignalsAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task LoadSignalsAsync()
    {
        var signals = await _signalRepo.GetRecentAsync(100).ConfigureAwait(false);
        Signals.Clear();
        foreach (var signal in signals)
        {
            if (MatchesFilter(signal))
                Signals.Add(signal);
        }
    }

    [RelayCommand]
    private async Task ReplayAsync()
    {
        if (SelectedSignal is null) return;
        var rules = await _ruleRepo.GetAllEnabledAsync().ConfigureAwait(false);
        await _pipeline.ProcessAsync(SelectedSignal, rules, globalDryRun: true).ConfigureAwait(false);
        StatusMessage = "Replay complete (dry run)";
    }

    [RelayCommand]
    private async Task GenerateSecretAsync()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        WebhookSecret = Convert.ToBase64String(bytes);
        await _settings.SetAsync("webhook_secret", WebhookSecret).ConfigureAwait(false);
        GenerateTvTemplate();
        StatusMessage = "New secret generated and saved";
    }

    [RelayCommand]
    private async Task SaveWebhookAsync()
    {
        if (!string.IsNullOrWhiteSpace(WebhookSecret))
            await _settings.SetAsync("webhook_secret", WebhookSecret).ConfigureAwait(false);
        await _settings.SetAsync("webhook_port", WebhookPort.ToString()).ConfigureAwait(false);
        StatusMessage = "Webhook settings saved";
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

    partial void OnFilterTextChanged(string value)
    {
        _ = LoadSignalsAsync();
    }

    private bool MatchesFilter(Signal signal)
    {
        if (string.IsNullOrWhiteSpace(FilterText)) return true;
        var f = FilterText.Trim();
        return signal.Symbol.Contains(f, StringComparison.OrdinalIgnoreCase)
            || signal.Action.ToString().Contains(f, StringComparison.OrdinalIgnoreCase)
            || signal.StrategyId.Contains(f, StringComparison.OrdinalIgnoreCase)
            || signal.AlertId.Contains(f, StringComparison.OrdinalIgnoreCase);
    }
}
