using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services;

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
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                await service.ProcessBulkSendJobAsync(job, BatchSize, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk WhatsApp job {JobId} failed.", job.JobId);
            }
        }
    }
}
