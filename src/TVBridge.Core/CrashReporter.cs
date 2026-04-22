using Microsoft.Extensions.Logging;

namespace TVBridge.Core;

/// <summary>
/// Writes unhandled exception details to a local crash report file.
/// No telemetry, no uploads — purely local.
/// </summary>
public sealed class CrashReporter
{
    private readonly string _crashDir;
    private readonly ILogger<CrashReporter> _logger;

    public CrashReporter(string appDataPath, ILogger<CrashReporter> logger)
    {
        _crashDir = Path.Combine(appDataPath, "crashes");
        _logger = logger;
        Directory.CreateDirectory(_crashDir);
    }

    public void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        WriteCrashReport(e.ExceptionObject as Exception, "UnhandledException");
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        WriteCrashReport(e.Exception, "UnobservedTaskException");
        e.SetObserved();
    }

    private void WriteCrashReport(Exception? ex, string source)
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"crash_{timestamp}.txt";
            var filePath = Path.Combine(_crashDir, fileName);

            var report = $"""
                TVBridge Crash Report
                =====================
                Time (UTC): {DateTime.UtcNow:O}
                Source: {source}
                Machine: {Environment.MachineName}
                OS: {Environment.OSVersion}
                .NET: {Environment.Version}

                Exception:
                {ex}
                """;

            File.WriteAllText(filePath, report);
            _logger.LogCritical(ex, "Crash report written to {Path}", filePath);

            // Keep only the last 20 crash reports
            CleanupOldReports(20);
        }
        catch
        {
            // Last resort — can't do anything if crash reporting itself fails
        }
    }

    private void CleanupOldReports(int keep)
    {
        var files = Directory.GetFiles(_crashDir, "crash_*.txt")
            .OrderByDescending(f => f)
            .Skip(keep);

        foreach (var file in files)
        {
            try { File.Delete(file); }
            catch { /* best effort */ }
        }
    }
}
