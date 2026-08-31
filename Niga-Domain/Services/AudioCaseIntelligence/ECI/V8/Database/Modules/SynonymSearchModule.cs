using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 2: curated synonym dictionary search (no GPT).</summary>
public sealed class SynonymSearchModule : IEciSynonymSearchModule
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<SynonymSearchModule> _logger;

    public SynonymSearchModule(NIGACentrumContext context, ILogger<SynonymSearchModule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> curatedSynonyms,
        CancellationToken cancellationToken = default)
    {
        if (curatedSynonyms.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        // Query aliases by normalized alias or raw alias match.
        var synonyms = curatedSynonyms
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(60)
            .ToList();

        var rows = await _context.RubricAliases.AsNoTracking()
            .Where(a => a.IsActive && synonyms.Contains(a.AliasText))
            .Take(200)
            .Select(a => new { a.SubSectionId, a.AliasText, a.Weight, a.Source })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        var rubricIds = rows.Select(r => r.SubSectionId).Distinct().ToList();
        var rubrics = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => rubricIds.Contains(s.SubSectionId) && !s.DeleteStatus)
            .Select(s => new { s.SubSectionId, s.SubSectionName })
            .ToListAsync(cancellationToken);

        var rubricMap = rubrics.ToDictionary(r => r.SubSectionId, r => r.SubSectionName ?? string.Empty);
        var map = new Dictionary<int, EciCandidateRubric>();

        foreach (var row in rows)
        {
            if (!rubricMap.TryGetValue(row.SubSectionId, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!map.TryGetValue(row.SubSectionId, out var candidate))
            {
                candidate = new EciCandidateRubric { SubSectionId = row.SubSectionId, SubSectionName = name };
                map[row.SubSectionId] = candidate;
            }

            var confidence = Math.Clamp(0.72m + row.Weight * 0.10m, 0.60m, 0.92m);
            candidate.Provenance.Add(new EciCandidateProvenance
            {
                Source = EciCandidateSources.Synonym,
                MatchPath = $"Curated synonym: '{row.AliasText}' (source={row.Source})",
                Confidence = confidence,
            });
        }

        return map.Values.ToList();
    }
}

