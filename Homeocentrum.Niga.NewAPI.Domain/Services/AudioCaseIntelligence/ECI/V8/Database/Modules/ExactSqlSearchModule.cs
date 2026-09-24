using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 1: exact rubric name + exact synonym (RubricAliases) search.</summary>
public sealed class ExactSqlSearchModule : IEciExactSqlSearchModule
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<ExactSqlSearchModule> _logger;

    public ExactSqlSearchModule(NIGACentrumContext context, ILogger<ExactSqlSearchModule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        CancellationToken cancellationToken = default)
    {
        var term = symptom.Symptom.Symptom?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return new List<EciCandidateRubric>();
        }

        var candidates = new Dictionary<int, EciCandidateRubric>();

        // Exact rubric name match.
        var rubricRows = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null && s.SubSectionName == term)
            .Take(10)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        foreach (var row in rubricRows)
        {
            Upsert(candidates, row.SubSectionId, row.SubSectionName!, EciCandidateSources.ExactSql,
                $"Exact rubric: '{term}'", 0.98m, sqlExact: 1m);
        }

        // Exact alias match.
        var aliasRows = await _context.RubricAliases.AsNoTracking()
            .Where(a => a.IsActive && a.AliasText == term)
            .Take(20)
            .Select(a => new { a.SubSectionId, a.AliasText, a.Weight, a.Source })
            .ToListAsync(cancellationToken);

        if (aliasRows.Count > 0)
        {
            var rubricIds = aliasRows.Select(a => a.SubSectionId).Distinct().ToList();
            var rubrics = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => rubricIds.Contains(s.SubSectionId) && !s.DeleteStatus)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            var rubricMap = rubrics.ToDictionary(r => r.SubSectionId, r => r.SubSectionName ?? string.Empty);
            foreach (var alias in aliasRows)
            {
                if (!rubricMap.TryGetValue(alias.SubSectionId, out var name) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var confidence = Math.Clamp(0.85m + alias.Weight * 0.10m, 0.70m, 0.97m);
                Upsert(candidates, alias.SubSectionId, name, EciCandidateSources.ExactSql,
                    $"Exact synonym: '{alias.AliasText}' (source={alias.Source})", confidence, sqlExact: 0.85m);
            }
        }

        return candidates.Values.ToList();
    }

    private static void Upsert(
        Dictionary<int, EciCandidateRubric> map,
        int rubricId,
        string name,
        string source,
        string path,
        decimal confidence,
        decimal sqlExact)
    {
        if (rubricId <= 0 || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (!map.TryGetValue(rubricId, out var existing))
        {
            existing = new EciCandidateRubric { SubSectionId = rubricId, SubSectionName = name };
            map[rubricId] = existing;
        }

        existing.SqlExactScore = Math.Max(existing.SqlExactScore, sqlExact);
        existing.Provenance.Add(new EciCandidateProvenance
        {
            Source = source,
            MatchPath = path,
            Confidence = confidence,
        });
    }
}

