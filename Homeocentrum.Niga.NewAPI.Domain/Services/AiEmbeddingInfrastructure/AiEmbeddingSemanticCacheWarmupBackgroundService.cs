using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

/// <summary>
/// Loads enterprise concept + rubric embedding vectors into memory after API startup
/// so the first audio case does not pay a multi-minute SQL load at RubricDiscovery (93%).
/// </summary>
public class AiEmbeddingSemanticCacheWarmupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiEmbeddingSemanticCacheReadiness _readiness;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEmbeddingSemanticCacheWarmupBackgroundService> _logger;

    public AiEmbeddingSemanticCacheWarmupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IAiEmbeddingSemanticCacheReadiness readiness,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEmbeddingSemanticCacheWarmupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _readiness = readiness;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled || !_options.EnableEnterpriseSemanticSearch)
        {
            _readiness.MarkReadyIfDisabled();
            _logger.LogInformation("Semantic embedding cache warmup skipped (infrastructure or semantic search disabled).");
            return;
        }

        if (!_options.EnableSemanticCacheWarmupOnStartup)
        {
            _readiness.MarkReadyIfDisabled();
            _logger.LogInformation("Semantic embedding cache warmup is disabled by configuration.");
            return;
        }

        var delay = TimeSpan.FromSeconds(Math.Max(10, _options.SemanticCacheWarmupDelaySeconds));
        _logger.LogInformation(
            "Semantic embedding cache warmup scheduled in {DelaySeconds}s. Audio analysis will wait until warmup completes.",
            delay.TotalSeconds);

        Exception? lastError = null;
        try
        {
            await Task.Delay(delay, stoppingToken);
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    await WarmupAsync(stoppingToken);
                    lastError = null;
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    if (attempt == 2)
                        break;
                    _logger.LogWarning(ex, "Semantic embedding cache warmup attempt 1 failed. Retrying once.");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        if (lastError != null)
        {
            _logger.LogWarning(lastError, "Semantic embedding cache warmup failed.");
            _readiness.MarkFailed(lastError.Message);
        }
    }

    private async Task WarmupAsync(CancellationToken cancellationToken)
    {
        _readiness.MarkWarming();
        var totalSw = Stopwatch.StartNew();

        await using var scope = _scopeFactory.CreateAsyncScope();
        var versionService = scope.ServiceProvider.GetRequiredService<IAiEmbeddingVersionService>();
        var conceptCache = scope.ServiceProvider.GetRequiredService<IAiConceptEmbeddingMemoryCache>();
        var rubricCache = scope.ServiceProvider.GetRequiredService<IAiEnterpriseRubricEmbeddingMemoryCache>();

        var version = await versionService.GetCurrentVersionAsync(cancellationToken);
        if (version == null)
        {
            const string error = "Semantic cache warmup skipped: no active embedding version.";
            _logger.LogWarning(error);
            _readiness.MarkFailed(error);
            return;
        }

        if (conceptCache.Entries.Count == 0)
        {
            _logger.LogInformation("Warming concept embedding cache for version {VersionCode}...", version.VersionCode);
            await conceptCache.RefreshAsync(version.EmbeddingVersionId, cancellationToken);
        }

        if (rubricCache.Entries.Count == 0)
        {
            _logger.LogInformation(
                "Warming rubric embedding cache for version {VersionCode} (this may take several minutes on first startup)...",
                version.VersionCode);
            await rubricCache.RefreshAsync(version.EmbeddingVersionId, cancellationToken);
        }

        var conceptCount = conceptCache.Entries.Count;
        var rubricCount = rubricCache.Entries.Count;

        if (conceptCount == 0 || rubricCount == 0)
        {
            var error = $"Semantic cache warmup incomplete: concepts={conceptCount}, rubrics={rubricCount}.";
            _logger.LogWarning(error);
            _readiness.MarkFailed(error);
            return;
        }

        _readiness.MarkReady(conceptCount, rubricCount);
        _logger.LogInformation(
            "Semantic embedding cache warmup complete in {ElapsedMs}ms: concepts={ConceptCount}, rubrics={RubricCount}. Audio analysis is now allowed.",
            totalSw.ElapsedMilliseconds,
            conceptCount,
            rubricCount);
    }
}
