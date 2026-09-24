using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class AiEmbeddingVersionService : IAiEmbeddingVersionService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IAiEmbeddingAuditService _auditService;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEmbeddingVersionService> _logger;

    public AiEmbeddingVersionService(
        IAiEmbeddingUnitOfWork unitOfWork,
        IAiEmbeddingAuditService auditService,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEmbeddingVersionService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AiEmbeddingVersionModel? Result)> RegisterVersionAsync(
        CreateAiEmbeddingVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return (false, "AI embedding infrastructure is disabled.", null);

        if (string.IsNullOrWhiteSpace(request.VersionCode))
            return (false, "VersionCode is required.", null);

        if (await _unitOfWork.Versions.VersionCodeExistsAsync(request.VersionCode.Trim(), cancellationToken))
            return (false, $"VersionCode '{request.VersionCode}' already exists.", null);

        var entity = new AiEmbeddingVersion
        {
            EmbeddingVersionId = Guid.NewGuid(),
            VersionCode = request.VersionCode.Trim(),
            ModelProvider = string.IsNullOrWhiteSpace(request.ModelProvider) ? _options.DefaultModelProvider : request.ModelProvider.Trim(),
            ModelName = string.IsNullOrWhiteSpace(request.ModelName) ? _options.DefaultModelName : request.ModelName.Trim(),
            ModelVersion = request.ModelVersion,
            DimensionCount = request.DimensionCount > 0 ? request.DimensionCount : _options.DefaultDimensionCount,
            VectorFormat = _options.DefaultVectorFormat,
            Description = request.Description,
            ConfigurationJson = request.ConfigurationJson,
            CreatedByUserId = request.CreatedByUserId,
            Status = AiEmbeddingStatuses.Draft,
            IsActive = true,
            IsCurrent = false,
        };

        await _unitOfWork.Versions.AddAsync(entity, cancellationToken);
        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Version,
            EntityId = entity.EmbeddingVersionId.ToString("D"),
            Action = "Create",
            NewStatus = entity.Status,
            EmbeddingVersionId = entity.EmbeddingVersionId,
            ActorUserId = request.CreatedByUserId,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Registered embedding version {VersionCode} ({VersionId})", entity.VersionCode, entity.EmbeddingVersionId);
        return (true, "Embedding version registered.", MapVersion(entity));
    }

    public async Task<(bool Success, string Message)> ActivateVersionAsync(
        Guid embeddingVersionId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var version = await _unitOfWork.Versions.GetByIdAsync(embeddingVersionId, cancellationToken);
        if (version == null) return (false, "Embedding version not found.");

        var oldStatus = version.Status;
        await _unitOfWork.Versions.SetCurrentAsync(embeddingVersionId, cancellationToken);
        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Version,
            EntityId = embeddingVersionId.ToString("D"),
            Action = "Activate",
            OldStatus = oldStatus,
            NewStatus = AiEmbeddingStatuses.Active,
            EmbeddingVersionId = embeddingVersionId,
            ActorUserId = actorUserId,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Activated embedding version {VersionId}", embeddingVersionId);
        return (true, "Embedding version activated.");
    }

    public async Task<AiEmbeddingVersionModel?> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        var current = await _unitOfWork.Versions.GetCurrentAsync(cancellationToken);
        return current == null ? null : MapVersion(current);
    }

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

