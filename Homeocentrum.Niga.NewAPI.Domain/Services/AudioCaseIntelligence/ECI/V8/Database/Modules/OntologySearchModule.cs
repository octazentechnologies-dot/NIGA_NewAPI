using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 3: ontology term search (deterministic, derived from accepted symptom).</summary>
public sealed class OntologySearchModule : IEciOntologySearchModule
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<OntologySearchModule> _logger;

    public OntologySearchModule(NIGACentrumContext context, ILogger<OntologySearchModule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> ontologyTerms,
        CancellationToken cancellationToken = default)
    {
        var terms = ontologyTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(80)
            .ToList();

        if (terms.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        // Use alias table first (preferred), then rubric name LIKE as weak fallback (still deterministic).
        var aliasRows = await _context.RubricAliases.AsNoTracking()
            .Where(a => a.IsActive && terms.Contains(a.AliasText))
            .Take(200)
            .Select(a => new { a.SubSectionId, a.AliasText, a.Weight, a.Source })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<int, EciCandidateRubric>();

        if (aliasRows.Count > 0)
        {
            var rubricIds = aliasRows.Select(r => r.SubSectionId).Distinct().ToList();
            var rubrics = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => rubricIds.Contains(s.SubSectionId) && !s.DeleteStatus)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);
            var rubricMap = rubrics.ToDictionary(r => r.SubSectionId, r => r.SubSectionName ?? string.Empty);

            foreach (var row in aliasRows)
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

                candidate.OntologyScore = Math.Max(candidate.OntologyScore, 0.70m);
                candidate.Provenance.Add(new EciCandidateProvenance
                {
                    Source = EciCandidateSources.Ontology,
                    MatchPath = $"Ontology term alias: '{row.AliasText}' (source={row.Source})",
                    Confidence = Math.Clamp(0.68m + row.Weight * 0.10m, 0.55m, 0.90m),
                });
            }
        }

        // Weak fallback: rubric name contains ontology token (avoid scanning full table).
        foreach (var term in terms.Take(12))
        {
            var token = term.Length > 32 ? term[..32] : term;
            var rows = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null
                    && EF.Functions.Like(s.SubSectionName, $"%{token}%"))
                .OrderBy(s => s.SubSectionName!.Length)
                .Take(8)
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
            {
                if (!map.TryGetValue(row.SubSectionId, out var candidate))
                {
                    candidate = new EciCandidateRubric { SubSectionId = row.SubSectionId, SubSectionName = row.SubSectionName ?? string.Empty };
                    map[row.SubSectionId] = candidate;
                }

                candidate.OntologyScore = Math.Max(candidate.OntologyScore, 0.45m);
                candidate.Provenance.Add(new EciCandidateProvenance
                {
                    Source = EciCandidateSources.Ontology,
                    MatchPath = $"Ontology token LIKE: '{token}'",
                    Confidence = 0.55m,
                });
            }
        }

        return map.Values.ToList();
    }
}

