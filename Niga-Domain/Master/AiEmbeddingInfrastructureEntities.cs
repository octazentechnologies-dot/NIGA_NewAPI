namespace Niga_Domain.Master;

public partial class AiEmbeddingVersion
{
    public Guid EmbeddingVersionId { get; set; }

    public string VersionCode { get; set; } = null!;

    public string ModelProvider { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string? ModelVersion { get; set; }

    public int DimensionCount { get; set; }

    public string VectorFormat { get; set; } = "JsonFloatArray";

    public bool IsActive { get; set; } = true;

    public bool IsCurrent { get; set; }

    public string Status { get; set; } = "Draft";

    public string? Description { get; set; }

    public string? ConfigurationJson { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; }
}

public partial class AiRubricEmbedding
{
    public long RubricEmbeddingId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public int RubricId { get; set; }

    public string SourceText { get; set; } = null!;

    public string TextHash { get; set; } = null!;

    public string? EmbeddingPayloadJson { get; set; }

    public int DimensionCount { get; set; }

    public int RevisionNo { get; set; } = 1;

    public string Status { get; set; } = "Pending";

    public string SourceType { get; set; } = "SubSectionName";

    public Guid? LastJobId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; }
}

public partial class AiConceptEmbedding
{
    public long ConceptEmbeddingId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string ConceptKey { get; set; } = null!;

    public string ConceptType { get; set; } = null!;

    public string? SourceDomain { get; set; }

    public string SourceText { get; set; } = null!;

    public string TextHash { get; set; } = null!;

    public string? EmbeddingPayloadJson { get; set; }

    public int DimensionCount { get; set; }

    public int RevisionNo { get; set; } = 1;

    public string Status { get; set; } = "Pending";

    public Guid? LastJobId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; }
}

public partial class AiEmbeddingJob
{
    public Guid JobId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string JobType { get; set; } = null!;

    public string? TriggerSource { get; set; }

    public string Status { get; set; } = "Queued";

    public int Priority { get; set; } = 100;

    public int TotalItems { get; set; }

    public int ProcessedItems { get; set; }

    public int FailedItems { get; set; }

    public int SkippedItems { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; } = 3;

    public string? CorrelationId { get; set; }

    public string? ErrorSummary { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; }
}

public partial class AiEmbeddingQueue
{
    public long QueueId { get; set; }

    public Guid JobId { get; set; }

    public string ItemType { get; set; } = null!;

    public int? RubricId { get; set; }

    public string? ConceptKey { get; set; }

    public string? ConceptType { get; set; }

    public string? PayloadJson { get; set; }

    public string Status { get; set; } = "Pending";

    public int Priority { get; set; } = 100;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; } = 3;

    public DateTime? NextRetryAtUtc { get; set; }

    public DateTime? LockedUntilUtc { get; set; }

    public string? LockedBy { get; set; }

    public string? LastError { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? DeletedDate { get; set; }

    public bool IsDeleted { get; set; }
}

public partial class AiEmbeddingAudit
{
    public long AuditId { get; set; }

    public string EntityType { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public Guid? JobId { get; set; }

    public long? QueueId { get; set; }

    public Guid? EmbeddingVersionId { get; set; }

    public int? ActorUserId { get; set; }

    public string? CorrelationId { get; set; }

    public string? DetailsJson { get; set; }

    public DateTime CreatedDate { get; set; }
}

public partial class AiEmbeddingStatistics
{
    public long StatId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public DateOnly StatDate { get; set; }

    public int RubricEmbeddingCount { get; set; }

    public int ConceptEmbeddingCount { get; set; }

    public int ActiveRubricEmbeddings { get; set; }

    public int ActiveConceptEmbeddings { get; set; }

    public int QueuePendingCount { get; set; }

    public int QueueProcessingCount { get; set; }

    public int QueueFailedCount { get; set; }

    public int QueueDeadLetterCount { get; set; }

    public int JobsCompleted { get; set; }

    public int JobsFailed { get; set; }

    public int? AvgProcessingMs { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public partial class AiEmbeddingSyncState
{
    public long SyncStateId { get; set; }

    public string SyncScope { get; set; } = null!;

    public Guid? EmbeddingVersionId { get; set; }

    public DateTime? LastSuccessfulSyncUtc { get; set; }

    public DateTime? LastScanStartedUtc { get; set; }

    public DateTime? LastScanCompletedUtc { get; set; }

    public int LastDetectedCount { get; set; }

    public int LastEnqueuedCount { get; set; }

    public int LastProcessedCount { get; set; }

    public int LastSkippedCount { get; set; }

    public int LastFailedCount { get; set; }

    public string? LastRunCorrelationId { get; set; }

    public Guid? LastJobId { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