public class AiEmbeddingJobService : IAiEmbeddingJobService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IAiEmbeddingAuditService _auditService;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEmbeddingJobService> _logger;

    public AiEmbeddingJobService(
        IAiEmbeddingUnitOfWork unitOfWork,
        IAiEmbeddingAuditService auditService,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEmbeddingJobService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, AiEmbeddingJobModel? Result)> CreateJobAsync(
        CreateAiEmbeddingJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return (false, "AI embedding infrastructure is disabled.", null);

        var version = await _unitOfWork.Versions.GetByIdAsync(request.EmbeddingVersionId, cancellationToken);
        if (version == null)
            return (false, "Embedding version not found.", null);

        var entity = new AiEmbeddingJob
        {
            JobId = Guid.NewGuid(),
            EmbeddingVersionId = request.EmbeddingVersionId,
            JobType = request.JobType,
            TriggerSource = request.TriggerSource,
            Status = AiEmbeddingStatuses.Queued,
            Priority = request.Priority > 0 ? request.Priority : _options.DefaultJobPriority,
            MaxRetries = request.MaxRetries > 0 ? request.MaxRetries : _options.DefaultMaxJobRetries,
            CorrelationId = request.CorrelationId ?? Guid.NewGuid().ToString("N")[..12],
            CreatedByUserId = request.CreatedByUserId,
        };

        await _unitOfWork.Jobs.AddAsync(entity, cancellationToken);
        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Job,
            EntityId = entity.JobId.ToString("D"),
            Action = "Create",
            NewStatus = entity.Status,
            JobId = entity.JobId,
            EmbeddingVersionId = entity.EmbeddingVersionId,
            ActorUserId = request.CreatedByUserId,
            CorrelationId = entity.CorrelationId,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created embedding job {JobId} type={JobType}", entity.JobId, entity.JobType);
        return (true, "Embedding job created.", MapJob(entity));
    }

    public async Task<(bool Success, string Message)> MarkJobRunningAsync(
        Guid jobId, int? actorUserId = null, CancellationToken cancellationToken = default) =>
        await UpdateJobStatusAsync(jobId, AiEmbeddingStatuses.Running, null, actorUserId, cancellationToken, setStarted: true);

    public async Task<(bool Success, string Message)> MarkJobCompletedAsync(
        Guid jobId, int? actorUserId = null, CancellationToken cancellationToken = default) =>
        await UpdateJobStatusAsync(jobId, AiEmbeddingStatuses.Completed, null, actorUserId, cancellationToken, setCompleted: true);

    public async Task<(bool Success, string Message)> MarkJobFailedAsync(
        Guid jobId, string errorSummary, int? actorUserId = null, CancellationToken cancellationToken = default) =>
        await UpdateJobStatusAsync(jobId, AiEmbeddingStatuses.Failed, errorSummary, actorUserId, cancellationToken, setCompleted: true);

    private async Task<(bool Success, string Message)> UpdateJobStatusAsync(
        Guid jobId,
        string newStatus,
        string? errorSummary,
        int? actorUserId,
        CancellationToken cancellationToken,
        bool setStarted = false,
        bool setCompleted = false)
    {
        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null) return (false, "Embedding job not found.");

        var oldStatus = job.Status;
        job.Status = newStatus;
        job.UpdatedDate = DateTime.UtcNow;
        if (setStarted) job.StartedAtUtc = DateTime.UtcNow;
        if (setCompleted) job.CompletedAtUtc = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(errorSummary)) job.ErrorSummary = errorSummary;

        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Job,
            EntityId = jobId.ToString("D"),
            Action = "StatusChange",
            OldStatus = oldStatus,
            NewStatus = newStatus,
            JobId = jobId,
            EmbeddingVersionId = job.EmbeddingVersionId,
            ActorUserId = actorUserId,
            CorrelationId = job.CorrelationId,
            DetailsJson = errorSummary,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, $"Job status updated to {newStatus}.");
    }

    private static AiEmbeddingJobModel MapJob(AiEmbeddingJob entity) => new()
    {
        JobId = entity.JobId,
        EmbeddingVersionId = entity.EmbeddingVersionId,
        JobType = entity.JobType,
        Status = entity.Status,
        Priority = entity.Priority,
        TotalItems = entity.TotalItems,
        ProcessedItems = entity.ProcessedItems,
        FailedItems = entity.FailedItems,
        SkippedItems = entity.SkippedItems,
        RetryCount = entity.RetryCount,
        MaxRetries = entity.MaxRetries,
        CorrelationId = entity.CorrelationId,
        ErrorSummary = entity.ErrorSummary,
        StartedAtUtc = entity.StartedAtUtc,
        CompletedAtUtc = entity.CompletedAtUtc,
        CreatedDate = entity.CreatedDate,
    };
}

