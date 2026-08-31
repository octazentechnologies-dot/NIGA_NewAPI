using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Repositories.AiEmbeddingInfrastructure;

public class RepertoryEmbeddingChangeDetector : IRepertoryEmbeddingChangeDetector
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<RepertoryEmbeddingChangeDetector> _logger;

    public RepertoryEmbeddingChangeDetector(
        NIGACentrumContext context,
        ILogger<RepertoryEmbeddingChangeDetector> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<RubricEmbeddingChangeCandidate>> DetectChangesAsync(
        DateTime watermarkUtc,
        CancellationToken cancellationToken = default)
    {
        var changes = new Dictionary<int, RubricEmbeddingChangeCandidate>();
        var detectedAt = DateTime.UtcNow;

        await DetectRubricMasterChangesAsync(watermarkUtc, detectedAt, changes, cancellationToken);
        await DetectSectionChangesAsync(watermarkUtc, detectedAt, changes, cancellationToken);
        await DetectSynonymChangesAsync(watermarkUtc, detectedAt, changes, cancellationToken);
        await DetectDoctorApprovedConceptChangesAsync(watermarkUtc, detectedAt, changes, cancellationToken);
        await DetectBootstrapMappingChangesAsync(watermarkUtc, detectedAt, changes, cancellationToken);

        _logger.LogInformation(
            "Incremental embedding change detection found {Count} rubric candidates since {WatermarkUtc}",
            changes.Count,
            watermarkUtc);

        return changes.Values
            .OrderBy(x => x.RubricId)
            .ToList();
    }

    private async Task DetectRubricMasterChangesAsync(
        DateTime watermarkUtc,
        DateTime detectedAt,
        Dictionary<int, RubricEmbeddingChangeCandidate> changes,
        CancellationToken cancellationToken)
    {
        var rubrics = await _context.SubSectionMasters.AsNoTracking()
            .Where(x => x.SubSectionName != null && x.SubSectionName != "")
            .Select(x => new
            {
                x.SubSectionId,
                x.DeleteStatus,
                x.EnteredDate,
                x.ChangedDate,
            })
            .ToListAsync(cancellationToken);

        foreach (var rubric in rubrics)
        {
            var entered = NormalizeDate(rubric.EnteredDate);
            var changed = NormalizeDate(rubric.ChangedDate);
            var effectiveUpdated = changed > entered ? changed : entered;

            if (rubric.DeleteStatus)
            {
                if (effectiveUpdated >= watermarkUtc || entered >= watermarkUtc)
                {
                    AddChange(changes, rubric.SubSectionId, AiEmbeddingQueueChangeTypes.Deleted, detectedAt, candidate =>
                    {
                        candidate.RequiresArchive = true;
                        candidate.RequiresEmbedding = false;
                        candidate.Reasons.Add(AiEmbeddingChangeReasons.DeletedDate);
                        if (changed >= watermarkUtc)
                            candidate.Reasons.Add(AiEmbeddingChangeReasons.UpdatedDate);
                    });
                }

                continue;
            }

            if (entered >= watermarkUtc)
            {
                AddChange(changes, rubric.SubSectionId, AiEmbeddingQueueChangeTypes.New, detectedAt, candidate =>
                {
                    candidate.Reasons.Add(AiEmbeddingChangeReasons.CreatedDate);
                });
                continue;
            }

            if (changed >= watermarkUtc)
            {
                var changeType = AiEmbeddingQueueChangeTypes.Updated;
                AddChange(changes, rubric.SubSectionId, changeType, detectedAt, candidate =>
                {
                    candidate.Reasons.Add(AiEmbeddingChangeReasons.UpdatedDate);
                    candidate.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
                });
            }
        }

        var restoredCandidates = await (
                from sub in _context.SubSectionMasters.AsNoTracking()
                where !sub.DeleteStatus
                    && sub.ChangedDate != null
                    && sub.ChangedDate >= watermarkUtc
                    && (sub.EnteredDate == null || sub.EnteredDate < watermarkUtc)
                join emb in _context.AiRubricEmbeddings.AsNoTracking()
                    on sub.SubSectionId equals emb.RubricId into embJoin
                from emb in embJoin.DefaultIfEmpty()
                where emb == null || emb.IsDeleted || emb.Status == AiEmbeddingStatuses.Archived
                select sub.SubSectionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var rubricId in restoredCandidates)
        {
            AddChange(changes, rubricId, AiEmbeddingQueueChangeTypes.Restored, detectedAt, candidate =>
            {
                candidate.Reasons.Add(AiEmbeddingChangeReasons.RestoredDate);
                candidate.Reasons.Add(AiEmbeddingChangeReasons.UpdatedDate);
                candidate.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
            });
        }
    }

    private async Task DetectSectionChangesAsync(
        DateTime watermarkUtc,
        DateTime detectedAt,
        Dictionary<int, RubricEmbeddingChangeCandidate> changes,
        CancellationToken cancellationToken)
    {
        var sectionIds = await _context.SectionMasters.AsNoTracking()
            .Where(x => !x.DeleteStatus && x.ChangedDate != null && x.ChangedDate >= watermarkUtc)
            .Select(x => x.SectionId)
            .ToListAsync(cancellationToken);

        if (sectionIds.Count == 0)
            return;

        var rubricIds = await _context.SubSectionMasters.AsNoTracking()
            .Where(x => !x.DeleteStatus && x.SectionId != null && sectionIds.Contains(x.SectionId.Value))
            .Select(x => x.SubSectionId)
            .ToListAsync(cancellationToken);

        foreach (var rubricId in rubricIds)
        {
            AddChange(changes, rubricId, AiEmbeddingQueueChangeTypes.EnrichmentChanged, detectedAt, candidate =>
            {
                candidate.Reasons.Add(AiEmbeddingChangeReasons.SectionUpdated);
                candidate.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
            });
        }
    }

    private async Task DetectSynonymChangesAsync(
        DateTime watermarkUtc,
        DateTime detectedAt,
        Dictionary<int, RubricEmbeddingChangeCandidate> changes,
        CancellationToken cancellationToken)
    {
        var aliasRubricIds = await _context.RubricAliases.AsNoTracking()
            .Where(x => x.IsActive
                && ((x.EnteredDate >= watermarkUtc)
                    || (x.ChangedDate != null && x.ChangedDate >= watermarkUtc)))
            .Select(x => x.SubSectionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var rubricId in aliasRubricIds)
        {
            AddChange(changes, rubricId, AiEmbeddingQueueChangeTypes.EnrichmentChanged, detectedAt, candidate =>
            {
                candidate.Reasons.Add(AiEmbeddingChangeReasons.NewSynonym);
                candidate.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
            });
        }
    }

    private async Task DetectDoctorApprovedConceptChangesAsync(
        DateTime watermarkUtc,
        DateTime detectedAt,
        Dictionary<int, RubricEmbeddingChangeCandidate> changes,
        CancellationToken cancellationToken)
    {
        var rubricIds = await _context.RubricMetaphorDictionaries.AsNoTracking()
            .Where(x => x.IsActive
                && x.ApprovalStatus == "Approved"
                && x.SubSectionId != null
                && ((x.ApprovedDate != null && x.ApprovedDate >= watermarkUtc)
                    || x.EnteredDate >= watermarkUtc))
            .Select(x => x.SubSectionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var rubricId in rubricIds)
        {
            AddChange(changes, rubricId, AiEmbeddingQueueChangeTypes.EnrichmentChanged, detectedAt, candidate =>
            {
                candidate.Reasons.Add(AiEmbeddingChangeReasons.DoctorApprovedConcept);
                candidate.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
            });
        }
    }

    private async Task DetectBootstrapMappingChangesAsync(
        DateTime watermarkUtc,
        DateTime detectedAt,
        Dictionary<int, RubricEmbeddingChangeCandidate> changes,
        CancellationToken cancellationToken)
    {
        var mappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive && x.EnteredDate >= watermarkUtc)
            .Select(x => x.SubSectionNamePattern)
            .ToListAsync(cancellationToken);

        foreach (var pattern in mappings)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                continue;

            var likePattern = pattern.Replace('*', '%');
            if (!likePattern.Contains('%'))
                likePattern = $"%{likePattern}%";

            var rubricIds = await _context.SubSectionMasters.AsNoTracking()
                .Where(x => !x.DeleteStatus
                    && x.SubSectionName != null
                    && EF.Functions.Like(x.SubSectionName, likePattern))
                .Select(x => x.SubSectionId)
                .Take(500)
                .ToListAsync(cancellationToken);

            foreach (var rubricId in rubricIds)
            {
                AddChange(changes, rubricId, AiEmbeddingQueueChangeTypes.EnrichmentChanged, detectedAt, candidate =>
                {
                    candidate.Reasons.Add(AiEmbeddingChangeReasons.BootstrapMappingChanged);
                    candidate.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
                });
            }
        }
    }

    public static void AddChange(
        Dictionary<int, RubricEmbeddingChangeCandidate> changes,
        int rubricId,
        string changeType,
        DateTime detectedAt,
        Action<RubricEmbeddingChangeCandidate>? configure = null)
    {
        if (changes.TryGetValue(rubricId, out var existing))
        {
            MergeCandidate(existing, changeType, configure);
            return;
        }

        var candidate = new RubricEmbeddingChangeCandidate
        {
            RubricId = rubricId,
            ChangeType = changeType,
            DetectedAtUtc = detectedAt,
        };
        configure?.Invoke(candidate);
        changes[rubricId] = candidate;
    }

    public static void MergeCandidate(
        RubricEmbeddingChangeCandidate existing,
        string changeType,
        Action<RubricEmbeddingChangeCandidate>? configure)
    {
        if (changeType == AiEmbeddingQueueChangeTypes.Deleted)
        {
            existing.ChangeType = AiEmbeddingQueueChangeTypes.Deleted;
            existing.RequiresArchive = true;
            existing.RequiresEmbedding = false;
        }
        else if (existing.ChangeType != AiEmbeddingQueueChangeTypes.Deleted)
        {
            existing.ChangeType = changeType;
            existing.RequiresEmbedding = true;
            existing.RequiresArchive = false;
        }

        configure?.Invoke(existing);
        existing.Reasons = existing.Reasons
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static DateTime NormalizeDate(DateTime? value) =>
        value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : DateTime.MinValue;
}
