using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

namespace Homeocentrum.Niga.NewAPI.Domain.Services;

public class AudioCaseTakingBackgroundService : BackgroundService
{
    private readonly AudioCaseTakingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAiEmbeddingSemanticCacheReadiness _cacheReadiness;
    private readonly IAudioCaseSessionProcessingGate _sessionGate;
    private readonly AudioCaseTakingOptions _options;
    private readonly RubricIntelligenceOptions _intelligenceOptions;
    private readonly IRubricIntelligenceSettingsService _intelligenceSettings;
    private readonly ILogger<AudioCaseTakingBackgroundService> _logger;
    private int _activeWorkers;

    public AudioCaseTakingBackgroundService(
        AudioCaseTakingQueue queue,
        IServiceScopeFactory scopeFactory,
        IAiEmbeddingSemanticCacheReadiness cacheReadiness,
        IAudioCaseSessionProcessingGate sessionGate,
        IOptions<AudioCaseTakingOptions> options,
        IOptions<RubricIntelligenceOptions> intelligenceOptions,
        IRubricIntelligenceSettingsService intelligenceSettings,
        ILogger<AudioCaseTakingBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _cacheReadiness = cacheReadiness;
        _sessionGate = sessionGate;
        _options = options.Value;
        _intelligenceOptions = intelligenceOptions.Value;
        _intelligenceSettings = intelligenceSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var startupScope = _scopeFactory.CreateAsyncScope();
            var startupService = startupScope.ServiceProvider.GetRequiredService<IAudioCaseTakingService>();
            var requeued = await startupService.RequeueOrphanedUploadedSessionsAsync(stoppingToken);
            if (requeued > 0)
            {
                _logger.LogInformation(
                    "Re-queued {Count} orphaned Uploaded audio case session(s) after API startup.",
                    requeued);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Audio case orphan re-queue cancelled during host shutdown/startup.");
        }
        catch (Exception ex)
        {
            // Hosting restarts often cancel SQL mid-flight; do not escalate to LogError
            // (EventLog dispose during failed bind caused AggregateException BackgroundService failed).
            var cancelled = ex is OperationCanceledException
                || (ex is Microsoft.Data.SqlClient.SqlException sql
                    && sql.Message.Contains("cancelled", StringComparison.OrdinalIgnoreCase));
            try
            {
                if (cancelled)
                    _logger.LogWarning(ex, "Orphaned audio re-queue skipped (startup cancelled).");
                else
                    _logger.LogWarning(ex, "Failed to re-queue orphaned Uploaded audio case sessions on startup.");
            }
            catch
            {
                // Swallow logger dispose races during failed host start.
            }
        }

        var workers = Math.Clamp(_options.MaxConcurrentProcessingWorkers, 1, 8);
        _logger.LogInformation("Audio case taking queue workers starting count={Count}.", workers);
        var tasks = Enumerable.Range(0, workers)
            .Select(id => RunWorkerAsync(id, stoppingToken))
            .ToArray();
        await Task.WhenAll(tasks);
    }

