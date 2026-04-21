using TVBridge.Core;

namespace TVBridge.Channels.Abstractions;

public interface IOutputChannel
{
    string Name { get; }
    string ChannelType { get; }

    Task<ChannelResult> SendAsync(Signal signal, bool dryRun, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default);
}
