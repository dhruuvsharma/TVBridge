namespace TVBridge.Core;

public sealed class Mt5Config
{
    public string PythonPath { get; set; } = "python";
    public string SidecarPath { get; set; } = "sidecar/mt5_bridge/main.py";
    public int RepPort { get; set; } = 5556;
    public int PubPort { get; set; } = 5557;
    public int RestartDelaySeconds { get; set; } = 5;
    public int MaxRestartAttempts { get; set; } = 3;
    public int AccountStateIntervalMs { get; set; } = 1000;
    public int CommandTimeoutMs { get; set; } = 10_000;
}
