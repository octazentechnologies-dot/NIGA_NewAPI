using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>
/// Module 4: Full Text Search using SQL Server CONTAINS.
/// If full-text is not configured, this module returns empty (no LIKE fallback here).
/// </summary>
public sealed class FullTextSearchModule : IEciFullTextSearchModule
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<FullTextSearchModule> _logger;

    public FullTextSearchModule(NIGACentrumContext context, ILogger<FullTextSearchModule> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken = default)
    {
        var queryTerms = terms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().Replace("\"", string.Empty))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        if (queryTerms.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        // Build: "term1" OR "term2" ... (phrase search)
        var contains = string.Join(" OR ", queryTerms.Select(t => $"\"{t}\""));

        try
        {
            // Note: We keep this module isolated to FTS only. If FTS isn't configured, SQL throws and we return empty.
            var sql = $@"
SELECT TOP (40) SubSectionId, SubSectionName
FROM SubSectionMasters WITH (NOLOCK)
WHERE DeleteStatus = 0 AND SubSectionName IS NOT NULL AND CONTAINS(SubSectionName, {{0}})
ORDER BY LEN(SubSectionName) ASC";

            var rows = await _context.SubSectionMasters
                .FromSqlRaw(sql, contains)
                .AsNoTracking()
                .Select(s => new { s.SubSectionId, s.SubSectionName })
                .ToListAsync(cancellationToken);

            var map = new Dictionary<int, EciCandidateRubric>();
            foreach (var row in rows)
            {
                if (!map.TryGetValue(row.SubSectionId, out var candidate))
                {
                    candidate = new EciCandidateRubric
                    {
                        SubSectionId = row.SubSectionId,
                        SubSectionName = row.SubSectionName ?? string.Empty,
                    };
                    map[row.SubSectionId] = candidate;
                }

                candidate.Provenance.Add(new EciCandidateProvenance
                {
                    Source = EciCandidateSources.FullText,
                    MatchPath = $"FTS CONTAINS: {contains}",
                    Confidence = 0.65m,
                });
            }

            return map.Values.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ECI v8 full-text search skipped (FTS not available).");
            return new List<EciCandidateRubric>();
        }
    }
}

