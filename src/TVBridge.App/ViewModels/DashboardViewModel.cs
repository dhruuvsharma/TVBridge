using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Core;
using TVBridge.Storage.Repositories;
using TVBridge.Tunnel;

namespace TVBridge.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly SignalRepository _signalRepo;
    private readonly SignalPipeline _pipeline;
    private readonly RuleRepository _ruleRepo;
    private readonly SettingsRepository _settings;
    private readonly CloudflaredManager _tunnelManager;
    private readonly UpdateChecker _updateChecker;

    // Status
    [ObservableProperty]
    private string _tunnelStatus = "Stopped";

    [ObservableProperty]
    private string? _tunnelUrl;

    [ObservableProperty]
    private string? _tunnelError;

    [ObservableProperty]
    private int _signalCount;

    [ObservableProperty]
    private string? _updateMessage;

    // Webhook / TradingView
    [ObservableProperty]
    private string _webhookSecret = string.Empty;

    [ObservableProperty]
    private int _webhookPort = 5555;

    [ObservableProperty]
    private string _tvTemplate = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    // Signal filter
    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private Signal? _selectedSignal;

    // Log viewer
    [ObservableProperty]
    private string _logContent = string.Empty;

    public ObservableCollection<Signal> RecentSignals { get; } = [];

    public DashboardViewModel(
        SignalRepository signalRepo,
        SignalPipeline pipeline,
        RuleRepository ruleRepo,
        SettingsRepository settings,
        CloudflaredManager tunnelManager,
        UpdateChecker updateChecker)
    {
        _signalRepo = signalRepo;
        _pipeline = pipeline;
        _ruleRepo = ruleRepo;
        _settings = settings;
        _tunnelManager = tunnelManager;
        _updateChecker = updateChecker;

        _tunnelManager.StatusChanged += (_, status) =>
        {
            TunnelStatus = status.ToString();
            TunnelUrl = _tunnelManager.TunnelUrl;
            TunnelError = status == Tunnel.TunnelStatus.Error ? _tunnelManager.LastError : null;
        };
    }

    [RelayCommand]
    private async Task RestartTunnelAsync()
    {
        StatusMessage = "Restarting tunnel...";
        await _tunnelManager.StopAsync().ConfigureAwait(false);
        try
        {
            await _tunnelManager.StartAsync().ConfigureAwait(false);
            StatusMessage = "Tunnel restart initiated";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Tunnel restart failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Status
        TunnelStatus = _tunnelManager.Status.ToString();
        TunnelUrl = _tunnelManager.TunnelUrl;
        TunnelError = _tunnelManager.Status == Tunnel.TunnelStatus.Error ? _tunnelManager.LastError : null;

        // Webhook settings
        WebhookSecret = await _settings.GetAsync("webhook_secret").ConfigureAwait(false) ?? "";
        var portStr = await _settings.GetAsync("webhook_port").ConfigureAwait(false);
        if (int.TryParse(portStr, out var port)) WebhookPort = port;
        GenerateTvTemplate();

        // Signals
        await LoadSignalsAsync().ConfigureAwait(false);

        // Logs
        await LoadLogsAsync().ConfigureAwait(false);

        // Update check (best-effort)
        var update = await _updateChecker.CheckAsync().ConfigureAwait(false);
        UpdateMessage = update is not null ? $"New version available: v{update.Version}" : null;
    }

    [RelayCommand]
    private async Task LoadSignalsAsync()
    {
        var signals = await _signalRepo.GetRecentAsync(100).ConfigureAwait(false);
        RecentSignals.Clear();
        foreach (var signal in signals)
        {
            if (MatchesFilter(signal))
                RecentSignals.Add(signal);
        }
        SignalCount = RecentSignals.Count;
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

    [RelayCommand]
    private async Task LoadLogsAsync()
    {
        await Task.Run(() =>
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TVBridge", "logs");
            if (!Directory.Exists(logDir))
            {
                LogContent = "No log directory found.";
                return;
            }

            var latest = Directory.GetFiles(logDir, "tvbridge-*.log")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            if (latest is null)
            {
                LogContent = "No log files found.";
                return;
            }

            // Read last ~50 lines using shared read
            using var fs = new FileStream(latest, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs);
            var allLines = reader.ReadToEnd().Split('\n');
            var tail = allLines.Length > 50 ? allLines[^50..] : allLines;
            LogContent = string.Join('\n', tail);
        }).ConfigureAwait(false);
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
