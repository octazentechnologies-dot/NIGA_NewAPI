namespace Niga_Domain.DTOs;

public static class AiEmbeddingEntityTypes
{
    public const string Version = "EmbeddingVersion";
    public const string RubricEmbedding = "RubricEmbedding";
    public const string ConceptEmbedding = "ConceptEmbedding";
    public const string Job = "EmbeddingJob";
    public const string Queue = "EmbeddingQueue";
    public const string Statistics = "EmbeddingStatistics";
}

public static class AiEmbeddingStatuses
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Deprecated = "Deprecated";
    public const string Archived = "Archived";
    public const string Pending = "Pending";
    public const string Superseded = "Superseded";
    public const string Failed = "Failed";
    public const string Queued = "Queued";
    public const string Running = "Running";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Processing = "Processing";
    public const string DeadLetter = "DeadLetter";
    public const string Skipped = "Skipped";
}

public static class AiEmbeddingJobTypes
{
    public const string FullReindex = "FullReindex";
    public const string Incremental = "Incremental";
    public const string RubricOnly = "RubricOnly";
    public const string ConceptOnly = "ConceptOnly";
}

public static class AiEmbeddingQueueItemTypes
{
    public const string Rubric = "Rubric";
    public const string Concept = "Concept";
}

public class AiEmbeddingVersionModel
{
    public Guid EmbeddingVersionId { get; set; }

    public string VersionCode { get; set; } = string.Empty;

    public string ModelProvider { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string? ModelVersion { get; set; }

    public int DimensionCount { get; set; }

    public string VectorFormat { get; set; } = "JsonFloatArray";

    public bool IsActive { get; set; }

    public bool IsCurrent { get; set; }

    public string Status { get; set; } = AiEmbeddingStatuses.Draft;

    public string? Description { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public class CreateAiEmbeddingVersionRequest
{
    public string VersionCode { get; set; } = string.Empty;

    public string ModelProvider { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string? ModelVersion { get; set; }

    public int DimensionCount { get; set; }

    public string? Description { get; set; }

    public string? ConfigurationJson { get; set; }

    public int? CreatedByUserId { get; set; }
}

public class AiRubricEmbeddingRecordModel
{
    public long RubricEmbeddingId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public int RubricId { get; set; }

    public string SourceText { get; set; } = string.Empty;

    public string TextHash { get; set; } = string.Empty;

    public int DimensionCount { get; set; }

    public int RevisionNo { get; set; }

    public string Status { get; set; } = AiEmbeddingStatuses.Pending;

    public Guid? LastJobId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public class AiConceptEmbeddingRecordModel
{
    public long ConceptEmbeddingId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string ConceptKey { get; set; } = string.Empty;

    public string ConceptType { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public string TextHash { get; set; } = string.Empty;

    public int DimensionCount { get; set; }

    public int RevisionNo { get; set; }

    public string Status { get; set; } = AiEmbeddingStatuses.Pending;

    public Guid? LastJobId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public class AiEmbeddingJobModel
{
    public Guid JobId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string JobType { get; set; } = string.Empty;

    public string Status { get; set; } = AiEmbeddingStatuses.Queued;

    public int Priority { get; set; }

    public int TotalItems { get; set; }

    public int ProcessedItems { get; set; }

    public int FailedItems { get; set; }

    public int SkippedItems { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; }

    public string? CorrelationId { get; set; }

    public string? ErrorSummary { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class CreateAiEmbeddingJobRequest
{
    public Guid EmbeddingVersionId { get; set; }

    public string JobType { get; set; } = AiEmbeddingJobTypes.Incremental;

    public string? TriggerSource { get; set; }

    public int Priority { get; set; } = 100;

    public int MaxRetries { get; set; } = 3;

    public string? CorrelationId { get; set; }

    public int? CreatedByUserId { get; set; }
}

public class EnqueueAiEmbeddingQueueRequest
{
    public Guid JobId { get; set; }

    public string ItemType { get; set; } = AiEmbeddingQueueItemTypes.Rubric;

    public int? RubricId { get; set; }

    public string? ConceptKey { get; set; }

    public string? ConceptType { get; set; }

    public string? SourceText { get; set; }

    public string? PayloadJson { get; set; }

    public int Priority { get; set; } = 100;

    public int MaxAttempts { get; set; } = 3;
}

public class AiEmbeddingQueueItemModel
{
    public long QueueId { get; set; }

    public Guid JobId { get; set; }

    public string ItemType { get; set; } = string.Empty;

    public int? RubricId { get; set; }

    public string? ConceptKey { get; set; }

    public string? ConceptType { get; set; }

    public string Status { get; set; } = AiEmbeddingStatuses.Pending;

    public int AttemptCount { get; set; }

    public int MaxAttempts { get; set; }

    public DateTime? NextRetryAtUtc { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class AiEmbeddingAuditModel
{
    public long AuditId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public Guid? JobId { get; set; }

    public long? QueueId { get; set; }

    public Guid? EmbeddingVersionId { get; set; }

    public DateTime CreatedDate { get; set; }
}

public class AppendAiEmbeddingAuditRequest
{
    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? OldStatus { get; set; }

    public string? NewStatus { get; set; }

    public Guid? JobId { get; set; }

    public long? QueueId { get; set; }

    public Guid? EmbeddingVersionId { get; set; }

    public int? ActorUserId { get; set; }

    public string? CorrelationId { get; set; }

    public string? DetailsJson { get; set; }
}

public class AiEmbeddingStatisticsModel
{
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
}

public class AiEmbeddingInfrastructureStatusModel
{
    public bool Enabled { get; set; }

    public bool LegacyRubricEmbeddingsActive { get; set; }

    public AiEmbeddingVersionModel? CurrentVersion { get; set; }

    public int ActiveVersions { get; set; }

    public int PendingQueueItems { get; set; }

    public int RunningJobs { get; set; }

    public int FailedJobsLast24Hours { get; set; }

    public AiEmbeddingStatisticsModel? TodayStatistics { get; set; }
}

public class AiEmbeddingOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public static AiEmbeddingOperationResult Ok(string message = "Success") =>
        new() { Success = true, Message = message };

    public static AiEmbeddingOperationResult Fail(string message) =>
        new() { Success = false, Message = message };
}
