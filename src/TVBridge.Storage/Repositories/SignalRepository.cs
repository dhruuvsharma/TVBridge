using Dapper;
using TVBridge.Core;

namespace TVBridge.Storage.Repositories;

public sealed class SignalRepository
{
    private readonly DatabaseManager _db;

    public SignalRepository(DatabaseManager db)
    {
        _db = db;
    }

    public async Task<int> InsertAsync(Signal signal, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        return await connection.ExecuteScalarAsync<int>("""
            INSERT INTO signals (alert_id, strategy_id, account_tag, symbol, action, order_type,
                                 entry_price, stop_loss, take_profit, lot_size, risk_percent,
                                 timeframe, timestamp, comment, received_at, processed)
            VALUES (@AlertId, @StrategyId, @AccountTag, @Symbol, @Action, @OrderType,
                    @EntryPrice, @StopLoss, @TakeProfit, @LotSize, @RiskPercent,
                    @Timeframe, @Timestamp, @Comment, @ReceivedAt, @Processed);
            SELECT last_insert_rowid();
            """, new
        {
            signal.AlertId,
            signal.StrategyId,
            signal.AccountTag,
            signal.Symbol,
            Action = signal.Action.ToString(),
            OrderType = signal.OrderType.ToString(),
            signal.EntryPrice,
            signal.StopLoss,
            signal.TakeProfit,
            signal.LotSize,
            signal.RiskPercent,
            signal.Timeframe,
            Timestamp = signal.Timestamp.ToString("o"),
            signal.Comment,
            ReceivedAt = signal.ReceivedAt == default ? DateTimeOffset.UtcNow.ToString("o") : signal.ReceivedAt.ToString("o"),
            signal.Processed
        }).ConfigureAwait(false);
    }

    public async Task<Signal?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var row = await connection.QuerySingleOrDefaultAsync<dynamic>(
            "SELECT * FROM signals WHERE id = @id", new { id }).ConfigureAwait(false);

        if (row is null)
            return null;

        return MapToSignal(row);
    }

    public async Task<IReadOnlyList<Signal>> GetRecentAsync(int count = 50, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection.QueryAsync<dynamic>(
            "SELECT * FROM signals ORDER BY received_at DESC LIMIT @count", new { count }).ConfigureAwait(false);

        return rows.Select(MapToSignal).ToList();
    }

    public async Task MarkProcessedAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            "UPDATE signals SET processed = 1 WHERE id = @id", new { id }).ConfigureAwait(false);
    }

    private static Signal MapToSignal(dynamic row)
    {
        return new Signal
        {
            Id = (int)(long)row.id,
            AlertId = (string)row.alert_id,
            StrategyId = (string)row.strategy_id,
            AccountTag = (string)row.account_tag,
            Symbol = (string)row.symbol,
            Action = Enum.Parse<SignalAction>((string)row.action, ignoreCase: true),
            OrderType = Enum.Parse<OrderType>((string)row.order_type, ignoreCase: true),
            EntryPrice = row.entry_price is null ? null : (decimal?)(double)row.entry_price,
            StopLoss = row.stop_loss is null ? null : (decimal?)(double)row.stop_loss,
            TakeProfit = row.take_profit is null ? null : (decimal?)(double)row.take_profit,
            LotSize = row.lot_size is null ? null : (decimal?)(double)row.lot_size,
            RiskPercent = row.risk_percent is null ? null : (decimal?)(double)row.risk_percent,
            Timeframe = (string)row.timeframe,
            Timestamp = DateTimeOffset.Parse((string)row.timestamp),
            Comment = (string?)row.comment,
            Secret = string.Empty, // Never read secret back from DB
            ReceivedAt = DateTimeOffset.Parse((string)row.received_at),
            Processed = (long)row.processed == 1
        };
    }
}
