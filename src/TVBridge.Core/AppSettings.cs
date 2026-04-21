namespace TVBridge.Core;

public sealed class AppSettings
{
    public bool GlobalDryRun { get; set; }
    public int WebhookPort { get; set; } = 5555;
    public string? WebhookSecret { get; set; }
}
