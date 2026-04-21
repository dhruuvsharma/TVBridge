using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using TVBridge.Storage;
using TVBridge.Storage.Repositories;

namespace TVBridge.App;

public partial class App : Application
{
    private IHost? _host;

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
                services.AddSingleton<MigrationRunner>();
                services.AddSingleton(sp => new DatabaseManager(
                    dbPath,
                    sp.GetRequiredService<MigrationRunner>(),
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseManager>>()));
                services.AddSingleton<SignalRepository>();
                services.AddSingleton<RuleRepository>();

                services.AddSingleton<ViewModels.MainViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Initialize database
        var db = _host.Services.GetRequiredService<DatabaseManager>();
        await db.InitializeAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _host.Services.GetRequiredService<ViewModels.MainViewModel>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
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
