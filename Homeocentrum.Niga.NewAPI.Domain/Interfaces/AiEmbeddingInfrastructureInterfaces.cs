using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IAiEmbeddingUnitOfWork : IAsyncDisposable
{
    IAiEmbeddingVersionRepository Versions { get; }
    IAiRubricEmbeddingRepository RubricEmbeddings { get; }
    IAiConceptEmbeddingRepository ConceptEmbeddings { get; }
    IAiEmbeddingJobRepository Jobs { get; }
    IAiEmbeddingQueueRepository Queue { get; }
    IAiEmbeddingAuditRepository Audits { get; }
    IAiEmbeddingStatisticsRepository Statistics { get; }
    IAiEmbeddingSyncStateRepository SyncState { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingVersionRepository
{
    Task<AiEmbeddingVersion?> GetByIdAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task<AiEmbeddingVersion?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<List<AiEmbeddingVersion>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> VersionCodeExistsAsync(string versionCode, CancellationToken cancellationToken = default);
    Task AddAsync(AiEmbeddingVersion entity, CancellationToken cancellationToken = default);
    Task SetCurrentAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(AiEmbeddingVersion entity, CancellationToken cancellationToken = default);
}

public interface IAiRubricEmbeddingRepository
{
    Task<AiRubricEmbedding?> GetByIdAsync(long rubricEmbeddingId, CancellationToken cancellationToken = default);
    Task<AiRubricEmbedding?> GetActiveByRubricAsync(Guid embeddingVersionId, int rubricId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, string>> GetActiveTextHashesAsync(Guid embeddingVersionId, IEnumerable<int> rubricIds, CancellationToken cancellationToken = default);
    Task<List<AiRubricEmbedding>> ListByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task AddAsync(AiRubricEmbedding entity, CancellationToken cancellationToken = default);
    Task<AiRubricEmbeddingUpsertOutcome> UpsertActiveEmbeddingAsync(
        Guid embeddingVersionId,
        int rubricId,
        string sourceText,
        string textHash,
        float[] vector,
        int dimensionCount,
        Guid jobId,
        CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(AiRubricEmbedding entity, CancellationToken cancellationToken = default);
    Task<bool> ArchiveActiveByRubricAsync(Guid embeddingVersionId, int rubricId, Guid jobId, CancellationToken cancellationToken = default);
    Task<int> CountActiveByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task<List<AiEnterpriseRubricEmbeddingCacheEntry>> LoadActiveCacheEntriesAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
}

public interface IAiConceptEmbeddingRepository
{
    Task<AiConceptEmbedding?> GetByIdAsync(long conceptEmbeddingId, CancellationToken cancellationToken = default);
    Task<AiConceptEmbedding?> GetActiveByKeyAsync(Guid embeddingVersionId, string conceptKey, string conceptType, CancellationToken cancellationToken = default);
    Task<List<AiConceptEmbedding>> ListByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task AddAsync(AiConceptEmbedding entity, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(AiConceptEmbedding entity, CancellationToken cancellationToken = default);
    Task<int> CountActiveByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task<List<AiConceptEmbeddingCacheEntry>> LoadActiveCacheEntriesAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetActiveTextHashesAsync(
        Guid embeddingVersionId,
        string conceptType,
        IEnumerable<string> conceptKeys,
        CancellationToken cancellationToken = default);
    Task<AiRubricEmbeddingUpsertOutcome> UpsertActiveEmbeddingAsync(
        Guid embeddingVersionId,
        string conceptKey,
        string conceptType,
        string? sourceDomain,
        string sourceText,
        string textHash,
        float[] vector,
        int dimensionCount,
        Guid jobId,
        CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingJobRepository
{
    Task<AiEmbeddingJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<List<AiEmbeddingJob>> ListByStatusAsync(string status, int take = 50, CancellationToken cancellationToken = default);
    Task AddAsync(AiEmbeddingJob entity, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(AiEmbeddingJob entity, CancellationToken cancellationToken = default);
    Task<int> CountRunningAsync(CancellationToken cancellationToken = default);
    Task<int> CountFailedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingQueueRepository
{
    Task<AiEmbeddingQueue?> GetByIdAsync(long queueId, CancellationToken cancellationToken = default);
    Task<List<AiEmbeddingQueue>> DequeueBatchAsync(int batchSize, string workerId, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task<List<AiEmbeddingQueue>> DequeueBatchForJobAsync(Guid jobId, int batchSize, string workerId, TimeSpan lockDuration, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<AiEmbeddingQueue> entities, CancellationToken cancellationToken = default);
    Task<bool> HasOpenRubricItemAsync(int rubricId, CancellationToken cancellationToken = default);
    Task<int> ReleaseStaleLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<int> CountPendingAsync(CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<int> CountByJobAndStatusAsync(Guid jobId, string status, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingAuditRepository
{
    Task AddAsync(AiEmbeddingAudit entity, CancellationToken cancellationToken = default);
    Task<List<AiEmbeddingAudit>> ListByEntityAsync(string entityType, string entityId, int take = 50, CancellationToken cancellationToken = default);
    Task<List<AiEmbeddingAudit>> ListByJobAsync(Guid jobId, int take = 100, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingStatisticsRepository
{
    Task<AiEmbeddingStatistics?> GetByVersionAndDateAsync(Guid embeddingVersionId, DateOnly statDate, CancellationToken cancellationToken = default);
    Task UpsertAsync(AiEmbeddingStatistics entity, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingSyncStateRepository
{
    Task<AiEmbeddingSyncState?> GetAsync(string syncScope, Guid? embeddingVersionId, CancellationToken cancellationToken = default);
    Task UpsertAsync(AiEmbeddingSyncState entity, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingVersionService
{
    Task<(bool Success, string Message, AiEmbeddingVersionModel? Result)> RegisterVersionAsync(
        CreateAiEmbeddingVersionRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> ActivateVersionAsync(
        Guid embeddingVersionId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<AiEmbeddingVersionModel?> GetCurrentVersionAsync(CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingJobService
{
    Task<(bool Success, string Message, AiEmbeddingJobModel? Result)> CreateJobAsync(
        CreateAiEmbeddingJobRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> MarkJobRunningAsync(
        Guid jobId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> MarkJobCompletedAsync(
        Guid jobId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> MarkJobFailedAsync(
        Guid jobId,
        string errorSummary,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingQueueService
{
    Task<(bool Success, string Message, long? QueueId)> EnqueueAsync(
        EnqueueAiEmbeddingQueueRequest request,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int EnqueuedCount)> EnqueueBatchAsync(
        IReadOnlyList<EnqueueAiEmbeddingQueueRequest> requests,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, List<AiEmbeddingQueueItemModel> Items)> DequeueBatchAsync(
        string workerId,
        int? batchSize = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> MarkCompletedAsync(
        long queueId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> MarkSkippedAsync(
        long queueId,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> MarkFailedAsync(
        long queueId,
        string error,
        int? actorUserId = null,
        CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingAuditService
{
    Task AppendAsync(AppendAiEmbeddingAuditRequest request, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingStatisticsService
{
    Task RefreshDailySnapshotAsync(Guid embeddingVersionId, DateOnly? statDate = null, CancellationToken cancellationToken = default);
    Task<AiEmbeddingStatisticsModel?> GetTodayAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
}

public interface IAiEmbeddingInfrastructureService
{
    Task<AiEmbeddingInfrastructureStatusModel> GetStatusAsync(CancellationToken cancellationToken = default);
}

public interface IRepertoryRubricCatalogReader
{
    Task<int> CountRubricsAsync(CancellationToken cancellationToken = default);

    Task<List<RepertoryRubricCatalogItem>> ReadPageAsync(int skip, int take, CancellationToken cancellationToken = default);

    Task<List<RepertoryRubricCatalogItem>> ReadByIdsAsync(IEnumerable<int> rubricIds, CancellationToken cancellationToken = default);
}

public interface IRepertoryEmbeddingChangeDetector
{
    Task<List<RubricEmbeddingChangeCandidate>> DetectChangesAsync(
        DateTime watermarkUtc,
        CancellationToken cancellationToken = default);
}

public interface IAiIncrementalEmbeddingRefreshService
{
    Task<IncrementalEmbeddingRefreshResult> RefreshAsync(
        IncrementalEmbeddingRefreshRequest request,
        CancellationToken cancellationToken = default);

    Task<AiEmbeddingSyncStateModel?> GetSyncStateAsync(
        Guid? embeddingVersionId = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Hangfire-ready entry point. Register recurring job against this interface when UseHangfireForIncrementalRefresh is enabled.
/// </summary>
public interface IAiEmbeddingRefreshJobRunner
{
    Task<IncrementalEmbeddingRefreshResult> RunAsync(
        IncrementalEmbeddingRefreshRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiEnterpriseRubricEmbeddingBuilder
{
    Task<EnterpriseRubricEmbeddingBuildResult> BuildAsync(
        BuildEnterpriseRubricEmbeddingsRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiConceptEmbeddingMemoryCache
{
    Guid? EmbeddingVersionId { get; }

    IReadOnlyList<AiConceptEmbeddingCacheEntry> Entries { get; }

    DateTime? LastRefreshedUtc { get; }

    Task RefreshAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
}

public interface IAiEnterpriseRubricEmbeddingMemoryCache
{
    Guid? EmbeddingVersionId { get; }

    IReadOnlyList<AiEnterpriseRubricEmbeddingCacheEntry> Entries { get; }

    DateTime? LastRefreshedUtc { get; }

    Task RefreshAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default);
}

public interface IAiEnterpriseSemanticSearchService
{
    Task<EnterpriseSemanticSearchResult> SearchAsync(
        EnterpriseSemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiEnterpriseConceptEmbeddingBuilder
{
    Task<EnterpriseConceptEmbeddingBuildResult> BuildAsync(
        BuildEnterpriseConceptEmbeddingsRequest request,
        CancellationToken cancellationToken = default);
}
