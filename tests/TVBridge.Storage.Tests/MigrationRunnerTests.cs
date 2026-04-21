using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace TVBridge.Storage.Tests;

public sealed class MigrationRunnerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MigrationRunner _runner;

    public MigrationRunnerTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _runner = new MigrationRunner(NullLogger<MigrationRunner>.Instance);
    }

    [Fact]
    public async Task RunAsync_CreatesSchemaVersionTable()
    {
        await _runner.RunAsync(_connection);

        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='schema_version'");

        tables.Should().Contain("schema_version");
    }

    [Fact]
    public async Task RunAsync_AppliesV001_CreatesSignalsTable()
    {
        await _runner.RunAsync(_connection);

        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='signals'");

        tables.Should().Contain("signals");
    }

    [Fact]
    public async Task RunAsync_AppliesV001_CreatesRulesTable()
    {
        await _runner.RunAsync(_connection);

        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='rules'");

        tables.Should().Contain("rules");
    }

    [Fact]
    public async Task RunAsync_AppliesV001_CreatesChannelsTable()
    {
        await _runner.RunAsync(_connection);

        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='channels'");

        tables.Should().Contain("channels");
    }

    [Fact]
    public async Task RunAsync_AppliesV001_CreatesAuditLogTable()
    {
        await _runner.RunAsync(_connection);

        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='audit_log'");

        tables.Should().Contain("audit_log");
    }

    [Fact]
    public async Task RunAsync_AppliesV001_CreatesSettingsTable()
    {
        await _runner.RunAsync(_connection);

        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name='settings'");

        tables.Should().Contain("settings");
    }

    [Fact]
    public async Task RunAsync_RecordsAppliedVersion()
    {
        await _runner.RunAsync(_connection);

        var versions = (await _connection.QueryAsync<int>("SELECT version FROM schema_version")).ToList();

        versions.Should().Contain(1);
    }

    [Fact]
    public async Task RunAsync_Idempotent_DoesNotReapply()
    {
        await _runner.RunAsync(_connection);
        await _runner.RunAsync(_connection);

        var count = await _connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM schema_version WHERE version = 1");

        count.Should().Be(1);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
