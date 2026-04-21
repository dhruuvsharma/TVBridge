using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using TVBridge.Core;
using TVBridge.Storage.Repositories;
using Xunit;

namespace TVBridge.Storage.Tests;

public sealed class SignalRepositoryTests : IDisposable
{
    private readonly DatabaseManager _db;
    private readonly SignalRepository _repo;
    private readonly SqliteConnection _keepAlive;

    public SignalRepositoryTests()
    {
        // In-memory SQLite needs a persistent connection to keep the DB alive
        _keepAlive = new SqliteConnection("Data Source=SignalRepoTest;Mode=Memory;Cache=Shared");
        _keepAlive.Open();

        _db = new DatabaseManager(
            "Data Source=SignalRepoTest;Mode=Memory;Cache=Shared",
            new MigrationRunner(NullLogger<MigrationRunner>.Instance),
            NullLogger<DatabaseManager>.Instance);
        _db.InitializeAsync().GetAwaiter().GetResult();
        _repo = new SignalRepository(_db);
    }

    private static Signal CreateTestSignal(string alertId = "test-1") => new()
    {
        AlertId = alertId,
        StrategyId = "MA_CROSS",
        AccountTag = "default",
        Symbol = "EURUSD",
        Action = SignalAction.Buy,
        OrderType = OrderType.Market,
        StopLoss = 1.0850m,
        TakeProfit = 1.1050m,
        LotSize = 0.1m,
        Timeframe = "1H",
        Timestamp = DateTimeOffset.Parse("2026-04-22T12:00:00Z"),
        Comment = "Test signal",
        Secret = "secret",
        ReceivedAt = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task InsertAsync_ReturnsNewId()
    {
        var id = await _repo.InsertAsync(CreateTestSignal());
        id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsInsertedSignal()
    {
        var id = await _repo.InsertAsync(CreateTestSignal("lookup-1"));
        var signal = await _repo.GetByIdAsync(id);

        signal.Should().NotBeNull();
        signal!.AlertId.Should().Be("lookup-1");
        signal.StrategyId.Should().Be("MA_CROSS");
        signal.Action.Should().Be(SignalAction.Buy);
        signal.StopLoss.Should().Be(1.0850m);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var signal = await _repo.GetByIdAsync(9999);
        signal.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentAsync_ReturnsOrderedByReceivedAt()
    {
        await _repo.InsertAsync(CreateTestSignal("first"));
        await _repo.InsertAsync(CreateTestSignal("second"));

        var signals = await _repo.GetRecentAsync(10);

        signals.Should().HaveCount(2);
        signals[0].AlertId.Should().Be("second"); // Most recent first
    }

    [Fact]
    public async Task MarkProcessedAsync_UpdatesFlag()
    {
        var id = await _repo.InsertAsync(CreateTestSignal());
        await _repo.MarkProcessedAsync(id);

        var signal = await _repo.GetByIdAsync(id);
        signal!.Processed.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_SecretIsEmpty()
    {
        var id = await _repo.InsertAsync(CreateTestSignal());
        var signal = await _repo.GetByIdAsync(id);

        // Secret should never be read back from DB
        signal!.Secret.Should().BeEmpty();
    }

    public void Dispose()
    {
        _db.Dispose();
        _keepAlive.Dispose();
    }
}
