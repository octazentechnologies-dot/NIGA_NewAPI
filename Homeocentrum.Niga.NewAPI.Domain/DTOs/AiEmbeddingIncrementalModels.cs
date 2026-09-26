using System.Text.Json.Serialization;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public static class AiEmbeddingSyncScopes
{
    public const string IncrementalRefresh = "IncrementalRefresh";
}

public static class AiEmbeddingQueueChangeTypes
{
    public const string New = "New";
    public const string Updated = "Updated";
    public const string Deleted = "Deleted";
    public const string Restored = "Restored";
    public const string EnrichmentChanged = "EnrichmentChanged";
    public const string HashMismatch = "HashMismatch";
}

public static class AiEmbeddingChangeReasons
{
    public const string CreatedDate = "CreatedDate";
    public const string UpdatedDate = "UpdatedDate";
    public const string DeletedDate = "DeletedDate";
    public const string RestoredDate = "RestoredDate";
    public const string HashComparison = "HashComparison";
    public const string NewSynonym = "NewSynonym";
    public const string DoctorApprovedConcept = "DoctorApprovedConcept";
    public const string SectionUpdated = "SectionUpdated";
    public const string BootstrapMappingChanged = "BootstrapMappingChanged";
}

public class RubricEmbeddingChangeCandidate
{
    public int RubricId { get; set; }

    public string ChangeType { get; set; } = AiEmbeddingQueueChangeTypes.Updated;

    public List<string> Reasons { get; set; } = new();

    public DateTime DetectedAtUtc { get; set; } = DateTime.UtcNow;

    public bool RequiresEmbedding { get; set; } = true;

    public bool RequiresArchive { get; set; }
}

public class AiEmbeddingQueuePayload
{
    [JsonPropertyName("changeType")]
    public string ChangeType { get; set; } = AiEmbeddingQueueChangeTypes.Updated;

    [JsonPropertyName("reasons")]
    public List<string> Reasons { get; set; } = new();

    [JsonPropertyName("detectedAtUtc")]
    public DateTime DetectedAtUtc { get; set; }

    [JsonPropertyName("expectedTextHash")]
    public string? ExpectedTextHash { get; set; }
}

public class IncrementalEmbeddingRefreshRequest
{
    public Guid? EmbeddingVersionId { get; set; }

    public int? ActorUserId { get; set; }

    public string? TriggerSource { get; set; }

    public bool DetectOnly { get; set; }

    public bool ProcessOnly { get; set; }

    public Guid? JobId { get; set; }

    public int? MaxQueueItems { get; set; }
}

public class IncrementalEmbeddingRefreshResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public Guid? JobId { get; set; }

    public Guid EmbeddingVersionId { get; set; }

    public string VersionCode { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public DateTime? WatermarkUtc { get; set; }

    public int DetectedCount { get; set; }

    public int EnqueuedCount { get; set; }

    public int ProcessedCount { get; set; }

    public int CreatedCount { get; set; }

    public int UpdatedCount { get; set; }

    public int SkippedCount { get; set; }

    public int ArchivedCount { get; set; }

    public int FailedCount { get; set; }

    public int DeadLetterCount { get; set; }

    public DateTime? CompletedAtUtc { get; set; }
}

public class AiEmbeddingSyncStateModel
{
    public string SyncScope { get; set; } = string.Empty;

    public Guid? EmbeddingVersionId { get; set; }

    public DateTime? LastSuccessfulSyncUtc { get; set; }

    public DateTime? LastScanStartedUtc { get; set; }

    public DateTime? LastScanCompletedUtc { get; set; }

    public int LastDetectedCount { get; set; }

    public int LastEnqueuedCount { get; set; }

    public int LastProcessedCount { get; set; }

    public int LastSkippedCount { get; set; }

    public int LastFailedCount { get; set; }

    public Guid? LastJobId { get; set; }

    public string? LastError { get; set; }
}
