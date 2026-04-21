using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TVBridge.Core;
using TVBridge.Storage;
using TVBridge.Storage.Repositories;
using TVBridge.Webhook;

namespace TVBridge.App;

public partial class App : Application
{
    private IHost? _host;
    private WebhookListener? _webhookListener;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TVBridge");
        Directory.CreateDirectory(appDataPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.WithProperty("App", "TVBridge")
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(appDataPath, "logs", "tvbridge-.log"),
                rollingInterval: RollingInterval.Day,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                var dbPath = Path.Combine(appDataPath, "tvbridge.db");

                // Storage
                services.AddSingleton<MigrationRunner>();
                services.AddSingleton(sp => new DatabaseManager(
                    dbPath,
                    sp.GetRequiredService<MigrationRunner>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseManager>>()));
                services.AddSingleton<SignalRepository>();
                services.AddSingleton<RuleRepository>();
                services.AddSingleton<SettingsRepository>();

                // Core
                services.AddSingleton<SignalValidator>();
                services.AddSingleton<RuleEvaluator>();
                services.AddSingleton<SignalPipeline>();

                // Webhook
                services.AddSingleton(sp =>
                {
                    var settings = sp.GetRequiredService<SettingsRepository>();
                    return new WebhookSecretValidator(() =>
                        settings.GetAsync("webhook_secret").GetAwaiter().GetResult());
                });
                services.AddSingleton(sp => new WebhookListener(
                    5555,
                    sp.GetRequiredService<SignalValidator>(),
                    sp.GetRequiredService<WebhookSecretValidator>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WebhookListener>>()));

                // UI
                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Initialize database
        var db = _host.Services.GetRequiredService<DatabaseManager>();
        await db.InitializeAsync();

        // Start webhook listener
        _webhookListener = _host.Services.GetRequiredService<WebhookListener>();
        var pipeline = _host.Services.GetRequiredService<SignalPipeline>();
        var ruleRepo = _host.Services.GetRequiredService<RuleRepository>();
        _webhookListener.OnSignalReceived(async (signal, ct) =>
        {
            var signalRepo = _host.Services.GetRequiredService<SignalRepository>();
            var id = await signalRepo.InsertAsync(signal, ct);
            var rules = await ruleRepo.GetAllEnabledAsync(ct);
            // TODO: read global dry-run from settings
            await pipeline.ProcessAsync(signal with { Id = id }, rules, globalDryRun: false, ct);
            await signalRepo.MarkProcessedAsync(id, ct);
        });
        await _webhookListener.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<ViewModels.MainViewModel>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_webhookListener is not null)
            await _webhookListener.DisposeAsync();

        if (_host is not null)
        {
            var db = _host.Services.GetRequiredService<DatabaseManager>();
            db.Dispose();
            await _host.StopAsync();
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }
}
