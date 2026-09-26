using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class AiEnterpriseEmbeddingBuilderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEnterpriseEmbeddingBuilderBackgroundService> _logger;

    public AiEnterpriseEmbeddingBuilderBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEnterpriseEmbeddingBuilderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled
            || !_options.EnableEnterpriseBuilder
            || _options.BuilderScheduledIntervalHours <= 0)
        {
            _logger.LogInformation("Enterprise embedding builder background service is disabled.");
            return;
        }

        var interval = TimeSpan.FromHours(_options.BuilderScheduledIntervalHours);
        _logger.LogInformation(
            "Enterprise embedding builder background service started. Interval={IntervalHours}h",
            _options.BuilderScheduledIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunBuildAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled enterprise embedding build failed.");
            }
        }
    }

    private async Task RunBuildAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var builder = scope.ServiceProvider.GetRequiredService<IAiEnterpriseRubricEmbeddingBuilder>();
        var result = await builder.BuildAsync(new BuildEnterpriseRubricEmbeddingsRequest
        {
            FullReindex = false,
            TriggerSource = "ScheduledBackgroundService",
        }, cancellationToken);

        if (!result.Success)
            _logger.LogWarning("Scheduled enterprise embedding build finished with error: {Error}", result.Error);
        else
            _logger.LogInformation(
                "Scheduled enterprise embedding build finished processed={Processed} skipped={Skipped} failed={Failed}",
                result.Processed,
                result.Skipped,
                result.Failed);
    }
}
