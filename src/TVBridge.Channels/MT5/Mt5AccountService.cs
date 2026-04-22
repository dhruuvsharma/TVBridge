using System.Text.Json;
using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Channels.Mt5;

/// <summary>
/// Provides account data queries and real-time state streaming for the UI.
/// </summary>
public sealed class Mt5AccountService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IMt5Client _client;
    private readonly ILogger<Mt5AccountService> _logger;
    private CancellationTokenSource? _streamCts;
    private bool _disposed;

    public event EventHandler<Mt5AccountState>? AccountStateUpdated;

    public Mt5AccountService(IMt5Client client, ILogger<Mt5AccountService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(
        int account, string password, string server, string? path = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.SendCommandAsync("connect",
            new { account, password, server, path }, cancellationToken).ConfigureAwait(false);

        if (response.Success)
            _logger.LogInformation("MT5 connected to account {Account}", account);
        else
            _logger.LogWarning("MT5 connect failed: {Error}", response.Error);

        return response.Success;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await _client.SendCommandAsync("disconnect", cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        _logger.LogInformation("MT5 disconnected");
    }

    public async Task<Mt5AccountBalance?> GetBalanceAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.SendCommandAsync("get_balance", cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!response.Success)
        {
            _logger.LogWarning("Failed to get balance: {Error}", response.Error);
            return null;
        }

        return DeserializeData<Mt5AccountBalance>(response.Data);
    }

    public async Task<List<Mt5Position>> GetPositionsAsync(
        string? symbol = null, CancellationToken cancellationToken = default)
    {
        var parameters = symbol is not null ? new { symbol } : null;
        var response = await _client.SendCommandAsync("get_positions", parameters, cancellationToken)
            .ConfigureAwait(false);

        if (!response.Success)
        {
            _logger.LogWarning("Failed to get positions: {Error}", response.Error);
            return [];
        }

        return DeserializeData<List<Mt5Position>>(response.Data) ?? [];
    }

    public async Task<List<Mt5HistoryDeal>> GetHistoryAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var response = await _client.SendCommandAsync("get_history",
            new { from_date = from, to_date = to }, cancellationToken).ConfigureAwait(false);

        if (!response.Success)
        {
            _logger.LogWarning("Failed to get history: {Error}", response.Error);
            return [];
        }

        return DeserializeData<List<Mt5HistoryDeal>>(response.Data) ?? [];
    }

    public void StartStreaming(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        StopStreaming();

        _streamCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _client.StartAccountStateStream(
            state => AccountStateUpdated?.Invoke(this, state),
            _streamCts.Token);

        _logger.LogInformation("Started MT5 account state streaming");
    }

    public void StopStreaming()
    {
        _streamCts?.Cancel();
        _client.StopAccountStateStream();
        _streamCts?.Dispose();
        _streamCts = null;
    }

    private static T? DeserializeData<T>(JsonElement? data)
    {
        if (data is null) return default;
        return JsonSerializer.Deserialize<T>(data.Value.GetRawText(), JsonOptions);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopStreaming();
    }
}
