using Dapper;
using TVBridge.Core;

namespace TVBridge.Storage.Repositories;

public sealed class RuleRepository
{
    private readonly DatabaseManager _db;

    public RuleRepository(DatabaseManager db)
    {
        _db = db;
    }

    public async Task<int> InsertAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO rules (name, strategy_id, symbol, action, account_tag, timeframe,
                               destination_ids, priority, continue_on_match, dry_run_override,
                               lot_multiplier, enabled)
            VALUES (@Name, @StrategyId, @Symbol, @Action, @AccountTag, @Timeframe,
                    @DestinationIds, @Priority, @ContinueOnMatch, @DryRunOverride,
                    @LotMultiplier, @Enabled);
            SELECT last_insert_rowid();
            """, new
        {
            rule.Name,
            rule.StrategyId,
            rule.Symbol,
            rule.Action,
            rule.AccountTag,
            rule.Timeframe,
            rule.DestinationIds,
            rule.Priority,
            ContinueOnMatch = rule.ContinueOnMatch ? 1 : 0,
            DryRunOverride = rule.DryRunOverride.HasValue ? (rule.DryRunOverride.Value ? 1 : 0) : (int?)null,
            LotMultiplier = (double)rule.LotMultiplier,
            Enabled = rule.Enabled ? 1 : 0
        }).ConfigureAwait(false);
    }

    public async Task<Rule?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM rules WHERE id = @id", new { id }).ConfigureAwait(false);

        if (row is null)
            return null;

        return MapToRule(row);
    }

    public async Task<IReadOnlyList<Rule>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<dynamic>(
            "SELECT * FROM rules WHERE enabled = 1 ORDER BY priority ASC").ConfigureAwait(false);

        return rows.Select(MapToRule).ToList();
    }

    public async Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<dynamic>(
            "SELECT * FROM rules ORDER BY priority ASC").ConfigureAwait(false);

        return rows.Select(MapToRule).ToList();
    }

    public async Task UpdateAsync(Rule rule, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync("""
            UPDATE rules SET name = @Name, strategy_id = @StrategyId, symbol = @Symbol,
                             action = @Action, account_tag = @AccountTag, timeframe = @Timeframe,
                             destination_ids = @DestinationIds, priority = @Priority,
                             continue_on_match = @ContinueOnMatch, dry_run_override = @DryRunOverride,
                             lot_multiplier = @LotMultiplier, enabled = @Enabled
            WHERE id = @Id
            """, new
        {
            rule.Id,
            rule.Name,
            rule.StrategyId,
            rule.Symbol,
            rule.Action,
            rule.AccountTag,
            rule.Timeframe,
            rule.DestinationIds,
            rule.Priority,
            ContinueOnMatch = rule.ContinueOnMatch ? 1 : 0,
            DryRunOverride = rule.DryRunOverride.HasValue ? (rule.DryRunOverride.Value ? 1 : 0) : (int?)null,
            LotMultiplier = (double)rule.LotMultiplier,
            Enabled = rule.Enabled ? 1 : 0
        }).ConfigureAwait(false);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync("DELETE FROM rules WHERE id = @id", new { id }).ConfigureAwait(false);
    }

    private static Rule MapToRule(dynamic row)
    {
        return new Rule
        {
            Id = (int)(long)row.id,
            Name = (string)row.name,
            StrategyId = (string?)row.strategy_id,
            Symbol = (string?)row.symbol,
            Action = (string?)row.action,
            AccountTag = (string?)row.account_tag,
            Timeframe = (string?)row.timeframe,
            DestinationIds = (string)row.destination_ids,
            Priority = (int)(long)row.priority,
            ContinueOnMatch = (long)row.continue_on_match == 1,
            DryRunOverride = row.dry_run_override is null ? null : (bool?)((long)row.dry_run_override == 1),
            LotMultiplier = (decimal)(double)row.lot_multiplier,
            Enabled = (long)row.enabled == 1
        };
    }
}
