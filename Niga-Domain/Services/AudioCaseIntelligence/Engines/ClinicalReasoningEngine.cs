using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class ClinicalReasoningEngine : IClinicalReasoningEngine
{
    public List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts)
    {
        return concepts.Select(concept =>
        {
            var copy = Clone(concept);
            if (!string.IsNullOrWhiteSpace(copy.ClinicalMeaning)) return copy;

            copy.ClinicalMeaning = InferClinicalMeaning(copy.RawStatement);
            return copy;
        }).ToList();
    }

    private static string InferClinicalMeaning(string rawStatement)
    {
        var lower = rawStatement.ToLowerInvariant();
        if (lower.Contains("vibration") && (lower.Contains("fit") || lower.Contains("convulsion")))
        {
            return "Prodromal sensory aura preceding convulsive episode";
        }

        if (lower.Contains("fear") && lower.Contains("dark"))
        {
            return "Anxiety or fear specifically triggered in darkness";
        }

        return rawStatement.Trim();
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
