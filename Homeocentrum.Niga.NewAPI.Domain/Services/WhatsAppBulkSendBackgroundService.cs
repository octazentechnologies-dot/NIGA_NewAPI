using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services;

public class WhatsAppBulkSendBackgroundService : BackgroundService
{
    private const int BatchSize = 10;

    private readonly WhatsAppBulkSendQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WhatsAppBulkSendBackgroundService> _logger;

    public WhatsAppBulkSendBackgroundService(
        WhatsAppBulkSendQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<WhatsAppBulkSendBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                    await service.ProcessBulkSendJobAsync(job, BatchSize, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Bulk WhatsApp job {JobId} failed.", job.JobId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