    private async Task RunWorkerAsync(int workerId, CancellationToken stoppingToken)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(stoppingToken))
            {
                while (_queue.Reader.TryRead(out var job))
                {
                    _queue.MarkDequeued();
                    await ProcessJobAsync(job, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Audio case taking worker {WorkerId} stopping.", workerId);
        }
    }

    private async Task ProcessJobAsync(AudioCaseTakingJob job, CancellationToken stoppingToken)
    {
            var dequeuedUtc = DateTime.UtcNow;
            var queueDepth = _queue.PendingCount;
            var workerBusy = Interlocked.Increment(ref _activeWorkers);
            try
            {
                // One DI scope for telemetry + processing so counters/stages share the same session.
                await using var scope = _scopeFactory.CreateAsyncScope();
                var telemetry = scope.ServiceProvider.GetRequiredService<IRubricPipelineTelemetry>();
                var service = scope.ServiceProvider.GetRequiredService<IAudioCaseTakingService>();
                var correlationId = job.CorrelationId;
                if (string.IsNullOrWhiteSpace(correlationId))
                    correlationId = job.SessionId.ToString("N")[..12];

                telemetry.BeginSession(job.SessionId, correlationId);
                telemetry.SetEngineVersion(RubricEngineStamp.FromRuntime(_intelligenceOptions, _intelligenceSettings));

                _logger.LogInformation(
                    "LatencyGap1 Session {SessionId}: dequeuedUtc={Dequeued:o} queueDepth={QueueDepth} activeWorkers={Active} cacheReady={CacheReady} cacheWarming={CacheWarming} loadedConcepts={Concepts} loadedRubrics={Rubrics}",
                    job.SessionId,
                    dequeuedUtc,
                    queueDepth,
                    workerBusy,
                    _cacheReadiness.IsReady,
                    _cacheReadiness.IsWarming,
                    _cacheReadiness.LoadedCounts.ConceptCount,
                    _cacheReadiness.LoadedCounts.RubricCount);

                await telemetry.RecordStageAsync(
                    "WorkerDequeued",
                    0,
                    dequeuedUtc,
                    DateTime.UtcNow,
                    message: $"queueDepth={queueDepth}; cacheReady={_cacheReadiness.IsReady}; warming={_cacheReadiness.IsWarming}",
                    cancellationToken: stoppingToken);

                var cacheWaitMs = 0;
                if (!_cacheReadiness.IsReady)
                {
                    // Stage C fast path: never burn 120s waiting on cold cache (baseline Timeout).
                    var waitMinutes = Math.Clamp(_options.SemanticCacheMaxWaitMinutes, 0, 20);
                    if (_intelligenceOptions.EnableFastClinicalRetrievalPipeline)
                    {
                        waitMinutes = _intelligenceOptions.FastPipelineSemanticCacheMaxWaitSeconds <= 0
                            ? 0
                            : Math.Min(
                                waitMinutes,
                                Math.Max(1, (int)Math.Ceiling(_intelligenceOptions.FastPipelineSemanticCacheMaxWaitSeconds / 60.0)));
                        if (_intelligenceOptions.FastPipelineSemanticCacheMaxWaitSeconds <= 0)
                            waitMinutes = 0;
                    }

                    if (waitMinutes <= 0)
                    {
                        _logger.LogWarning(
                            "LatencyGap1 Session {SessionId}: cache not ready — proceeding without wait (fastPipeline={Fast}).",
                            job.SessionId,
                            _intelligenceOptions.EnableFastClinicalRetrievalPipeline);
                        telemetry.IncrementCacheMiss();
                        await telemetry.RecordStageAsync(
                            "SemanticCacheWait",
                            0,
                            DateTime.UtcNow,
                            DateTime.UtcNow,
                            status: "Skipped",
                            message: $"proceedingDegraded=true; fastPipeline={_intelligenceOptions.EnableFastClinicalRetrievalPipeline}",
                            cancellationToken: stoppingToken);
                    }
                    else
                    {
                        var waitSw = Stopwatch.StartNew();
                        var waitStart = DateTime.UtcNow;
                        _logger.LogInformation(
                            "LatencyGap1 Session {SessionId}: SemanticCache wait START (max {WaitMinutes}m) at {Start:o}",
                            job.SessionId,
                            waitMinutes,
                            waitStart);

                        try
                        {
                            await _cacheReadiness.WaitUntilReadyAsync(
                                TimeSpan.FromMinutes(waitMinutes),
                                stoppingToken);
                            waitSw.Stop();
                            cacheWaitMs = (int)waitSw.ElapsedMilliseconds;
                            if (_cacheReadiness.IsReady)
                                telemetry.IncrementCacheHit();
                            else
                                telemetry.IncrementCacheMiss();

                            _logger.LogInformation(
                                "LatencyGap1 Session {SessionId}: SemanticCache wait END ready={Ready} waitMs={WaitMs}",
                                job.SessionId,
                                _cacheReadiness.IsReady,
                                cacheWaitMs);
                            await telemetry.RecordStageAsync(
                                "SemanticCacheWait",
                                cacheWaitMs,
                                waitStart,
                                DateTime.UtcNow,
                                message: $"ready={_cacheReadiness.IsReady}; waitMs={cacheWaitMs}",
                                cancellationToken: stoppingToken);
                        }
                        catch (TimeoutException)
                        {
                            waitSw.Stop();
                            cacheWaitMs = (int)waitSw.ElapsedMilliseconds;
                            telemetry.IncrementCacheMiss();
                            _logger.LogWarning(
                                "LatencyGap1 Session {SessionId}: SemanticCache wait TIMEOUT after {WaitMs}ms — proceeding without full cache (degraded embeddings).",
                                job.SessionId,
                                cacheWaitMs);
                            await telemetry.RecordStageAsync(
                                "SemanticCacheWait",
                                cacheWaitMs,
                                waitStart,
                                DateTime.UtcNow,
                                status: "Timeout",
                                message: $"waitMs={cacheWaitMs}; proceedingDegraded=true",
                                cancellationToken: stoppingToken);
                        }
                    }
                }
                else
                {
                    telemetry.IncrementCacheHit();
                    await telemetry.RecordStageAsync(
                        "SemanticCacheAlreadyReady",
                        0,
                        DateTime.UtcNow,
                        DateTime.UtcNow,
                        message: $"concepts={_cacheReadiness.LoadedCounts.ConceptCount}; rubrics={_cacheReadiness.LoadedCounts.RubricCount}",
                        cancellationToken: stoppingToken);
                }

                var processStartUtc = DateTime.UtcNow;
                _logger.LogInformation(
                    "LatencyGap1 Session {SessionId}: ProcessSession START at {Start:o} (msSinceDequeue={Ms})",
                    job.SessionId,
                    processStartUtc,
                    (int)(processStartUtc - dequeuedUtc).TotalMilliseconds);

                if (!_sessionGate.TryEnter(job.SessionId))
                {
                    _logger.LogWarning(
                        "Skipping duplicate in-flight job for SessionId={SessionId} JobType={JobType}",
                        job.SessionId, job.JobType);
                    await telemetry.RecordStageAsync(
                        "DuplicateSessionSkipped",
                        0,
                        processStartUtc,
                        DateTime.UtcNow,
                        status: "Skipped",
                        message: "session already processing",
                        cancellationToken: stoppingToken);
                    return;
                }

                try
                {
                    if (string.Equals(job.JobType, "ReAnalyze", StringComparison.OrdinalIgnoreCase))
                    {
                        await service.ProcessReAnalyzeAsync(job.SessionId, job.EditedTranscript ?? string.Empty, stoppingToken);
                    }
                    else
                    {
                        await service.ProcessSessionAsync(job.SessionId, stoppingToken);
                    }

                    var processElapsed = (int)(DateTime.UtcNow - processStartUtc).TotalMilliseconds;
                    _logger.LogInformation(
                        "LatencyGap1 Session {SessionId}: ProcessSession END elapsedMs={Elapsed}",
                        job.SessionId,
                        processElapsed);

                    await telemetry.RecordStageAsync(
                        "ProcessSessionTotal",
                        processElapsed,
                        processStartUtc,
                        DateTime.UtcNow,
                        cancellationToken: stoppingToken);

                    await RecordPipelineBudgetAsync(telemetry, job.SessionId, processElapsed, stoppingToken);
                    await telemetry.FlushSummaryAsync(stoppingToken);
                }
                finally
                {
                    _sessionGate.Exit(job.SessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Audio case taking job failed for SessionId={SessionId}", job.SessionId);
                try
                {
                    await using var failScope = _scopeFactory.CreateAsyncScope();
                    var failService = failScope.ServiceProvider.GetRequiredService<IAudioCaseTakingService>();
                    await failService.FailSessionFromSystemAsync(
                        job.SessionId,
                        "BACKGROUND_JOB_FAILED",
                        ex.Message,
                        stoppingToken);
                }
                catch (Exception failEx)
                {
                    _logger.LogError(failEx, "Could not mark session {SessionId} as failed after background error.", job.SessionId);
                }
            }
            finally
            {
                Interlocked.Decrement(ref _activeWorkers);
            }
    }

    private async Task RecordPipelineBudgetAsync(
        IRubricPipelineTelemetry telemetry,
        Guid sessionId,
        int processElapsedMs,
        CancellationToken cancellationToken)
    {
        var softMs = Math.Max(1, _options.PipelineSoftBudgetMinutes) * 60_000;
        var hardMs = Math.Max(_options.PipelineSoftBudgetMinutes + 1, _options.PipelineHardBudgetMinutes) * 60_000;
        if (processElapsedMs <= softMs)
            return;

        if (processElapsedMs > hardMs)
        {
            _logger.LogError(
                "AI_Rubric_PipelineHardBudgetExceeded Session={SessionId} ElapsedMs={Elapsed} HardMinutes={Hard}",
                sessionId,
                processElapsedMs,
                _options.PipelineHardBudgetMinutes);
            await telemetry.RecordStageAsync(
                "PipelineHardBudgetExceeded",
                processElapsedMs,
                DateTime.UtcNow.AddMilliseconds(-processElapsedMs),
                DateTime.UtcNow,
                status: "Alert",
                message: $"elapsedMs={processElapsedMs}; hardMinutes={_options.PipelineHardBudgetMinutes}",
                cancellationToken: cancellationToken);
            return;
        }

        _logger.LogWarning(
            "AI_Rubric_PipelineSoftBudgetExceeded Session={SessionId} ElapsedMs={Elapsed} SoftMinutes={Soft}",
            sessionId,
            processElapsedMs,
            _options.PipelineSoftBudgetMinutes);
        await telemetry.RecordStageAsync(
            "PipelineSoftBudgetExceeded",
            processElapsedMs,
            DateTime.UtcNow.AddMilliseconds(-processElapsedMs),
            DateTime.UtcNow,
            status: "Warning",
            message: $"elapsedMs={processElapsedMs}; softMinutes={_options.PipelineSoftBudgetMinutes}",
            cancellationToken: cancellationToken);
    }
}
