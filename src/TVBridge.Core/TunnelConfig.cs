namespace TVBridge.Core;

public sealed class TunnelConfig
{
    public bool UseQuickTunnel { get; set; } = true;
    public int LocalPort { get; set; } = 5555;
    public string CloudflaredPath { get; set; } = "tools/cloudflared/cloudflared.exe";
    public int RestartDelaySeconds { get; set; } = 5;
    public int MaxRestartAttempts { get; set; } = 3;
}
