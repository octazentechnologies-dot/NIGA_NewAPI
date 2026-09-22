using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories.AiEmbeddingInfrastructure;

public class AiEmbeddingUnitOfWork : IAiEmbeddingUnitOfWork
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingUnitOfWork(NIGACentrumContext context)
    {
        _context = context;
        Versions = new AiEmbeddingVersionRepository(context);
        RubricEmbeddings = new AiRubricEmbeddingRepository(context);
        ConceptEmbeddings = new AiConceptEmbeddingRepository(context);
        Jobs = new AiEmbeddingJobRepository(context);
        Queue = new AiEmbeddingQueueRepository(context);
        Audits = new AiEmbeddingAuditRepository(context);
        Statistics = new AiEmbeddingStatisticsRepository(context);
        SyncState = new AiEmbeddingSyncStateRepository(context);
    }

    public IAiEmbeddingVersionRepository Versions { get; }
    public IAiRubricEmbeddingRepository RubricEmbeddings { get; }
    public IAiConceptEmbeddingRepository ConceptEmbeddings { get; }
    public IAiEmbeddingJobRepository Jobs { get; }
    public IAiEmbeddingQueueRepository Queue { get; }
    public IAiEmbeddingAuditRepository Audits { get; }
    public IAiEmbeddingStatisticsRepository Statistics { get; }
    public IAiEmbeddingSyncStateRepository SyncState { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public ValueTask DisposeAsync() => _context.DisposeAsync();
}

public class AiEmbeddingVersionRepository : IAiEmbeddingVersionRepository
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingVersionRepository(NIGACentrumContext context) => _context = context;

    public Task<AiEmbeddingVersion?> GetByIdAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingVersions
            .FirstOrDefaultAsync(x => x.EmbeddingVersionId == embeddingVersionId && !x.IsDeleted, cancellationToken);

    public Task<AiEmbeddingVersion?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingVersions
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsCurrent && x.IsActive)
            .OrderByDescending(x => x.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<AiEmbeddingVersion>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingVersions
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderByDescending(x => x.IsCurrent)
            .ThenByDescending(x => x.CreatedDate)
            .ToListAsync(cancellationToken);

    public Task<bool> VersionCodeExistsAsync(string versionCode, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingVersions.AnyAsync(
            x => !x.IsDeleted && x.VersionCode == versionCode,
            cancellationToken);

    public async Task AddAsync(AiEmbeddingVersion entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await _context.AiEmbeddingVersions.AddAsync(entity, cancellationToken);
    }

    public async Task SetCurrentAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default)
    {
        var allCurrent = await _context.AiEmbeddingVersions
            .Where(x => !x.IsDeleted && x.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var version in allCurrent)
        {
            version.IsCurrent = false;
            version.UpdatedDate = DateTime.UtcNow;
        }

        var target = await _context.AiEmbeddingVersions
            .FirstOrDefaultAsync(x => x.EmbeddingVersionId == embeddingVersionId && !x.IsDeleted, cancellationToken);
        if (target == null) return;

        target.IsCurrent = true;
        target.IsActive = true;
        target.Status = AiEmbeddingStatuses.Active;
        target.UpdatedDate = DateTime.UtcNow;
    }

    public Task SoftDeleteAsync(AiEmbeddingVersion entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.IsCurrent = false;
        entity.DeletedDate = DateTime.UtcNow;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.Status = AiEmbeddingStatuses.Archived;
        return Task.CompletedTask;
    }
}

public class AiRubricEmbeddingRepository : IAiRubricEmbeddingRepository
{
    private readonly NIGACentrumContext _context;

    public AiRubricEmbeddingRepository(NIGACentrumContext context) => _context = context;

    public Task<AiRubricEmbedding?> GetByIdAsync(long rubricEmbeddingId, CancellationToken cancellationToken = default) =>
        _context.AiRubricEmbeddings
            .FirstOrDefaultAsync(x => x.RubricEmbeddingId == rubricEmbeddingId && !x.IsDeleted, cancellationToken);

