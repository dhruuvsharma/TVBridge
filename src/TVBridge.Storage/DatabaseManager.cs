using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace TVBridge.Storage;

public sealed class DatabaseManager : IDisposable
{
    private readonly string _connectionString;
    private readonly MigrationRunner _migrationRunner;
    private readonly ILogger<DatabaseManager> _logger;
    private SqliteConnection? _connection;

    public DatabaseManager(
        string connectionStringOrPath,
        MigrationRunner migrationRunner,
        ILogger<DatabaseManager> logger)
    {
        _connectionString = connectionStringOrPath.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
            ? connectionStringOrPath
            : $"Data Source={connectionStringOrPath}";
        _migrationRunner = migrationRunner;
        _logger = logger;
    }

    public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (_connection is { State: ConnectionState.Open })
            return _connection;

        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        // Enable WAL mode for better concurrency
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA foreign_keys=ON;";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return _connection;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing database...");
        var connection = await GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await _migrationRunner.RunAsync(connection, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Database initialization complete");
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connection = null;
    }
}
