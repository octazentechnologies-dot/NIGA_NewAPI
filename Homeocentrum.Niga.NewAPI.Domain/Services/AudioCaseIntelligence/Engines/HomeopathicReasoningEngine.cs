using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class HomeopathicReasoningEngine : IHomeopathicReasoningEngine
{
    private static readonly string[] SrpIndicators =
    {
        "strange", "rare", "peculiar", "never before", "unusual", "odd",
        "aura", "prodrome", "never had", "only at",
    };

    public List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts)
    {
        return concepts.Select(concept =>
        {
            var copy = Clone(concept);
            var text = $"{copy.RawStatement} {copy.ClinicalMeaning} {copy.HomeopathicMeaning}".ToLowerInvariant();

            if (!copy.IsSRP)
            {
                copy.IsSRP = SrpIndicators.Any(i => text.Contains(i, StringComparison.Ordinal))
                             || copy.Category == "mental"
                             || (copy.Category == "causation" && copy.Confidence >= 0.85m);
            }

            if (string.IsNullOrWhiteSpace(copy.HomeopathicMeaning))
            {
                copy.HomeopathicMeaning = copy.IsSRP
                    ? $"SRP — {copy.ClinicalMeaning ?? copy.RawStatement}"
                    : copy.ClinicalMeaning ?? copy.RawStatement;
            }

            if (copy.IsSRP && copy.Confidence < 0.85m)
            {
                copy.Confidence = Math.Max(copy.Confidence, 0.85m);
            }

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
