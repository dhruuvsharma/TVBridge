namespace TVBridge.Channels.Abstractions;

public sealed record ChannelResult(bool Success, string Message)
{
    public static ChannelResult Ok(string message = "Sent successfully") => new(true, message);
    public static ChannelResult Fail(string message) => new(false, message);
    public static ChannelResult DryRun(string message) => new(true, $"[DRY RUN] {message}");
}
