using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class AiIncrementalEmbeddingRefreshService : IAiIncrementalEmbeddingRefreshService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IRepertoryEmbeddingChangeDetector _changeDetector;
    private readonly IAiEmbeddingVersionService _versionService;
    private readonly IAiEmbeddingJobService _jobService;
    private readonly IAiEmbeddingQueueService _queueService;
    private readonly IAiEmbeddingStatisticsService _statisticsService;
    private readonly AiIncrementalEmbeddingQueueProcessor _queueProcessor;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiIncrementalEmbeddingRefreshService> _logger;

    public AiIncrementalEmbeddingRefreshService(
        IAiEmbeddingUnitOfWork unitOfWork,
        IRepertoryEmbeddingChangeDetector changeDetector,
        IAiEmbeddingVersionService versionService,
        IAiEmbeddingJobService jobService,
        IAiEmbeddingQueueService queueService,
        IAiEmbeddingStatisticsService statisticsService,
        AiIncrementalEmbeddingQueueProcessor queueProcessor,
        IEmbeddingClient embeddingClient,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiIncrementalEmbeddingRefreshService> logger)
    {
        _unitOfWork = unitOfWork;
        _changeDetector = changeDetector;
        _versionService = versionService;
        _jobService = jobService;
        _queueService = queueService;
        _statisticsService = statisticsService;
        _queueProcessor = queueProcessor;
        _embeddingClient = embeddingClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IncrementalEmbeddingRefreshResult> RefreshAsync(
        IncrementalEmbeddingRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new IncrementalEmbeddingRefreshResult
        {
            CorrelationId = Guid.NewGuid().ToString("N")[..12],
        };

        if (!_options.Enabled || !_options.EnableIncrementalRefresh)
        {
            result.Error = "Incremental embedding refresh is disabled.";
            return result;
        }

        var version = await ResolveVersionAsync(request.EmbeddingVersionId, request.ActorUserId, cancellationToken);
        if (version == null)
        {
            result.Error = "No embedding version is available.";
            return result;
        }

        result.EmbeddingVersionId = version.EmbeddingVersionId;
        result.VersionCode = version.VersionCode;

        if (request.ProcessOnly && !request.JobId.HasValue)
        {
            result.Error = "JobId is required when ProcessOnly is true.";
            return result;
        }

        Guid jobId;
        if (request.ProcessOnly && request.JobId.HasValue)
        {
            jobId = request.JobId.Value;
            result.JobId = jobId;
            var existingJob = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
            if (existingJob == null)
            {
                result.Error = "Embedding job not found.";
                return result;
            }

            if (existingJob.Status == AiEmbeddingStatuses.Queued)
                await _jobService.MarkJobRunningAsync(jobId, request.ActorUserId, cancellationToken);
        }
        else
        {
            var (created, message, job) = await _jobService.CreateJobAsync(new CreateAiEmbeddingJobRequest
            {
                EmbeddingVersionId = version.EmbeddingVersionId,
                JobType = AiEmbeddingJobTypes.Incremental,
                TriggerSource = request.TriggerSource ?? "IncrementalRefresh",
                CreatedByUserId = request.ActorUserId,
                CorrelationId = result.CorrelationId,
            }, cancellationToken);

            if (!created || job == null)
            {
                result.Error = message;
                return result;
            }

            jobId = job.JobId;
            result.JobId = jobId;
            await _jobService.MarkJobRunningAsync(jobId, request.ActorUserId, cancellationToken);
        }

        var syncState = await GetOrCreateSyncStateAsync(version.EmbeddingVersionId, cancellationToken);
        var watermark = syncState.LastSuccessfulSyncUtc
            ?? DateTime.UtcNow.AddHours(-Math.Max(1, _options.IncrementalDetectionLookbackHours));
        result.WatermarkUtc = watermark;

        syncState.LastScanStartedUtc = DateTime.UtcNow;
        syncState.LastRunCorrelationId = result.CorrelationId;
        syncState.LastJobId = jobId;
        syncState.LastError = null;
        await _unitOfWork.SyncState.UpsertAsync(syncState, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            if (!request.ProcessOnly)
            {
                var candidates = await _changeDetector.DetectChangesAsync(watermark, cancellationToken);
                result.DetectedCount = candidates.Count;
                syncState.LastDetectedCount = candidates.Count;

                var enqueueRequests = new List<EnqueueAiEmbeddingQueueRequest>();
                foreach (var candidate in candidates)
                {
                    if (await _unitOfWork.Queue.HasOpenRubricItemAsync(candidate.RubricId, cancellationToken))
                        continue;

                    enqueueRequests.Add(new EnqueueAiEmbeddingQueueRequest
                    {
                        JobId = jobId,
                        ItemType = AiEmbeddingQueueItemTypes.Rubric,
                        RubricId = candidate.RubricId,
                        PayloadJson = AiIncrementalEmbeddingQueueProcessor.SerializePayload(candidate),
                        Priority = candidate.RequiresArchive ? 10 : _options.DefaultJobPriority,
                    });
                }

                if (enqueueRequests.Count > 0)
                {
                    var (enqueued, enqueueMessage, enqueuedCount) = await _queueService.EnqueueBatchAsync(
                        enqueueRequests,
                        cancellationToken);
                    if (!enqueued)
                    {
                        result.Error = enqueueMessage;
                        await MarkSyncFailureAsync(syncState, result.Error, cancellationToken);
                        await _jobService.MarkJobFailedAsync(jobId, result.Error, request.ActorUserId, cancellationToken);
                        return result;
                    }

                    result.EnqueuedCount = enqueuedCount;
                    syncState.LastEnqueuedCount = enqueuedCount;
                }
            }

            if (!request.DetectOnly && _embeddingClient.IsConfigured)
            {
                var processStats = await _queueProcessor.ProcessJobAsync(
                    jobId,
                    version.EmbeddingVersionId,
                    version,
                    request.MaxQueueItems,
                    request.ActorUserId,
                    cancellationToken);

                result.ProcessedCount = processStats.Processed;
                result.CreatedCount = processStats.Created;
                result.UpdatedCount = processStats.Updated;
                result.SkippedCount = processStats.Skipped;
                result.ArchivedCount = processStats.Archived;
                result.FailedCount = processStats.Failed;
                result.DeadLetterCount = processStats.DeadLetter;

                syncState.LastProcessedCount = processStats.Processed;
                syncState.LastSkippedCount = processStats.Skipped;
                syncState.LastFailedCount = processStats.Failed;
            }
            else if (!request.DetectOnly && !_embeddingClient.IsConfigured)
            {
                result.Error = "Embedding API is not configured.";
                await MarkSyncFailureAsync(syncState, result.Error, cancellationToken);
                await _jobService.MarkJobFailedAsync(jobId, result.Error, request.ActorUserId, cancellationToken);
                return result;
            }

            syncState.LastScanCompletedUtc = DateTime.UtcNow;
            if (!request.ProcessOnly && !request.DetectOnly)
                syncState.LastSuccessfulSyncUtc = DateTime.UtcNow;
            syncState.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.SyncState.UpsertAsync(syncState, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            if (!request.DetectOnly)
            {
                await _jobService.MarkJobCompletedAsync(jobId, request.ActorUserId, cancellationToken);
                await _statisticsService.RefreshDailySnapshotAsync(version.EmbeddingVersionId, cancellationToken: cancellationToken);
            }

            result.Success = string.IsNullOrWhiteSpace(result.Error);
            result.CompletedAtUtc = DateTime.UtcNow;

            _logger.LogInformation(
                "Incremental embedding refresh completed job={JobId} detected={Detected} enqueued={Enqueued} processed={Processed} skipped={Skipped} failed={Failed}",
                jobId,
                result.DetectedCount,
                result.EnqueuedCount,
                result.ProcessedCount,
                result.SkippedCount,
                result.FailedCount);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental embedding refresh failed job={JobId}", jobId);
            result.Error = ex.Message;
            await MarkSyncFailureAsync(syncState, ex.Message, cancellationToken);
            await _jobService.MarkJobFailedAsync(jobId, ex.Message, request.ActorUserId, cancellationToken);
            return result;
        }
    }

    public async Task<AiEmbeddingSyncStateModel?> GetSyncStateAsync(
        Guid? embeddingVersionId = null,
        CancellationToken cancellationToken = default)
    {
        var versionId = embeddingVersionId;
        if (!versionId.HasValue)
        {
            var current = await _versionService.GetCurrentVersionAsync(cancellationToken);
            versionId = current?.EmbeddingVersionId;
        }

        var state = await _unitOfWork.SyncState.GetAsync(
            AiEmbeddingSyncScopes.IncrementalRefresh,
            versionId,
            cancellationToken);

        return state == null ? null : MapSyncState(state);
    }

    private async Task<AiEmbeddingVersionModel?> ResolveVersionAsync(
        Guid? requestedVersionId,
        int? actorUserId,
        CancellationToken cancellationToken)
    {
        if (requestedVersionId.HasValue)
        {
            var explicitVersion = await _unitOfWork.Versions.GetByIdAsync(requestedVersionId.Value, cancellationToken);
            return explicitVersion == null ? null : MapVersion(explicitVersion);
        }

        return await _versionService.GetCurrentVersionAsync(cancellationToken);
    }

    private async Task<AiEmbeddingSyncState> GetOrCreateSyncStateAsync(
        Guid embeddingVersionId,
        CancellationToken cancellationToken)
    {
        var existing = await _unitOfWork.SyncState.GetAsync(
            AiEmbeddingSyncScopes.IncrementalRefresh,
            embeddingVersionId,
            cancellationToken);

        return existing ?? new AiEmbeddingSyncState
        {
            SyncScope = AiEmbeddingSyncScopes.IncrementalRefresh,
            EmbeddingVersionId = embeddingVersionId,
        };
    }

    private async Task MarkSyncFailureAsync(
        AiEmbeddingSyncState syncState,
        string error,
        CancellationToken cancellationToken)
    {
        syncState.LastError = error;
        syncState.LastScanCompletedUtc = DateTime.UtcNow;
        syncState.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SyncState.UpsertAsync(syncState, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static AiEmbeddingSyncStateModel MapSyncState(AiEmbeddingSyncState entity) => new()
    {
        SyncScope = entity.SyncScope,
        EmbeddingVersionId = entity.EmbeddingVersionId,
        LastSuccessfulSyncUtc = entity.LastSuccessfulSyncUtc,
        LastScanStartedUtc = entity.LastScanStartedUtc,
        LastScanCompletedUtc = entity.LastScanCompletedUtc,
        LastDetectedCount = entity.LastDetectedCount,
        LastEnqueuedCount = entity.LastEnqueuedCount,
        LastProcessedCount = entity.LastProcessedCount,
        LastSkippedCount = entity.LastSkippedCount,
        LastFailedCount = entity.LastFailedCount,
        LastJobId = entity.LastJobId,
        LastError = entity.LastError,
    };

    private static AiEmbeddingVersionModel MapVersion(AiEmbeddingVersion entity) => new()
    {
        EmbeddingVersionId = entity.EmbeddingVersionId,
        VersionCode = entity.VersionCode,
        ModelProvider = entity.ModelProvider,
        ModelName = entity.ModelName,
        ModelVersion = entity.ModelVersion,
        DimensionCount = entity.DimensionCount,
        VectorFormat = entity.VectorFormat,
        IsActive = entity.IsActive,
        IsCurrent = entity.IsCurrent,
        Status = entity.Status,
        Description = entity.Description,
        CreatedDate = entity.CreatedDate,
        UpdatedDate = entity.UpdatedDate,
    };
}