public class AiEmbeddingQueueService : IAiEmbeddingQueueService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IAiEmbeddingAuditService _auditService;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<AiEmbeddingQueueService> _logger;

    public AiEmbeddingQueueService(
        IAiEmbeddingUnitOfWork unitOfWork,
        IAiEmbeddingAuditService auditService,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<AiEmbeddingQueueService> logger)
    {
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, long? QueueId)> EnqueueAsync(
        EnqueueAiEmbeddingQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return (false, "AI embedding infrastructure is disabled.", null);

        var job = await _unitOfWork.Jobs.GetByIdAsync(request.JobId, cancellationToken);
        if (job == null) return (false, "Embedding job not found.", null);

        var entity = new AiEmbeddingQueue
        {
            JobId = request.JobId,
            ItemType = request.ItemType,
            RubricId = request.RubricId,
            ConceptKey = request.ConceptKey,
            ConceptType = request.ConceptType,
            PayloadJson = request.PayloadJson,
            Priority = request.Priority > 0 ? request.Priority : _options.DefaultJobPriority,
            MaxAttempts = request.MaxAttempts > 0 ? request.MaxAttempts : _options.DefaultMaxQueueAttempts,
            Status = AiEmbeddingStatuses.Pending,
        };

        await _unitOfWork.Queue.AddRangeAsync(new[] { entity }, cancellationToken);
        job.TotalItems += 1;
        job.UpdatedDate = DateTime.UtcNow;

        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Queue,
            EntityId = "pending",
            Action = "Enqueue",
            NewStatus = entity.Status,
            JobId = request.JobId,
            EmbeddingVersionId = job.EmbeddingVersionId,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, "Queue item created.", entity.QueueId);
    }

    public async Task<(bool Success, string Message, List<AiEmbeddingQueueItemModel> Items)> DequeueBatchAsync(
        string workerId,
        int? batchSize = null,
        CancellationToken cancellationToken = default)
    {
        var size = batchSize ?? _options.QueueBatchSize;
        var items = await _unitOfWork.Queue.DequeueBatchAsync(
            size,
            workerId,
            TimeSpan.FromMinutes(_options.QueueLockMinutes),
            cancellationToken);

        if (items.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, $"{items.Count} queue items dequeued.", items.Select(MapQueue).ToList());
    }

    public async Task<(bool Success, string Message)> MarkCompletedAsync(
        long queueId, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Queue.GetByIdAsync(queueId, cancellationToken);
        if (item == null) return (false, "Queue item not found.");

        var oldStatus = item.Status;
        item.Status = AiEmbeddingStatuses.Completed;
        item.CompletedAtUtc = DateTime.UtcNow;
        item.UpdatedDate = DateTime.UtcNow;
        item.LockedUntilUtc = null;
        item.LockedBy = null;

        var job = await _unitOfWork.Jobs.GetByIdAsync(item.JobId, cancellationToken);
        if (job != null)
        {
            job.ProcessedItems += 1;
            job.UpdatedDate = DateTime.UtcNow;
        }

        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Queue,
            EntityId = queueId.ToString(),
            Action = "Complete",
            OldStatus = oldStatus,
            NewStatus = item.Status,
            JobId = item.JobId,
            QueueId = queueId,
            ActorUserId = actorUserId,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, "Queue item completed.");
    }

    public async Task<(bool Success, string Message)> MarkSkippedAsync(
        long queueId, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Queue.GetByIdAsync(queueId, cancellationToken);
        if (item == null) return (false, "Queue item not found.");

        var oldStatus = item.Status;
        item.Status = AiEmbeddingStatuses.Skipped;
        item.CompletedAtUtc = DateTime.UtcNow;
        item.UpdatedDate = DateTime.UtcNow;
        item.LockedUntilUtc = null;
        item.LockedBy = null;

        var job = await _unitOfWork.Jobs.GetByIdAsync(item.JobId, cancellationToken);
        if (job != null)
        {
            job.SkippedItems += 1;
            job.UpdatedDate = DateTime.UtcNow;
        }

        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Queue,
            EntityId = queueId.ToString(),
            Action = "Skip",
            OldStatus = oldStatus,
            NewStatus = item.Status,
            JobId = item.JobId,
            QueueId = queueId,
            ActorUserId = actorUserId,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (true, "Queue item skipped.");
    }

    public async Task<(bool Success, string Message, int EnqueuedCount)> EnqueueBatchAsync(
        IReadOnlyList<EnqueueAiEmbeddingQueueRequest> requests,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            return (false, "AI embedding infrastructure is disabled.", 0);

        if (requests.Count == 0)
            return (true, "No queue items to enqueue.", 0);

        var jobId = requests[0].JobId;
        var job = await _unitOfWork.Jobs.GetByIdAsync(jobId, cancellationToken);
        if (job == null)
            return (false, "Embedding job not found.", 0);

        var entities = requests.Select(request => new AiEmbeddingQueue
        {
            JobId = request.JobId,
            ItemType = request.ItemType,
            RubricId = request.RubricId,
            ConceptKey = request.ConceptKey,
            ConceptType = request.ConceptType,
            PayloadJson = request.PayloadJson,
            Priority = request.Priority > 0 ? request.Priority : _options.DefaultJobPriority,
            MaxAttempts = request.MaxAttempts > 0 ? request.MaxAttempts : _options.DefaultMaxQueueAttempts,
            Status = AiEmbeddingStatuses.Pending,
        }).ToList();

        await _unitOfWork.Queue.AddRangeAsync(entities, cancellationToken);
        job.TotalItems += entities.Count;
        job.UpdatedDate = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return (true, $"{entities.Count} queue items created.", entities.Count);
    }

    public async Task<(bool Success, string Message)> MarkFailedAsync(
        long queueId, string error, int? actorUserId = null, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Queue.GetByIdAsync(queueId, cancellationToken);
        if (item == null) return (false, "Queue item not found.");

        var oldStatus = item.Status;
        item.AttemptCount += 1;
        item.LastError = error;
        item.UpdatedDate = DateTime.UtcNow;
        item.LockedUntilUtc = null;
        item.LockedBy = null;
        item.Status = AiEmbeddingRetryPolicy.ResolveQueueStatusAfterFailure(item.AttemptCount, item.MaxAttempts);
        item.NextRetryAtUtc = item.Status == AiEmbeddingStatuses.Pending
            ? AiEmbeddingRetryPolicy.ComputeNextRetryUtc(item.AttemptCount, _options)
            : null;

        var job = await _unitOfWork.Jobs.GetByIdAsync(item.JobId, cancellationToken);
        if (job != null)
        {
            if (item.Status == AiEmbeddingStatuses.DeadLetter)
                job.FailedItems += 1;
            job.UpdatedDate = DateTime.UtcNow;
        }

        await _auditService.AppendAsync(new AppendAiEmbeddingAuditRequest
        {
            EntityType = AiEmbeddingEntityTypes.Queue,
            EntityId = queueId.ToString(),
            Action = "Fail",
            OldStatus = oldStatus,
            NewStatus = item.Status,
            JobId = item.JobId,
            QueueId = queueId,
            ActorUserId = actorUserId,
            DetailsJson = error,
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Queue item {QueueId} failed attempt {Attempt}/{MaxAttempts}: {Error}",
            queueId, item.AttemptCount, item.MaxAttempts, error);
        return (true, $"Queue item marked {item.Status}.");
    }

    private static AiEmbeddingQueueItemModel MapQueue(AiEmbeddingQueue entity) => new()
    {
        QueueId = entity.QueueId,
        JobId = entity.JobId,
        ItemType = entity.ItemType,
        RubricId = entity.RubricId,
        ConceptKey = entity.ConceptKey,
        ConceptType = entity.ConceptType,
        Status = entity.Status,
        AttemptCount = entity.AttemptCount,
        MaxAttempts = entity.MaxAttempts,
        NextRetryAtUtc = entity.NextRetryAtUtc,
        CreatedDate = entity.CreatedDate,
    };
}

