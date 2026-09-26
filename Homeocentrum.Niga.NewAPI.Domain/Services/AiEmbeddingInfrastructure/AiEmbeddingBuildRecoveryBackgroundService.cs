using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

/// <summary>
/// On API startup (and optional periodic checks), clears zombie embedding jobs left by
/// power loss / PC restart and resumes incremental rubric + concept builds from the last saved count.
/// </summary>
public class AiEmbeddingBuildRecoveryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEmbeddingBuildRecoveryBackgroundService> _logger;

    public AiEmbeddingBuildRecoveryBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEmbeddingBuildRecoveryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Embedding build recovery service is disabled (infrastructure off).");
            return;
        }

        if (!_options.EnableAutoResumeEmbeddingBuildOnStartup)
        {
            _logger.LogInformation("Embedding build recovery is disabled by configuration.");
            return;
        }

        var startupDelay = TimeSpan.FromSeconds(Math.Max(5, _options.AutoResumeStartupDelaySeconds));
        _logger.LogInformation(
            "Embedding build recovery service waiting {DelaySeconds}s before first check.",
            startupDelay.TotalSeconds);

        try
        {
            await Task.Delay(startupDelay, stoppingToken);
            await RunRecoveryCycleAsync("Startup", stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Embedding build recovery startup check failed. Periodic checks will retry.");
        }

        if (!_options.EnableAutoResumeEmbeddingBuildOnStartup
            || _options.AutoResumeCheckIntervalHours <= 0)
        {
            return;
        }

        var interval = TimeSpan.FromHours(_options.AutoResumeCheckIntervalHours);
        _logger.LogInformation(
            "Embedding build recovery periodic checks enabled. Interval={IntervalHours}h",
            _options.AutoResumeCheckIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunRecoveryCycleAsync("PeriodicCheck", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Periodic embedding build recovery check failed.");
            }
        }
    }

    private async Task RunRecoveryCycleAsync(string trigger, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IAiEmbeddingUnitOfWork>();
        var jobService = scope.ServiceProvider.GetRequiredService<IAiEmbeddingJobService>();
        var versionService = scope.ServiceProvider.GetRequiredService<IAiEmbeddingVersionService>();
        var catalogReader = scope.ServiceProvider.GetRequiredService<IRepertoryRubricCatalogReader>();
        var rubricBuilder = scope.ServiceProvider.GetRequiredService<IAiEnterpriseRubricEmbeddingBuilder>();

        var recovered = await RecoverStaleRunningJobsAsync(unitOfWork, jobService, cancellationToken);
        if (recovered > 0)
        {
            _logger.LogWarning(
                "Recovered {RecoveredCount} stale embedding job(s) marked Failed after process restart.",
                recovered);
        }

        if (!_options.EnableAutoResumeEmbeddingBuildOnStartup)
            return;

        if (await unitOfWork.Jobs.CountRunningAsync(cancellationToken) > 0)
        {
            _logger.LogInformation("Embedding build recovery skipped ({Trigger}): a job is already running.", trigger);
            return;
        }

        var version = await versionService.GetCurrentVersionAsync(cancellationToken);
        if (version == null)
        {
            _logger.LogInformation("Embedding build recovery skipped ({Trigger}): no current embedding version.", trigger);
            return;
        }

        var catalogCount = await catalogReader.CountRubricsAsync(cancellationToken);
        var rubricCount = await unitOfWork.RubricEmbeddings.CountActiveByVersionAsync(
            version.EmbeddingVersionId,
            cancellationToken);
        var conceptCount = await unitOfWork.ConceptEmbeddings.CountActiveByVersionAsync(
            version.EmbeddingVersionId,
            cancellationToken);

        _logger.LogInformation(
            "Embedding build recovery check ({Trigger}): rubrics={RubricCount}/{CatalogCount} concepts={ConceptCount}",
            trigger,
            rubricCount,
            catalogCount,
            conceptCount);

        if (rubricCount < catalogCount && _options.EnableEnterpriseBuilder)
        {
            _logger.LogInformation(
                "Auto-resuming incremental rubric embedding build from {RubricCount} embedded rows.",
                rubricCount);

            var rubricResult = await rubricBuilder.BuildAsync(new BuildEnterpriseRubricEmbeddingsRequest
            {
                FullReindex = false,
                TriggerSource = $"AutoResume:{trigger}",
            }, cancellationToken);

            if (!rubricResult.Success)
            {
                if (string.Equals(rubricResult.Error, "Embedding API is not configured.", StringComparison.Ordinal))
                {
                    _logger.LogInformation(
                        "Embedding auto-resume skipped ({Trigger}): API key is not set. {RubricCount} of {CatalogCount} rubrics are already stored.",
                        trigger,
                        rubricCount,
                        catalogCount);
                }
                else
                {
                    _logger.LogWarning(
                        "Auto-resume rubric build failed ({Trigger}): {Error}",
                        trigger,
                        rubricResult.Error);
                }
                return;
            }

            _logger.LogInformation(
                "Auto-resume rubric build finished ({Trigger}): processed={Processed} skipped={Skipped} failed={Failed}",
                trigger,
                rubricResult.Processed,
                rubricResult.Skipped,
                rubricResult.Failed);
        }

        if (!_options.EnableAutoResumeConceptBuildAfterRubrics
            || !_options.EnableEnterpriseSemanticSearch)
        {
            return;
        }

        if (await unitOfWork.Jobs.CountRunningAsync(cancellationToken) > 0)
            return;

        rubricCount = await unitOfWork.RubricEmbeddings.CountActiveByVersionAsync(
            version.EmbeddingVersionId,
            cancellationToken);
        conceptCount = await unitOfWork.ConceptEmbeddings.CountActiveByVersionAsync(
            version.EmbeddingVersionId,
            cancellationToken);

        if (rubricCount >= catalogCount
            && await ShouldResumeConceptBuildAsync(unitOfWork, conceptCount, cancellationToken))
        {
            _logger.LogInformation(
                "Auto-resuming concept embedding build after rubric completion (concepts={ConceptCount}).",
                conceptCount);

            // Detached so recovery returns immediately and API host stays alive during long builds.
            _ = RunConceptBuildDetachedAsync(trigger, cancellationToken);
        }
    }

    private Task RunConceptBuildDetachedAsync(string trigger, CancellationToken cancellationToken) =>
        Task.Run(async () =>
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var conceptBuilder = scope.ServiceProvider.GetRequiredService<IAiEnterpriseConceptEmbeddingBuilder>();
                var result = await conceptBuilder.BuildAsync(new BuildEnterpriseConceptEmbeddingsRequest
                {
                    TriggerSource = $"AutoResume:{trigger}",
                }, cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        "Detached auto-resume concept build failed ({Trigger}): {Error}",
                        trigger,
                        result.Error);
                    return;
                }

                _logger.LogInformation(
                    "Detached auto-resume concept build finished ({Trigger}): processed={Processed} skipped={Skipped} failed={Failed}",
                    trigger,
                    result.Processed,
                    result.Skipped,
                    result.Failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detached auto-resume concept build crashed ({Trigger}).", trigger);
            }
        }, cancellationToken);

    /// <summary>
    /// Resume concepts when rubrics are complete and no successful ConceptOnly job exists yet,
    /// including after partial runs (e.g. 50 rows) or failed IIS/manual attempts.
    /// </summary>
    private static async Task<bool> ShouldResumeConceptBuildAsync(
        IAiEmbeddingUnitOfWork unitOfWork,
        int conceptCount,
        CancellationToken cancellationToken)
    {
        var completedJobs = await unitOfWork.Jobs.ListByStatusAsync(
            AiEmbeddingStatuses.Completed,
            50,
            cancellationToken);
        if (completedJobs.Any(j => j.JobType == AiEmbeddingJobTypes.ConceptOnly))
            return false;

        if (conceptCount == 0)
            return true;

        var failedJobs = await unitOfWork.Jobs.ListByStatusAsync(
            AiEmbeddingStatuses.Failed,
            50,
            cancellationToken);
        return failedJobs.Any(j => j.JobType == AiEmbeddingJobTypes.ConceptOnly);
    }

    private static async Task<int> RecoverStaleRunningJobsAsync(
        IAiEmbeddingUnitOfWork unitOfWork,
        IAiEmbeddingJobService jobService,
        CancellationToken cancellationToken)
    {
        var running = await unitOfWork.Jobs.ListByStatusAsync(AiEmbeddingStatuses.Running, 100, cancellationToken);
        if (running.Count == 0)
            return 0;

        var recovered = 0;
        foreach (var job in running)
        {
            await jobService.MarkJobFailedAsync(
                job.JobId,
                "Stale job recovered after API/process restart. Incremental build will resume automatically.",
                job.CreatedByUserId,
                cancellationToken);
            recovered++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return recovered;
    }
}