    public Task<AiRubricEmbedding?> GetActiveByRubricAsync(
        Guid embeddingVersionId, int rubricId, CancellationToken cancellationToken = default) =>
        _context.AiRubricEmbeddings
            .Where(x => !x.IsDeleted
                && x.EmbeddingVersionId == embeddingVersionId
                && x.RubricId == rubricId
                && x.Status == AiEmbeddingStatuses.Active)
            .OrderByDescending(x => x.RevisionNo)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Dictionary<int, string>> GetActiveTextHashesAsync(
        Guid embeddingVersionId,
        IEnumerable<int> rubricIds,
        CancellationToken cancellationToken = default)
    {
        var ids = rubricIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();

        return await _context.AiRubricEmbeddings
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.EmbeddingVersionId == embeddingVersionId
                && x.Status == AiEmbeddingStatuses.Active
                && ids.Contains(x.RubricId))
            .ToDictionaryAsync(x => x.RubricId, x => x.TextHash, cancellationToken);
    }

    public Task<List<AiRubricEmbedding>> ListByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default) =>
        _context.AiRubricEmbeddings
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.EmbeddingVersionId == embeddingVersionId)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AiRubricEmbedding entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await _context.AiRubricEmbeddings.AddAsync(entity, cancellationToken);
    }

    public async Task<AiRubricEmbeddingUpsertOutcome> UpsertActiveEmbeddingAsync(
        Guid embeddingVersionId,
        int rubricId,
        string sourceText,
        string textHash,
        float[] vector,
        int dimensionCount,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetActiveByRubricAsync(embeddingVersionId, rubricId, cancellationToken);
        if (existing != null && string.Equals(existing.TextHash, textHash, StringComparison.OrdinalIgnoreCase))
            return AiRubricEmbeddingUpsertOutcome.Skipped;

        var payloadJson = System.Text.Json.JsonSerializer.Serialize(vector, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        });

        var isUpdate = existing != null;
        if (existing != null)
        {
            existing.Status = AiEmbeddingStatuses.Superseded;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        var revisionNo = (existing?.RevisionNo ?? 0) + 1;
        await AddAsync(new AiRubricEmbedding
        {
            EmbeddingVersionId = embeddingVersionId,
            RubricId = rubricId,
            SourceText = sourceText,
            TextHash = textHash,
            EmbeddingPayloadJson = payloadJson,
            DimensionCount = dimensionCount,
            RevisionNo = revisionNo,
            Status = AiEmbeddingStatuses.Active,
            SourceType = "SemanticDocument",
            LastJobId = jobId,
        }, cancellationToken);

        return isUpdate ? AiRubricEmbeddingUpsertOutcome.Updated : AiRubricEmbeddingUpsertOutcome.Created;
    }

    public async Task<bool> ArchiveActiveByRubricAsync(
        Guid embeddingVersionId,
        int rubricId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetActiveByRubricAsync(embeddingVersionId, rubricId, cancellationToken);
        if (existing == null)
            return false;

        existing.Status = AiEmbeddingStatuses.Archived;
        existing.IsDeleted = true;
        existing.DeletedDate = DateTime.UtcNow;
        existing.UpdatedDate = DateTime.UtcNow;
        existing.LastJobId = jobId;
        return true;
    }

    public Task SoftDeleteAsync(AiRubricEmbedding entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.Status = AiEmbeddingStatuses.Archived;
        return Task.CompletedTask;
    }

    public Task<int> CountActiveByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default) =>
        _context.AiRubricEmbeddings.CountAsync(
            x => !x.IsDeleted && x.EmbeddingVersionId == embeddingVersionId && x.Status == AiEmbeddingStatuses.Active,
            cancellationToken);

    public async Task<List<AiEnterpriseRubricEmbeddingCacheEntry>> LoadActiveCacheEntriesAsync(
        Guid embeddingVersionId,
        CancellationToken cancellationToken = default)
    {
        var previousTimeout = _context.Database.GetCommandTimeout();
        _context.Database.SetCommandTimeout(120);
        try
        {
            const int pageSize = 2000;
            var lastId = 0;
            var result = new List<AiEnterpriseRubricEmbeddingCacheEntry>();
            while (true)
            {
                var rows = await _context.AiRubricEmbeddings
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted
                        && x.EmbeddingVersionId == embeddingVersionId
                        && x.Status == AiEmbeddingStatuses.Active
                        && x.EmbeddingPayloadJson != null
                        && x.EmbeddingPayloadJson != ""
                        && x.RubricId > lastId)
                    .OrderBy(x => x.RubricId)
                    .Select(x => new { x.RubricId, x.SourceText, x.EmbeddingPayloadJson })
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
                if (rows.Count == 0)
                    break;
                lastId = rows[rows.Count - 1].RubricId;
                foreach (var row in rows)
                {
                    var vector = AiEmbeddingVectorSerializer.Deserialize(row.EmbeddingPayloadJson!);
                    if (vector.Length == 0)
                        continue;
                    result.Add(new AiEnterpriseRubricEmbeddingCacheEntry
                    {
                        RubricId = row.RubricId,
                        SourceText = row.SourceText,
                        Vector = vector,
                    });
                }
            }
            return result;
        }
        finally
        {
            _context.Database.SetCommandTimeout(previousTimeout);
        }
    }
}

