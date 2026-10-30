using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Background drain for dbo.PushNotificationOutbox.
/// Independent of SMS SmsOutboxWorker. Does not run FCM on the HTTP request thread.
/// </summary>
public sealed class PushNotificationOutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PushNotificationOutboxWorker> _logger;

    public PushNotificationOutboxWorker(
        IServiceScopeFactory scopes,
        ILogger<PushNotificationOutboxWorker> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Short grace so schema ensure / Firebase init can finish after recycle.
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<IPushOutboxProcessor>();
                var handled = await processor.ProcessPendingAsync(stoppingToken);
                if (handled > 0)
                    _logger.LogDebug("Push outbox processed {Count} row(s).", handled);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push outbox drain failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
