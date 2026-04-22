using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class SignalsViewModel : ObservableObject
{
    private readonly SignalRepository _signalRepo;
    private readonly SignalPipeline _pipeline;
    private readonly RuleRepository _ruleRepo;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private Signal? _selectedSignal;

    public ObservableCollection<Signal> Signals { get; } = [];

    public SignalsViewModel(
        SignalRepository signalRepo,
        SignalPipeline pipeline,
        RuleRepository ruleRepo)
    {
        _signalRepo = signalRepo;
        _pipeline = pipeline;
        _ruleRepo = ruleRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
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
    }

    partial void OnFilterTextChanged(string value)
    {
        _ = LoadAsync();
    }

    private bool MatchesFilter(Signal signal)
    {
        if (string.IsNullOrWhiteSpace(FilterText))
            return true;

        var filter = FilterText.Trim();
        return signal.Symbol.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || signal.Action.ToString().Contains(filter, StringComparison.OrdinalIgnoreCase)
            || signal.StrategyId.Contains(filter, StringComparison.OrdinalIgnoreCase)
            || signal.AlertId.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }
}