public class AiEmbeddingAuditService : IAiEmbeddingAuditService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;

    public AiEmbeddingAuditService(IAiEmbeddingUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task AppendAsync(AppendAiEmbeddingAuditRequest request, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.Audits.AddAsync(new AiEmbeddingAudit
        {
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Action = request.Action,
            OldStatus = request.OldStatus,
            NewStatus = request.NewStatus,
            JobId = request.JobId,
            QueueId = request.QueueId,
            EmbeddingVersionId = request.EmbeddingVersionId,
            ActorUserId = request.ActorUserId,
            CorrelationId = request.CorrelationId,
            DetailsJson = request.DetailsJson,
        }, cancellationToken);
    }
}

public class AiEmbeddingStatisticsService : IAiEmbeddingStatisticsService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;

    public AiEmbeddingStatisticsService(IAiEmbeddingUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task RefreshDailySnapshotAsync(Guid embeddingVersionId, DateOnly? statDate = null, CancellationToken cancellationToken = default)
    {
        var date = statDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var rubricEmbeddings = await _unitOfWork.RubricEmbeddings.ListByVersionAsync(embeddingVersionId, cancellationToken);
        var conceptEmbeddings = await _unitOfWork.ConceptEmbeddings.ListByVersionAsync(embeddingVersionId, cancellationToken);

        var entity = new AiEmbeddingStatistics
        {
            EmbeddingVersionId = embeddingVersionId,
            StatDate = date,
            RubricEmbeddingCount = rubricEmbeddings.Count,
            ConceptEmbeddingCount = conceptEmbeddings.Count,
            ActiveRubricEmbeddings = await _unitOfWork.RubricEmbeddings.CountActiveByVersionAsync(embeddingVersionId, cancellationToken),
            ActiveConceptEmbeddings = await _unitOfWork.ConceptEmbeddings.CountActiveByVersionAsync(embeddingVersionId, cancellationToken),
            QueuePendingCount = await _unitOfWork.Queue.CountPendingAsync(cancellationToken),
            QueueProcessingCount = await _unitOfWork.Queue.CountByStatusAsync(AiEmbeddingStatuses.Processing, cancellationToken),
            QueueFailedCount = await _unitOfWork.Queue.CountByStatusAsync(AiEmbeddingStatuses.Failed, cancellationToken),
            QueueDeadLetterCount = await _unitOfWork.Queue.CountByStatusAsync(AiEmbeddingStatuses.DeadLetter, cancellationToken),
            JobsCompleted = 0,
            JobsFailed = 0,
        };

        await _unitOfWork.Statistics.UpsertAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiEmbeddingStatisticsModel?> GetTodayAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var stat = await _unitOfWork.Statistics.GetByVersionAndDateAsync(embeddingVersionId, today, cancellationToken);
        return stat == null ? null : Map(stat);
    }

    private static AiEmbeddingStatisticsModel Map(AiEmbeddingStatistics entity) => new()
    {
        EmbeddingVersionId = entity.EmbeddingVersionId,
        StatDate = entity.StatDate,
        RubricEmbeddingCount = entity.RubricEmbeddingCount,
        ConceptEmbeddingCount = entity.ConceptEmbeddingCount,
        ActiveRubricEmbeddings = entity.ActiveRubricEmbeddings,
        ActiveConceptEmbeddings = entity.ActiveConceptEmbeddings,
        QueuePendingCount = entity.QueuePendingCount,
        QueueProcessingCount = entity.QueueProcessingCount,
        QueueFailedCount = entity.QueueFailedCount,
        QueueDeadLetterCount = entity.QueueDeadLetterCount,
        JobsCompleted = entity.JobsCompleted,
        JobsFailed = entity.JobsFailed,
        AvgProcessingMs = entity.AvgProcessingMs,
    };
}

