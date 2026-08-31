using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class ConcomitantDetectionEngine : IConcomitantDetectionEngine
{
    private static readonly string[] ConcomitantMarkers =
    {
        "with", "along with", "accompanied", "during", "before", "after", "and also",
    };

    public List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts)
    {
        if (concepts.Count <= 1)
        {
            return concepts.ToList();
        }

        return concepts.Select(concept =>
        {
            var copy = Clone(concept);
            if (copy.Concomitants.Count > 0) return copy;

            var related = concepts
                .Where(c => c.ConceptId != concept.ConceptId)
                .Where(c => SharesContext(concept, c))
                .Select(c => c.ClinicalMeaning ?? c.RawStatement)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            copy.Concomitants = related;
            return copy;
        }).ToList();
    }

    private static bool SharesContext(ClinicalConceptModel a, ClinicalConceptModel b)
    {
        var textA = a.RawStatement.ToLowerInvariant();
        return ConcomitantMarkers.Any(marker => textA.Contains(marker, StringComparison.Ordinal))
               || a.Category == "concomitant"
               || (a.Category == b.Category && a.Category == "particular");
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
