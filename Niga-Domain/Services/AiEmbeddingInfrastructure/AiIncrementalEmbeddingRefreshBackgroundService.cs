using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AiEmbeddingInfrastructure;

public class AiIncrementalEmbeddingRefreshBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiIncrementalEmbeddingRefreshBackgroundService> _logger;

    public AiIncrementalEmbeddingRefreshBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiIncrementalEmbeddingRefreshBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled
            || !_options.EnableIncrementalRefresh
            || _options.UseHangfireForIncrementalRefresh
            || _options.IncrementalRefreshIntervalDays <= 0)
        {
            _logger.LogInformation(
                "Incremental embedding refresh background service is disabled (Hangfire={UseHangfire}).",
                _options.UseHangfireForIncrementalRefresh);
            return;
        }

        var interval = TimeSpan.FromDays(_options.IncrementalRefreshIntervalDays);
        _logger.LogInformation(
            "Incremental embedding refresh background service started. Interval={IntervalDays} days",
            _options.IncrementalRefreshIntervalDays);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunRefreshAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled incremental embedding refresh failed.");
            }
        }
    }

    private async Task RunRefreshAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IAiEmbeddingRefreshJobRunner>();
        var result = await runner.RunAsync(new IncrementalEmbeddingRefreshRequest
        {
            TriggerSource = "ScheduledBackgroundService",
        }, cancellationToken);

        if (!result.Success)
            _logger.LogWarning("Scheduled incremental refresh finished with error: {Error}", result.Error);
        else
            _logger.LogInformation(
                "Scheduled incremental refresh finished detected={Detected} enqueued={Enqueued} processed={Processed} skipped={Skipped} failed={Failed}",
                result.DetectedCount,
                result.EnqueuedCount,
                result.ProcessedCount,
                result.SkippedCount,
                result.FailedCount);
    }
}
