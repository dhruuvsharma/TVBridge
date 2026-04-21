namespace TVBridge.Channels.Abstractions;

/// <summary>
/// Contract for all output channels (MT5, Telegram, Discord, NinjaTrader, etc.).
/// </summary>
public interface IOutputChannel
{
    string Name { get; }

    Task SendAsync(/* Signal signal, */ CancellationToken cancellationToken = default);

    Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default);
}
