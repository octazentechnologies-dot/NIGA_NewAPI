using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Embeddings;

public class RubricEmbeddingIndexerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<RubricEmbeddingIndexerBackgroundService> _logger;

    public RubricEmbeddingIndexerBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<RubricEmbeddingIndexerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var settings = scope.ServiceProvider.GetRequiredService<IRubricIntelligenceSettingsService>();

                    if (settings.IsV2Active && settings.GetBaseOptions().EnableEmbeddingSearch)
                    {
                        var cache = scope.ServiceProvider.GetRequiredService<IRubricEmbeddingMemoryCache>();
                        await cache.RefreshAsync(stoppingToken);

                        var indexer = scope.ServiceProvider.GetRequiredService<IRubricEmbeddingIndexerService>();
                        var result = await indexer.ReindexAsync(cancellationToken: stoppingToken);
                        if (result.Processed > 0)
                        {
                            _logger.LogInformation(
                                "Scheduled embedding indexer processed {Count} rubric(s).", result.Processed);
                        }
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Scheduled rubric embedding indexer failed.");
                }

                var delayHours = Math.Max(1, _options.EmbeddingIndexerIntervalHours);
                await Task.Delay(TimeSpan.FromHours(delayHours), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
