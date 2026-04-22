using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Channels.NinjaTrader;

/// <summary>
/// Output channel that routes signals to NinjaTrader 8 via ATI TCP socket.
/// </summary>
public sealed class NinjaTraderChannel : IOutputChannel
{
    private readonly IAtiClient _client;
    private readonly NtSymbolMapper _symbolMapper;
    private readonly NinjaTraderConfig _config;
    private readonly ILogger<NinjaTraderChannel> _logger;

    public string Name { get; }
    public string ChannelType => "NinjaTrader";
    public int ChannelId { get; }

    public NinjaTraderChannel(
        IAtiClient client,
        NinjaTraderConfig config,
        ILogger<NinjaTraderChannel> logger,
        string name = "NinjaTrader 8",
        int channelId = 0)
    {
        _client = client;
        _config = config;
        _symbolMapper = new NtSymbolMapper(config.SymbolMap);
        _logger = logger;
        Name = name;
        ChannelId = channelId;
    }

    public async Task<ChannelResult> SendAsync(Signal signal, bool dryRun, CancellationToken cancellationToken = default)
    {
        var ntSymbol = _symbolMapper.Map(signal.Symbol);
        var description = DescribeAction(signal, ntSymbol);

        if (dryRun)
            return ChannelResult.DryRun(description);

        try
        {
            if (!_client.IsConnected)
            {
                var connected = await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
                if (!connected)
                    return ChannelResult.Fail("Cannot connect to NinjaTrader ATI");
            }

            return signal.Action switch
            {
                SignalAction.Buy or SignalAction.Sell => await PlaceOrderAsync(signal, ntSymbol, cancellationToken).ConfigureAwait(false),
                SignalAction.Close => await ClosePositionAsync(ntSymbol, cancellationToken).ConfigureAwait(false),
                SignalAction.Modify => ChannelResult.Fail("NinjaTrader ATI does not support position modification — use Close + re-entry"),
                _ => ChannelResult.Fail($"Unsupported action: {signal.Action}")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NinjaTrader channel error for signal {AlertId}", signal.AlertId);
            return ChannelResult.Fail($"NinjaTrader error: {ex.Message}");
        }
    }

    public async Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _client.ConnectAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return false;
        }
    }

    private async Task<ChannelResult> PlaceOrderAsync(Signal signal, string ntSymbol, CancellationToken cancellationToken)
    {
        var command = NtAtiCommandBuilder.BuildPlaceOrder(signal, ntSymbol);
        _logger.LogInformation("ATI PLACE: {Command}", command);

        var response = await _client.SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        return response.Success
            ? ChannelResult.Ok($"NinjaTrader order placed: {signal.Action} {ntSymbol}")
            : ChannelResult.Fail($"NinjaTrader ATI error: {response.Message}");
    }

    private async Task<ChannelResult> ClosePositionAsync(string ntSymbol, CancellationToken cancellationToken)
    {
        var command = NtAtiCommandBuilder.BuildClosePosition(ntSymbol);
        _logger.LogInformation("ATI CLOSE: {Command}", command);

        var response = await _client.SendCommandAsync(command, cancellationToken).ConfigureAwait(false);

        return response.Success
            ? ChannelResult.Ok($"NinjaTrader position closed: {ntSymbol}")
            : ChannelResult.Fail($"NinjaTrader ATI error: {response.Message}");
    }

    private static string DescribeAction(Signal signal, string ntSymbol) =>
        signal.Action switch
        {
            SignalAction.Buy or SignalAction.Sell =>
                $"{signal.Action} {signal.LotSize ?? 1} {ntSymbol} {signal.OrderType}",
            SignalAction.Close => $"Close {ntSymbol}",
            SignalAction.Modify => $"Modify {ntSymbol} (unsupported by ATI)",
            _ => $"Unknown: {signal.Action}"
        };
}
