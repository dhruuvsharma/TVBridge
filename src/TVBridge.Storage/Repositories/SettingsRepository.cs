using Dapper;

namespace TVBridge.Storage.Repositories;

public sealed class SettingsRepository
{
    private readonly DatabaseManager _db;

    public SettingsRepository(DatabaseManager db)
    {
        _db = db;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var encrypted = await connection.ExecuteScalarAsync<byte[]?>(
            "SELECT encrypted_value FROM settings WHERE key = @key", new { key }).ConfigureAwait(false);

        if (encrypted is null)
            return null;

        return DpapiHelper.Decrypt(encrypted);
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        var encrypted = DpapiHelper.Encrypt(value);

        await connection.ExecuteAsync("""
            INSERT INTO settings (key, encrypted_value) VALUES (@key, @encrypted)
            ON CONFLICT(key) DO UPDATE SET encrypted_value = @encrypted
            """, new { key, encrypted }).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var connection = await _db.GetConnectionAsync(cancellationToken).ConfigureAwait(false);
        await connection.ExecuteAsync(
            "DELETE FROM settings WHERE key = @key", new { key }).ConfigureAwait(false);
    }
}
