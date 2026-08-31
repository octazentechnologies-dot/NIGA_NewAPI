using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Task 6: additive unified scoring contract for Database + AiSuggested candidates.
/// </summary>
public static class RubricUnifiedContractHelper
{
    public static List<AudioCaseSuggestedRubricModel> ApplyUnifiedContract(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics)
    {
        var ranked = rubrics
            .OrderByDescending(r => r.Scores?.FinalHybridScore
                ?? r.ConfidenceScore
                ?? r.MatchScore)
            .ThenByDescending(r => r.RemedyCountForSort)
            .ToList();

        for (var i = 0; i < ranked.Count; i++)
        {
            var r = ranked[i];
            r.Rank = i + 1;
            r.Source ??= r.IsAiSuggested || r.SubSectionId <= 0 ? "AiSuggested" : "Database";
            r.RemedyCount ??= r.RemedyCountForSort > 0 ? r.RemedyCountForSort : null;
            r.EvidenceChainComplete ??=
                r.EvidenceChain != null
                && (r.EvidenceChain.PatientStatements.Count > 0 || !string.IsNullOrWhiteSpace(r.EvidenceChain.TranscriptExcerpt))
                && r.EvidenceChain.ClinicalMeanings.Count > 0;

            r.Scores ??= new RubricUnifiedScoresModel
            {
                ConceptMatchConfidence = r.ConfidenceScore ?? r.MatchScore,
                FinalHybridScore = r.ConfidenceScore ?? r.MatchScore,
                CalibratedAcceptanceProbability = r.ConfidenceScore ?? r.MatchScore,
                EmbeddingCosine = r.MatchLayer?.Contains("Embedding", StringComparison.OrdinalIgnoreCase) == true
                    ? r.MatchScore
                    : null,
                AliasMatch = r.MatchLayer?.Contains("Alias", StringComparison.OrdinalIgnoreCase) == true
                    ? r.MatchScore
                    : null,
            };

            if (r.Scores.FinalHybridScore == null)
                r.Scores.FinalHybridScore = r.ConfidenceScore ?? r.MatchScore;

            r.MatchedFromDetail ??= new RubricMatchedFromModel
            {
                PatientStatement = r.MatchedFrom ?? r.Explainability?.PatientStatement,
                NormalizedMeaning = r.Explainability?.PatientStatement ?? r.Explainability?.ClinicalMeaning,
                ClinicalConcept = r.Explainability?.ClinicalMeaning,
                HomeopathicConcept = r.Explainability?.HomeopathicMeaning,
                MetaphorResolution = r.WhySuggested,
                SymptomClass = r.RubricTier,
                CausationLinked = r.Explainability?.CausationChain.Count > 0,
            };

            r.Validation ??= new RubricValidationSummaryModel
            {
                Domain = r.ValidationStatus,
                Gender = null,
                Hallucination = r.ValidationFlags.Any(f =>
                    f.Contains("hallucin", StringComparison.OrdinalIgnoreCase)),
            };
        }

        return ranked;
    }
}
