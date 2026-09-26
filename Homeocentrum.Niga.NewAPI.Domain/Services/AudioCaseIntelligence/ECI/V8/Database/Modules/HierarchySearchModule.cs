using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 6: hierarchy expansion (parent/child/sibling + cross references) for known rubric seeds.</summary>
public sealed class HierarchySearchModule : IEciHierarchySearchModule
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<HierarchySearchModule> _logger;

    public HierarchySearchModule(NIGACentrumContext context, ILogger<HierarchySearchModule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> ExpandAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<EciCandidateRubric> seedCandidates,
        CancellationToken cancellationToken = default)
    {
        var seedIds = seedCandidates
            .Select(c => c.SubSectionId)
            .Where(id => id > 0)
            .Distinct()
            .Take(30)
            .ToList();

        if (seedIds.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        var map = new Dictionary<int, EciCandidateRubric>();

        var seeds = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => seedIds.Contains(s.SubSectionId) && !s.DeleteStatus)
            .Select(s => new { s.SubSectionId, s.SubSectionName, s.ParentSubSectionId, s.SectionId })
            .ToListAsync(cancellationToken);

        foreach (var seed in seeds)
        {
            if (seed.ParentSubSectionId is > 0)
            {
                var parent = await _context.SubSectionMasters.AsNoTracking()
                    .Where(s => s.SubSectionId == seed.ParentSubSectionId && !s.DeleteStatus)
                    .Select(s => new { s.SubSectionId, s.SubSectionName })
                    .FirstOrDefaultAsync(cancellationToken);

                if (parent?.SubSectionName != null)
                {
                    Upsert(map, parent.SubSectionId, parent.SubSectionName, $"Parent of '{seed.SubSectionName}'", 0.60m);
                }
            }

            var children = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => s.ParentSubSectionId == seed.SubSectionId && !s.DeleteStatus)
                .Take(8)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                if (!string.IsNullOrWhiteSpace(child.SubSectionName))
                {
                    Upsert(map, child.SubSectionId, child.SubSectionName!, $"Child of '{seed.SubSectionName}'", 0.55m);
                }
            }

            if (seed.SectionId.HasValue)
            {
                var siblings = await _context.SubSectionMasters.AsNoTracking()
                    .Where(s => s.SectionId == seed.SectionId
                        && s.SubSectionId != seed.SubSectionId
                        && s.ParentSubSectionId == seed.ParentSubSectionId
                        && !s.DeleteStatus)
                    .Take(6)
                    .Select(s => new { s.SubSectionId, s.SubSectionName })
                    .ToListAsync(cancellationToken);

                foreach (var sibling in siblings)
                {
                    if (!string.IsNullOrWhiteSpace(sibling.SubSectionName))
                    {
                        Upsert(map, sibling.SubSectionId, sibling.SubSectionName!, $"Sibling of '{seed.SubSectionName}'", 0.50m);
                    }
                }
            }
        }

        // Cross references.
        var refs = await _context.ReferenceRubricDetails.AsNoTracking()
            .Where(r => r.DeleteStatus != true
                && (seedIds.Contains(r.SubSectionId ?? 0) || seedIds.Contains(r.RefSubSectionId ?? 0)))
            .Take(50)
            .ToListAsync(cancellationToken);

        var targetIds = refs
            .SelectMany(r => new[] { r.SubSectionId, r.RefSubSectionId })
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .Where(id => !map.ContainsKey(id))
            .Take(40)
            .ToList();

        if (targetIds.Count > 0)
        {
            var rubrics = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => targetIds.Contains(s.SubSectionId) && !s.DeleteStatus)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var r in rubrics)
            {
                if (!string.IsNullOrWhiteSpace(r.SubSectionName))
                {
                    Upsert(map, r.SubSectionId, r.SubSectionName!, "Cross-reference", 0.48m);
                }
            }
        }

        return map.Values.ToList();
    }

    private static void Upsert(
        Dictionary<int, EciCandidateRubric> map,
        int rubricId,
        string name,
        string path,
        decimal confidence)
    {
        if (rubricId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!map.TryGetValue(rubricId, out var candidate))
        {
            candidate = new EciCandidateRubric { SubSectionId = rubricId, SubSectionName = name };
            map[rubricId] = candidate;
        }

        candidate.HierarchyScore = Math.Max(candidate.HierarchyScore, confidence);
        candidate.Provenance.Add(new EciCandidateProvenance
        {
            Source = EciCandidateSources.Hierarchy,
            MatchPath = path,
            Confidence = confidence,
        });
    }
}

