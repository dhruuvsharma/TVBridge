using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TVBridge.Core;
using TVBridge.Storage.Repositories;

namespace TVBridge.App.ViewModels;

public sealed partial class RulesViewModel : ObservableObject
{
    private readonly RuleRepository _ruleRepo;

    [ObservableProperty]
    private Rule? _selectedRule;

    // Edit form fields
    [ObservableProperty]
    private string _editName = string.Empty;

    [ObservableProperty]
    private string _editStrategyId = string.Empty;

    [ObservableProperty]
    private string _editSymbol = string.Empty;

    [ObservableProperty]
    private string _editAction = string.Empty;

    [ObservableProperty]
    private string _editAccountTag = string.Empty;

    [ObservableProperty]
    private string _editTimeframe = string.Empty;

    [ObservableProperty]
    private string _editDestinationIds = string.Empty;

    [ObservableProperty]
    private int _editPriority;

    [ObservableProperty]
    private bool _editContinueOnMatch;

    [ObservableProperty]
    private bool _editEnabled = true;

    public ObservableCollection<Rule> Rules { get; } = [];

    public RulesViewModel(RuleRepository ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var rules = await _ruleRepo.GetAllAsync().ConfigureAwait(false);
        Rules.Clear();
        foreach (var rule in rules.OrderBy(r => r.Priority))
            Rules.Add(rule);
    }

    [RelayCommand]
    private async Task SaveRuleAsync()
    {
        if (string.IsNullOrWhiteSpace(EditName)) return;

        var rule = new Rule
        {
            Id = SelectedRule?.Id ?? 0,
            Name = EditName,
            StrategyId = NullIfEmpty(EditStrategyId),
            Symbol = NullIfEmpty(EditSymbol),
            Action = NullIfEmpty(EditAction),
            AccountTag = NullIfEmpty(EditAccountTag),
            Timeframe = NullIfEmpty(EditTimeframe),
            DestinationIds = EditDestinationIds,
            Priority = EditPriority,
            ContinueOnMatch = EditContinueOnMatch,
            Enabled = EditEnabled
        };

        if (rule.Id == 0)
            await _ruleRepo.InsertAsync(rule).ConfigureAwait(false);
        else
            await _ruleRepo.UpdateAsync(rule).ConfigureAwait(false);

        await LoadAsync().ConfigureAwait(false);
        ClearForm();
    }

    [RelayCommand]
    private async Task DeleteRuleAsync()
    {
        if (SelectedRule is null) return;
        await _ruleRepo.DeleteAsync(SelectedRule.Id).ConfigureAwait(false);
        await LoadAsync().ConfigureAwait(false);
        ClearForm();
    }

    [RelayCommand]
    private async Task MoveUpAsync()
    {
        if (SelectedRule is null) return;
        var idx = Rules.IndexOf(SelectedRule);
        if (idx <= 0) return;

        var above = Rules[idx - 1];
        var current = SelectedRule;

        await _ruleRepo.UpdateAsync(current with { Priority = above.Priority }).ConfigureAwait(false);
        await _ruleRepo.UpdateAsync(above with { Priority = current.Priority }).ConfigureAwait(false);
        await LoadAsync().ConfigureAwait(false);
    }

    [RelayCommand]
    private async Task MoveDownAsync()
    {
        if (SelectedRule is null) return;
        var idx = Rules.IndexOf(SelectedRule);
        if (idx < 0 || idx >= Rules.Count - 1) return;

        var below = Rules[idx + 1];
        var current = SelectedRule;

        await _ruleRepo.UpdateAsync(current with { Priority = below.Priority }).ConfigureAwait(false);
        await _ruleRepo.UpdateAsync(below with { Priority = current.Priority }).ConfigureAwait(false);
        await LoadAsync().ConfigureAwait(false);
    }

    partial void OnSelectedRuleChanged(Rule? value)
    {
        if (value is null) { ClearForm(); return; }
        EditName = value.Name;
        EditStrategyId = value.StrategyId ?? "";
        EditSymbol = value.Symbol ?? "";
        EditAction = value.Action ?? "";
        EditAccountTag = value.AccountTag ?? "";
        EditTimeframe = value.Timeframe ?? "";
        EditDestinationIds = value.DestinationIds;
        EditPriority = value.Priority;
        EditContinueOnMatch = value.ContinueOnMatch;
        EditEnabled = value.Enabled;
    }

    private void ClearForm()
    {
        SelectedRule = null;
        EditName = "";
        EditStrategyId = "";
        EditSymbol = "";
        EditAction = "";
        EditAccountTag = "";
        EditTimeframe = "";
        EditDestinationIds = "";
        EditPriority = Rules.Count;
        EditContinueOnMatch = false;
        EditEnabled = true;
    }

    private static string? NullIfEmpty(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