public class AiEmbeddingInfrastructureService : IAiEmbeddingInfrastructureService
{
    private readonly IAiEmbeddingUnitOfWork _unitOfWork;
    private readonly IAiEmbeddingVersionService _versionService;
    private readonly IAiEmbeddingStatisticsService _statisticsService;
    private readonly AiEmbeddingInfrastructureOptions _options;

    public AiEmbeddingInfrastructureService(
        IAiEmbeddingUnitOfWork unitOfWork,
        IAiEmbeddingVersionService versionService,
        IAiEmbeddingStatisticsService statisticsService,
        IOptions<AiEmbeddingInfrastructureOptions> options)
    {
        _unitOfWork = unitOfWork;
        _versionService = versionService;
        _statisticsService = statisticsService;
        _options = options.Value;
    }

    public async Task<AiEmbeddingInfrastructureStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var current = await _versionService.GetCurrentVersionAsync(cancellationToken);
        AiEmbeddingStatisticsModel? today = null;
        if (current != null)
            today = await _statisticsService.GetTodayAsync(current.EmbeddingVersionId, cancellationToken);

        var activeVersions = (await _unitOfWork.Versions.ListActiveAsync(cancellationToken)).Count;
        var pendingQueue = await _unitOfWork.Queue.CountPendingAsync(cancellationToken);
        var runningJobs = await _unitOfWork.Jobs.CountRunningAsync(cancellationToken);
        var failedJobs = await _unitOfWork.Jobs.CountFailedSinceAsync(DateTime.UtcNow.AddHours(-24), cancellationToken);

        return new AiEmbeddingInfrastructureStatusModel
        {
            Enabled = _options.Enabled,
            LegacyRubricEmbeddingsActive = _options.KeepLegacyRubricEmbeddingsActive,
            CurrentVersion = current,
            ActiveVersions = activeVersions,
            PendingQueueItems = pendingQueue,
            RunningJobs = runningJobs,
            FailedJobsLast24Hours = failedJobs,
            TodayStatistics = today,
        };
    }
}