public class AiConceptEmbeddingRepository : IAiConceptEmbeddingRepository
{
    private readonly NIGACentrumContext _context;

    public AiConceptEmbeddingRepository(NIGACentrumContext context) => _context = context;

    public Task<AiConceptEmbedding?> GetByIdAsync(long conceptEmbeddingId, CancellationToken cancellationToken = default) =>
        _context.AiConceptEmbeddings
            .FirstOrDefaultAsync(x => x.ConceptEmbeddingId == conceptEmbeddingId && !x.IsDeleted, cancellationToken);

    public Task<AiConceptEmbedding?> GetActiveByKeyAsync(
        Guid embeddingVersionId, string conceptKey, string conceptType, CancellationToken cancellationToken = default) =>
        _context.AiConceptEmbeddings
            .Where(x => !x.IsDeleted
                && x.EmbeddingVersionId == embeddingVersionId
                && x.ConceptKey == conceptKey
                && x.ConceptType == conceptType
                && x.Status == AiEmbeddingStatuses.Active)
            .OrderByDescending(x => x.RevisionNo)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<AiConceptEmbedding>> ListByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default) =>
        _context.AiConceptEmbeddings
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.EmbeddingVersionId == embeddingVersionId)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AiConceptEmbedding entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await _context.AiConceptEmbeddings.AddAsync(entity, cancellationToken);
    }

    public Task SoftDeleteAsync(AiConceptEmbedding entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.Status = AiEmbeddingStatuses.Archived;
        return Task.CompletedTask;
    }

    public Task<int> CountActiveByVersionAsync(Guid embeddingVersionId, CancellationToken cancellationToken = default) =>
        _context.AiConceptEmbeddings.CountAsync(
            x => !x.IsDeleted && x.EmbeddingVersionId == embeddingVersionId && x.Status == AiEmbeddingStatuses.Active,
            cancellationToken);

    public async Task<List<AiConceptEmbeddingCacheEntry>> LoadActiveCacheEntriesAsync(
        Guid embeddingVersionId,
        CancellationToken cancellationToken = default)
    {
        var previousTimeout = _context.Database.GetCommandTimeout();
        _context.Database.SetCommandTimeout(120);
        try
        {
            const int pageSize = 2000;
            var lastId = 0L;
            var result = new List<AiConceptEmbeddingCacheEntry>();
            while (true)
            {
                var rows = await _context.AiConceptEmbeddings
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted
                        && x.EmbeddingVersionId == embeddingVersionId
                        && x.Status == AiEmbeddingStatuses.Active
                        && x.EmbeddingPayloadJson != null
                        && x.EmbeddingPayloadJson != ""
                        && x.ConceptEmbeddingId > lastId)
                    .OrderBy(x => x.ConceptEmbeddingId)
                    .Select(x => new
                    {
                        x.ConceptEmbeddingId,
                        x.ConceptKey,
                        x.ConceptType,
                        x.SourceText,
                        x.EmbeddingPayloadJson,
                    })
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);
                if (rows.Count == 0)
                    break;
                lastId = rows[rows.Count - 1].ConceptEmbeddingId;
                foreach (var row in rows)
                {
                    var vector = AiEmbeddingVectorSerializer.Deserialize(row.EmbeddingPayloadJson!);
                    if (vector.Length == 0)
                        continue;
                    result.Add(new AiConceptEmbeddingCacheEntry
                    {
                        ConceptEmbeddingId = row.ConceptEmbeddingId,
                        ConceptKey = row.ConceptKey,
                        ConceptType = row.ConceptType,
                        SourceText = row.SourceText,
                        Vector = vector,
                    });
                }
            }
            return result;
        }
        finally
        {
            _context.Database.SetCommandTimeout(previousTimeout);
        }
    }

    public async Task<Dictionary<string, string>> GetActiveTextHashesAsync(
        Guid embeddingVersionId,
        string conceptType,
        IEnumerable<string> conceptKeys,
        CancellationToken cancellationToken = default)
    {
        var keys = conceptKeys.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (keys.Count == 0)
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        return await _context.AiConceptEmbeddings
            .AsNoTracking()
            .Where(x => !x.IsDeleted
                && x.EmbeddingVersionId == embeddingVersionId
                && x.ConceptType == conceptType
                && x.Status == AiEmbeddingStatuses.Active
                && keys.Contains(x.ConceptKey))
            .ToDictionaryAsync(x => x.ConceptKey, x => x.TextHash, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    public async Task<AiRubricEmbeddingUpsertOutcome> UpsertActiveEmbeddingAsync(
        Guid embeddingVersionId,
        string conceptKey,
        string conceptType,
        string? sourceDomain,
        string sourceText,
        string textHash,
        float[] vector,
        int dimensionCount,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var existing = await GetActiveByKeyAsync(embeddingVersionId, conceptKey, conceptType, cancellationToken);
        if (existing != null && string.Equals(existing.TextHash, textHash, StringComparison.OrdinalIgnoreCase))
            return AiRubricEmbeddingUpsertOutcome.Skipped;

        var payloadJson = AiEmbeddingVectorSerializer.Serialize(vector);
        var isUpdate = existing != null;
        if (existing != null)
        {
            existing.Status = AiEmbeddingStatuses.Superseded;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        await AddAsync(new AiConceptEmbedding
        {
            EmbeddingVersionId = embeddingVersionId,
            ConceptKey = conceptKey,
            ConceptType = conceptType,
            SourceDomain = sourceDomain,
            SourceText = sourceText,
            TextHash = textHash,
            EmbeddingPayloadJson = payloadJson,
            DimensionCount = dimensionCount,
            RevisionNo = (existing?.RevisionNo ?? 0) + 1,
            Status = AiEmbeddingStatuses.Active,
            LastJobId = jobId,
        }, cancellationToken);

        return isUpdate ? AiRubricEmbeddingUpsertOutcome.Updated : AiRubricEmbeddingUpsertOutcome.Created;
    }
}

public class AiEmbeddingJobRepository : IAiEmbeddingJobRepository
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingJobRepository(NIGACentrumContext context) => _context = context;

    public Task<AiEmbeddingJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingJobs.FirstOrDefaultAsync(x => x.JobId == jobId && !x.IsDeleted, cancellationToken);

    public Task<List<AiEmbeddingJob>> ListByStatusAsync(string status, int take = 50, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingJobs
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Status == status)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedDate)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(AiEmbeddingJob entity, CancellationToken cancellationToken = default)
    {
        if (entity.JobId == Guid.Empty) entity.JobId = Guid.NewGuid();
        entity.CreatedDate = DateTime.UtcNow;
        await _context.AiEmbeddingJobs.AddAsync(entity, cancellationToken);
    }

    public Task SoftDeleteAsync(AiEmbeddingJob entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.DeletedDate = DateTime.UtcNow;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.Status = AiEmbeddingStatuses.Cancelled;
        return Task.CompletedTask;
    }

    public Task<int> CountRunningAsync(CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingJobs.CountAsync(
            x => !x.IsDeleted && x.Status == AiEmbeddingStatuses.Running,
            cancellationToken);

    public Task<int> CountFailedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingJobs.CountAsync(
            x => !x.IsDeleted && x.Status == AiEmbeddingStatuses.Failed && x.UpdatedDate >= sinceUtc,
            cancellationToken);
}

public class AiEmbeddingQueueRepository : IAiEmbeddingQueueRepository
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingQueueRepository(NIGACentrumContext context) => _context = context;

    public Task<AiEmbeddingQueue?> GetByIdAsync(long queueId, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingQueues.FirstOrDefaultAsync(x => x.QueueId == queueId && !x.IsDeleted, cancellationToken);

    public async Task<List<AiEmbeddingQueue>> DequeueBatchAsync(
        int batchSize, string workerId, TimeSpan lockDuration, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await _context.AiEmbeddingQueues
            .Where(x => !x.IsDeleted
                && x.Status == AiEmbeddingStatuses.Pending
                && (x.NextRetryAtUtc == null || x.NextRetryAtUtc <= now)
                && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedDate)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.Status = AiEmbeddingStatuses.Processing;
            item.LockedBy = workerId;
            item.LockedUntilUtc = now.Add(lockDuration);
            item.UpdatedDate = now;
        }

        return items;
    }

    public async Task<List<AiEmbeddingQueue>> DequeueBatchForJobAsync(
        Guid jobId,
        int batchSize,
        string workerId,
        TimeSpan lockDuration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var items = await _context.AiEmbeddingQueues
            .Where(x => !x.IsDeleted
                && x.JobId == jobId
                && x.Status == AiEmbeddingStatuses.Pending
                && (x.NextRetryAtUtc == null || x.NextRetryAtUtc <= now)
                && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now))
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.CreatedDate)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            item.Status = AiEmbeddingStatuses.Processing;
            item.LockedBy = workerId;
            item.LockedUntilUtc = now.Add(lockDuration);
            item.UpdatedDate = now;
        }

        return items;
    }

    public async Task AddRangeAsync(IEnumerable<AiEmbeddingQueue> entities, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entity in entities)
        {
            entity.CreatedDate = now;
            await _context.AiEmbeddingQueues.AddAsync(entity, cancellationToken);
        }
    }

    public Task<int> CountPendingAsync(CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingQueues.CountAsync(
            x => !x.IsDeleted && x.Status == AiEmbeddingStatuses.Pending,
            cancellationToken);

    public Task<int> CountByStatusAsync(string status, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingQueues.CountAsync(
            x => !x.IsDeleted && x.Status == status,
            cancellationToken);

    public Task<int> CountByJobAndStatusAsync(Guid jobId, string status, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingQueues.CountAsync(
            x => !x.IsDeleted && x.JobId == jobId && x.Status == status,
            cancellationToken);

    public Task<bool> HasOpenRubricItemAsync(int rubricId, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingQueues.AnyAsync(
            x => !x.IsDeleted
                && x.RubricId == rubricId
                && (x.Status == AiEmbeddingStatuses.Pending || x.Status == AiEmbeddingStatuses.Processing),
            cancellationToken);

    public async Task<int> ReleaseStaleLocksAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var stale = await _context.AiEmbeddingQueues
            .Where(x => !x.IsDeleted
                && x.Status == AiEmbeddingStatuses.Processing
                && x.LockedUntilUtc != null
                && x.LockedUntilUtc < utcNow)
            .ToListAsync(cancellationToken);

        foreach (var item in stale)
        {
            item.Status = AiEmbeddingStatuses.Pending;
            item.LockedBy = null;
            item.LockedUntilUtc = null;
            item.UpdatedDate = utcNow;
        }

        return stale.Count;
    }
}

public class AiEmbeddingAuditRepository : IAiEmbeddingAuditRepository
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingAuditRepository(NIGACentrumContext context) => _context = context;

    public async Task AddAsync(AiEmbeddingAudit entity, CancellationToken cancellationToken = default)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await _context.AiEmbeddingAudits.AddAsync(entity, cancellationToken);
    }

    public Task<List<AiEmbeddingAudit>> ListByEntityAsync(
        string entityType, string entityId, int take = 50, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingAudits
            .AsNoTracking()
            .Where(x => x.EntityType == entityType && x.EntityId == entityId)
            .OrderByDescending(x => x.CreatedDate)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<List<AiEmbeddingAudit>> ListByJobAsync(Guid jobId, int take = 100, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingAudits
            .AsNoTracking()
            .Where(x => x.JobId == jobId)
            .OrderByDescending(x => x.CreatedDate)
            .Take(take)
            .ToListAsync(cancellationToken);
}

public class AiEmbeddingStatisticsRepository : IAiEmbeddingStatisticsRepository
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingStatisticsRepository(NIGACentrumContext context) => _context = context;

    public Task<AiEmbeddingStatistics?> GetByVersionAndDateAsync(
        Guid embeddingVersionId, DateOnly statDate, CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingStatistics
            .FirstOrDefaultAsync(x => x.EmbeddingVersionId == embeddingVersionId && x.StatDate == statDate, cancellationToken);

    public async Task UpsertAsync(AiEmbeddingStatistics entity, CancellationToken cancellationToken = default)
    {
        var existing = await GetByVersionAndDateAsync(entity.EmbeddingVersionId, entity.StatDate, cancellationToken);
        if (existing == null)
        {
            entity.CreatedDate = DateTime.UtcNow;
            await _context.AiEmbeddingStatistics.AddAsync(entity, cancellationToken);
            return;
        }

        existing.RubricEmbeddingCount = entity.RubricEmbeddingCount;
        existing.ConceptEmbeddingCount = entity.ConceptEmbeddingCount;
        existing.ActiveRubricEmbeddings = entity.ActiveRubricEmbeddings;
        existing.ActiveConceptEmbeddings = entity.ActiveConceptEmbeddings;
        existing.QueuePendingCount = entity.QueuePendingCount;
        existing.QueueProcessingCount = entity.QueueProcessingCount;
        existing.QueueFailedCount = entity.QueueFailedCount;
        existing.QueueDeadLetterCount = entity.QueueDeadLetterCount;
        existing.JobsCompleted = entity.JobsCompleted;
        existing.JobsFailed = entity.JobsFailed;
        existing.AvgProcessingMs = entity.AvgProcessingMs;
        existing.UpdatedDate = DateTime.UtcNow;
    }
}

public class AiEmbeddingSyncStateRepository : IAiEmbeddingSyncStateRepository
{
    private readonly NIGACentrumContext _context;

    public AiEmbeddingSyncStateRepository(NIGACentrumContext context) => _context = context;

    public Task<AiEmbeddingSyncState?> GetAsync(
        string syncScope,
        Guid? embeddingVersionId,
        CancellationToken cancellationToken = default) =>
        _context.AiEmbeddingSyncStates
            .FirstOrDefaultAsync(
                x => x.SyncScope == syncScope && x.EmbeddingVersionId == embeddingVersionId,
                cancellationToken);

    public async Task UpsertAsync(AiEmbeddingSyncState entity, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync(entity.SyncScope, entity.EmbeddingVersionId, cancellationToken);
        if (existing == null)
        {
            entity.CreatedDate = DateTime.UtcNow;
            await _context.AiEmbeddingSyncStates.AddAsync(entity, cancellationToken);
            return;
        }

        existing.LastSuccessfulSyncUtc = entity.LastSuccessfulSyncUtc;
        existing.LastScanStartedUtc = entity.LastScanStartedUtc;
        existing.LastScanCompletedUtc = entity.LastScanCompletedUtc;
        existing.LastDetectedCount = entity.LastDetectedCount;
        existing.LastEnqueuedCount = entity.LastEnqueuedCount;
        existing.LastProcessedCount = entity.LastProcessedCount;
        existing.LastSkippedCount = entity.LastSkippedCount;
        existing.LastFailedCount = entity.LastFailedCount;
        existing.LastRunCorrelationId = entity.LastRunCorrelationId;
        existing.LastJobId = entity.LastJobId;
        existing.LastError = entity.LastError;
        existing.UpdatedDate = DateTime.UtcNow;
    }
}

public static class AiEmbeddingHashHelper
{
    public static string ComputeSha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text.Trim()));
        return Convert.ToHexString(bytes);
    }
}

public static class AiEmbeddingVectorSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static float[] Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<float>();

        return JsonSerializer.Deserialize<float[]>(json, JsonOptions) ?? Array.Empty<float>();
    }

    public static string Serialize(float[] vector) =>
        JsonSerializer.Serialize(vector, JsonOptions);
}
