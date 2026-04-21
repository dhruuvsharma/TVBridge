namespace TVBridge.Core;

public interface IOutputChannel
{
    string Name { get; }
    string ChannelType { get; }
    int ChannelId { get; }

    Task<ChannelResult> SendAsync(Signal signal, bool dryRun, CancellationToken cancellationToken = default);
    Task<bool> ValidateConfigAsync(CancellationToken cancellationToken = default);
}

public sealed record ChannelResult(bool Success, string Message)
{
    public static ChannelResult Ok(string message = "Sent successfully") => new(true, message);
    public static ChannelResult Fail(string message) => new(false, message);
    public static ChannelResult DryRun(string message) => new(true, $"[DRY RUN] {message}");
}
