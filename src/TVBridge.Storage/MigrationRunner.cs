using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Logging;

namespace TVBridge.Storage;

public sealed partial class MigrationRunner
{
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(ILogger<MigrationRunner> logger)
    {
        _logger = logger;
    }

    public async Task RunAsync(IDbConnection connection, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaVersionTableAsync(connection).ConfigureAwait(false);

        var applied = (await connection.QueryAsync<int>(
            "SELECT version FROM schema_version ORDER BY version").ConfigureAwait(false)).ToHashSet();

        var migrations = LoadEmbeddedMigrations();

        foreach (var (version, name, upSql) in migrations.OrderBy(m => m.Version))
        {
            if (applied.Contains(version))
                continue;

            _logger.LogInformation("Applying migration V{Version}: {Name}", version, name);

            await connection.ExecuteAsync(upSql).ConfigureAwait(false);
            await connection.ExecuteAsync(
                "INSERT INTO schema_version (version, name, applied_at) VALUES (@version, @name, datetime('now'))",
                new { version, name }).ConfigureAwait(false);

            _logger.LogInformation("Migration V{Version} applied successfully", version);
        }
    }

    private static async Task EnsureSchemaVersionTableAsync(IDbConnection connection)
    {
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS schema_version (
                version    INTEGER PRIMARY KEY,
                name       TEXT NOT NULL,
                applied_at TEXT NOT NULL
            )
            """).ConfigureAwait(false);
    }

    private static List<(int Version, string Name, string UpSql)> LoadEmbeddedMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "TVBridge.Storage.Migrations.";
        var results = new List<(int Version, string Name, string UpSql)>();

        foreach (var resourceName in assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".sql", StringComparison.Ordinal)))
        {
            var fileName = resourceName[prefix.Length..];
            var match = MigrationFilePattern().Match(fileName);
            if (!match.Success)
                continue;

            var version = int.Parse(match.Groups[1].Value);
            var name = match.Groups[2].Value;

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            var fullSql = reader.ReadToEnd();

            var upSql = ExtractUpSection(fullSql);
            results.Add((version, name, upSql));
        }

        return results;
    }

    private static string ExtractUpSection(string sql)
    {
        var upIndex = sql.IndexOf("-- UP", StringComparison.OrdinalIgnoreCase);
        var downIndex = sql.IndexOf("-- DOWN", StringComparison.OrdinalIgnoreCase);

        if (upIndex < 0)
            return sql;

        var start = upIndex + "-- UP".Length;
        var length = downIndex > upIndex ? downIndex - start : sql.Length - start;
        return sql.Substring(start, length).Trim();
    }

    [GeneratedRegex(@"^V(\d+)_(.+)\.sql$")]
    private static partial Regex MigrationFilePattern();
}
