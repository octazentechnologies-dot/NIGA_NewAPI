using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 9: merges candidates across sources, removes duplicates, keeps provenance.</summary>
public sealed class CandidateMerger : IEciCandidateMerger
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<CandidateMerger> _logger;

    public CandidateMerger(NIGACentrumContext context, ILogger<CandidateMerger> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<EciCandidateRubric> Merge(params IEnumerable<EciCandidateRubric>[] lists)
    {
        var map = new Dictionary<int, EciCandidateRubric>();

        foreach (var list in lists)
        {
            foreach (var c in list)
            {
                if (c.SubSectionId <= 0)
                {
                    continue;
                }

                if (!map.TryGetValue(c.SubSectionId, out var existing))
                {
                    existing = new EciCandidateRubric
                    {
                        SubSectionId = c.SubSectionId,
                        SubSectionName = c.SubSectionName,
                    };
                    map[c.SubSectionId] = existing;
                }

                if (!string.IsNullOrWhiteSpace(c.SubSectionName) && string.IsNullOrWhiteSpace(existing.SubSectionName))
                {
                    existing.SubSectionName = c.SubSectionName;
                }

                existing.SqlExactScore = Math.Max(existing.SqlExactScore, c.SqlExactScore);
                existing.OntologyScore = Math.Max(existing.OntologyScore, c.OntologyScore);
                existing.EmbeddingScore = Math.Max(existing.EmbeddingScore, c.EmbeddingScore);
                existing.HierarchyScore = Math.Max(existing.HierarchyScore, c.HierarchyScore);
                existing.HistoricalScore = Math.Max(existing.HistoricalScore, c.HistoricalScore);
                existing.ExpertRulesScore = Math.Max(existing.ExpertRulesScore, c.ExpertRulesScore);

                foreach (var p in c.Provenance)
                {
                    existing.Provenance.Add(p);
                }
            }
        }

        return map.Values.ToList();
    }

    /// <summary>
    /// Resolves missing rubric names for candidates produced by historical mapping (only rubricId known).
    /// </summary>
    public async Task ResolveNamesAsync(List<EciCandidateRubric> candidates, CancellationToken cancellationToken = default)
    {
        var missing = candidates
            .Where(c => c.SubSectionId > 0 && string.IsNullOrWhiteSpace(c.SubSectionName))
            .Select(c => c.SubSectionId)
            .Distinct()
            .Take(200)
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        var rows = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => missing.Contains(s.SubSectionId) && !s.DeleteStatus)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        var map = rows.ToDictionary(r => r.SubSectionId, r => r.SubSectionName ?? string.Empty);
        foreach (var c in candidates)
        {
            if (string.IsNullOrWhiteSpace(c.SubSectionName) && map.TryGetValue(c.SubSectionId, out var name))
            {
                c.SubSectionName = name;
            }
        }
    }
}

