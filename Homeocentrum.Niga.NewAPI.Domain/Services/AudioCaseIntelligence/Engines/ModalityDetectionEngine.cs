using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class ModalityDetectionEngine : IModalityDetectionEngine
{
    private static readonly string[] ModalityKeywords =
    {
        "better", "worse", "aggravat", "ameliorat", "morning", "evening", "night",
        "cold", "heat", "warm", "weather", "motion", "rest", "eating", "fasting",
    };

    public List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts)
    {
        return concepts.Select(concept =>
        {
            var copy = Clone(concept);
            if (copy.Modalities.Count > 0) return copy;

            var text = $"{copy.RawStatement} {copy.ClinicalMeaning}".ToLowerInvariant();
            copy.Modalities = ModalityKeywords
                .Where(k => text.Contains(k, StringComparison.Ordinal))
                .Select(k => k.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return copy;
        }).ToList();
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
