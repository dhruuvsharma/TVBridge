using System.Text.Json;
using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Channels.Mt5;

/// <summary>
/// Output channel that routes signals to MetaTrader 5 via the Python sidecar.
/// </summary>
public sealed class Mt5Channel : IOutputChannel
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true
    };

    private readonly IMt5Client _client;
    private readonly ILogger<Mt5Channel> _logger;

    public string Name { get; }
    public string ChannelType => "MT5";
    public int ChannelId { get; }

    public Mt5Channel(
        IMt5Client client,
        ILogger<Mt5Channel> logger,
        string name = "MetaTrader 5",
        int channelId = 0)
    {
        _client = client;
        _logger = logger;
        Name = name;
        ChannelId = channelId;
    }

    public async Task<ChannelResult> SendAsync(Signal signal, bool dryRun, CancellationToken cancellationToken = default)
    {
        var description = DescribeAction(signal);

        if (dryRun)
            return ChannelResult.DryRun(description);

        try
        {
            return signal.Action switch
            {
                SignalAction.Buy or SignalAction.Sell => await PlaceOrderAsync(signal, cancellationToken).ConfigureAwait(false),
                SignalAction.Close => await CloseBySymbolAsync(signal, cancellationToken).ConfigureAwait(false),
                SignalAction.Modify => await ModifyBySymbolAsync(signal, cancellationToken).ConfigureAwait(false),
                _ => ChannelResult.Fail($"Unsupported action: {signal.Action}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MT5 channel error for signal {AlertId}", signal.AlertId);
            return ChannelResult.Fail($"MT5 error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.SendCommandAsync("ping", cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<ChannelResult> PlaceOrderAsync(Signal signal, CancellationToken cancellationToken)
    {
        var parameters = new
        {
            symbol = signal.Symbol,
            action = signal.Action.ToString(),
            order_type = signal.OrderType.ToString(),
            lot_size = signal.LotSize ?? 0.01m,
            entry_price = signal.EntryPrice,
            sl = signal.StopLoss,
            tp = signal.TakeProfit,
            comment = signal.Comment ?? $"TVBridge:{signal.AlertId}"
        };

        var response = await _client.SendCommandAsync("place_order", parameters, cancellationToken)
            .ConfigureAwait(false);

        if (!response.Success)
            return ChannelResult.Fail($"MT5 order failed: {response.Error}");

        var result = DeserializeData<Mt5OrderResult>(response.Data);
        return ChannelResult.Ok($"Order placed: ticket={result?.Ticket} {result?.Action} {result?.Volume} {result?.Symbol} @ {result?.Price}");
    }

    private async Task<ChannelResult> CloseBySymbolAsync(Signal signal, CancellationToken cancellationToken)
    {
        // TradingView doesn't know MT5 tickets, so we look up positions by symbol (FIFO)
        var posResponse = await _client.SendCommandAsync("get_positions",
            new { symbol = signal.Symbol }, cancellationToken).ConfigureAwait(false);

        if (!posResponse.Success)
            return ChannelResult.Fail($"Failed to get positions: {posResponse.Error}");

        var positions = DeserializeData<List<Mt5Position>>(posResponse.Data);
        if (positions is null || positions.Count == 0)
            return ChannelResult.Fail($"No open positions for {signal.Symbol}");

        // FIFO: close the oldest position
        var oldest = positions.OrderBy(p => p.Time).First();

        var closeResponse = await _client.SendCommandAsync("close",
            new { ticket = oldest.Ticket, lot_size = signal.LotSize }, cancellationToken).ConfigureAwait(false);

        if (!closeResponse.Success)
            return ChannelResult.Fail($"MT5 close failed: {closeResponse.Error}");

        var result = DeserializeData<Mt5OrderResult>(closeResponse.Data);
        return ChannelResult.Ok($"Position closed: ticket={oldest.Ticket} {result?.Symbol} @ {result?.Price}");
    }

    private async Task<ChannelResult> ModifyBySymbolAsync(Signal signal, CancellationToken cancellationToken)
    {
        var posResponse = await _client.SendCommandAsync("get_positions",
            new { symbol = signal.Symbol }, cancellationToken).ConfigureAwait(false);

        if (!posResponse.Success)
            return ChannelResult.Fail($"Failed to get positions: {posResponse.Error}");

        var positions = DeserializeData<List<Mt5Position>>(posResponse.Data);
        if (positions is null || positions.Count == 0)
            return ChannelResult.Fail($"No open positions for {signal.Symbol}");

        // Modify the oldest position (FIFO)
        var oldest = positions.OrderBy(p => p.Time).First();

        var modifyResponse = await _client.SendCommandAsync("modify",
            new { ticket = oldest.Ticket, sl = signal.StopLoss, tp = signal.TakeProfit },
            cancellationToken).ConfigureAwait(false);

        if (!modifyResponse.Success)
            return ChannelResult.Fail($"MT5 modify failed: {modifyResponse.Error}");

        return ChannelResult.Ok($"Position modified: ticket={oldest.Ticket} SL={signal.StopLoss} TP={signal.TakeProfit}");
    }

    private static string DescribeAction(Signal signal) =>
        signal.Action switch
        {
            SignalAction.Buy or SignalAction.Sell =>
                $"{signal.Action} {signal.LotSize ?? 0.01m} {signal.Symbol} {signal.OrderType}",
            SignalAction.Close => $"Close {signal.Symbol} (FIFO)",
            SignalAction.Modify => $"Modify {signal.Symbol} SL={signal.StopLoss} TP={signal.TakeProfit}",
            _ => $"Unknown action: {signal.Action}"
        };

    private static T? DeserializeData<T>(JsonElement? data)
    {
        if (data is null) return default;
        return JsonSerializer.Deserialize<T>(data.Value.GetRawText(), JsonOptions);
    }
}
