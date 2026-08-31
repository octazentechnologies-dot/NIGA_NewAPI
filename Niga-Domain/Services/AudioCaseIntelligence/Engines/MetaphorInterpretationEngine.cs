using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Repositories;
using Niga_Domain.Services.AudioCaseIntelligence;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class MetaphorInterpretationEngine : IMetaphorInterpretationEngine
{
    private readonly IRubricIntelligenceAdminService _adminService;

    public MetaphorInterpretationEngine(IRubricIntelligenceAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<MetaphorInterpretationResult> EnrichAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string? language,
        CancellationToken cancellationToken = default)
    {
        var enriched = concepts.Select(Clone).ToList();
        var searchTerms = enriched
            .SelectMany(c => new[] { c.RawStatement }.Concat(c.SearchTerms))
            .Select(IntelligenceTextNormalizer.Normalize)
            .Where(t => t.Length >= 3)
            .Distinct()
            .ToList();

        var metaphors = await _adminService.SearchApprovedMetaphorsAsync(searchTerms, language, cancellationToken);
        var matched = 0;

        foreach (var concept in enriched)
        {
            var conceptNorm = IntelligenceTextNormalizer.Normalize(concept.RawStatement);
            var hit = metaphors.FirstOrDefault(m =>
                conceptNorm.Contains(m.NormalizedExpression, StringComparison.Ordinal)
                || m.NormalizedExpression.Contains(conceptNorm, StringComparison.Ordinal)
                || MetaphorTokensMatch(conceptNorm, m.NormalizedExpression));

            if (hit == null) continue;

            matched++;
            concept.ClinicalMeaning = hit.ClinicalMeaning;
            concept.HomeopathicMeaning = hit.RubricMeaning;
            if (!concept.SearchTerms.Contains(hit.RubricMeaning, StringComparer.OrdinalIgnoreCase))
            {
                concept.SearchTerms.Add(hit.RubricMeaning);
            }

            foreach (var term in hit.RubricMeaning.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (term.Length >= 3 && !concept.SearchTerms.Contains(term, StringComparer.OrdinalIgnoreCase))
                {
                    concept.SearchTerms.Add(term);
                }
            }

            concept.Confidence = Math.Max(concept.Confidence, hit.ConfidenceWeight);
            concept.IsSRP = concept.IsSRP || hit.ConfidenceWeight >= 0.9m;
        }

        return new MetaphorInterpretationResult
        {
            Concepts = enriched,
            MetaphorsMatched = matched,
        };
    }

    private static bool MetaphorTokensMatch(string conceptNorm, string metaphorNorm)
    {
        var metaphorTokens = metaphorNorm.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 4)
            .ToList();
        if (metaphorTokens.Count == 0) return false;
        return metaphorTokens.All(token => conceptNorm.Contains(token, StringComparison.Ordinal));
    }

    private static ClinicalConceptModel Clone(ClinicalConceptModel source) => new()
    {
        ConceptId = source.ConceptId,
        RawStatement = source.RawStatement,
        ClinicalMeaning = source.ClinicalMeaning,
        HomeopathicMeaning = source.HomeopathicMeaning,
        Category = source.Category,
        IsSRP = source.IsSRP,
        Modalities = source.Modalities.ToList(),
        Concomitants = source.Concomitants.ToList(),
        SearchTerms = source.SearchTerms.ToList(),
        Confidence = source.Confidence,
        SourceLanguage = source.SourceLanguage,
    };
}
