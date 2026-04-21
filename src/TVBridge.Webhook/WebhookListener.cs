using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TVBridge.Core;

namespace TVBridge.Webhook;

public sealed class WebhookListener : IAsyncDisposable
{
    private readonly int _port;
    private readonly SignalValidator _validator;
    private readonly WebhookSecretValidator _secretValidator;
    private readonly ILogger<WebhookListener> _logger;
    private WebApplication? _app;

    private Func<Signal, CancellationToken, Task>? _onSignalReceived;

    public WebhookListener(
        int port,
        SignalValidator validator,
        WebhookSecretValidator secretValidator,
        ILogger<WebhookListener> logger)
    {
        _port = port;
        _validator = validator;
        _secretValidator = secretValidator;
        _logger = logger;
    }

    public void OnSignalReceived(Func<Signal, CancellationToken, Task> handler)
    {
        _onSignalReceived = handler;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.ConfigureKestrel(opts =>
        {
            opts.ListenLocalhost(_port);
        });

        // Suppress ASP.NET Core default logging noise
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        _app = builder.Build();

        _app.MapPost("/api/webhook", (Delegate)HandleWebhookAsync);

        _app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

        _logger.LogInformation("Starting webhook listener on port {Port}", _port);
        await _app.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_app is not null)
        {
            _logger.LogInformation("Stopping webhook listener");
            await _app.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<IResult> HandleWebhookAsync(HttpContext context)
    {
        try
        {
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync(context.RequestAborted)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(body))
                return Results.BadRequest(new { error = "Empty request body" });

            Signal signal;
            try
            {
                signal = JsonSerializer.Deserialize<Signal>(body, SignalJsonOptions.Default)!;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning("Invalid JSON received: {Error}", ex.Message);
                return Results.BadRequest(new { error = "Invalid JSON", details = ex.Message });
            }

            // Validate secret
            if (!_secretValidator.Validate(signal.Secret))
            {
                _logger.LogWarning("Invalid webhook secret from {RemoteIp}",
                    context.Connection.RemoteIpAddress);
                return Results.Unauthorized();
            }

            // Validate signal
            var validation = _validator.Validate(signal);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Signal validation failed: {Errors}",
                    string.Join("; ", validation.Errors));
                return Results.BadRequest(new { error = "Validation failed", errors = validation.Errors });
            }

            // Stamp received time
            signal = signal with { ReceivedAt = DateTimeOffset.UtcNow };

            _logger.LogInformation("Received signal {AlertId} for {Symbol} {Action}",
                signal.AlertId, signal.Symbol, signal.Action);

            // Dispatch to pipeline
            if (_onSignalReceived is not null)
            {
                await _onSignalReceived(signal, context.RequestAborted).ConfigureAwait(false);
            }

            return Results.Ok(new
            {
                status = "accepted",
                alert_id = signal.AlertId,
                warnings = validation.Warnings.Count > 0 ? validation.Warnings : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error processing webhook");
            return Results.StatusCode(500);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync().ConfigureAwait(false);
            _app = null;
        }
    }
}
