using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class ExplainabilityEngine : IExplainabilityEngine
{
    public List<AudioCaseSuggestedRubricModel> Enrich(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<CausationLinkModel> causationLinks)
    {
        return rubrics.Select(rubric =>
        {
            var concept = FindBestConcept(rubric, concepts);
            var causationChain = BuildCausationChain(concept, causationLinks);
            var tier = rubric.RubricTier ?? ResolveTier(rubric);
            var reviewRequired = rubric.RequiresDoctorReview
                || tier == "Inference"
                || rubric.RequiresManualApproval;

            var explainability = new RubricExplainabilityModel
            {
                PatientStatement = concept?.RawStatement ?? rubric.MatchedFrom,
                ClinicalMeaning = concept?.ClinicalMeaning ?? concept?.RawStatement,
                HomeopathicMeaning = concept?.HomeopathicMeaning ?? concept?.ClinicalMeaning,
                WhySuggested = rubric.WhySuggested ?? rubric.InferenceReason,
                MatchLayer = rubric.MatchLayer ?? rubric.MatchSource,
                ConfidenceScore = rubric.ConfidenceScore ?? rubric.MatchScore,
                RubricTier = tier,
                ReviewRequired = reviewRequired,
                CausationChain = causationChain,
                SourceConceptId = rubric.SourceConceptId ?? concept?.ConceptId,
                EvidenceChain = rubric.EvidenceChain,
                QualityScore = rubric.QualityScore,
                ValidationFlags = rubric.ValidationFlags,
            };

            return new AudioCaseSuggestedRubricModel
            {
                SubSectionId = rubric.SubSectionId,
                SubSectionName = rubric.SubSectionName,
                SectionId = rubric.SectionId,
                MatchScore = rubric.MatchScore,
                SuggestedIntensityNo = rubric.SuggestedIntensityNo,
                MatchedFrom = rubric.MatchedFrom,
                RemedyCountForSort = rubric.RemedyCountForSort,
                IsAiSuggested = rubric.IsAiSuggested,
                MatchSource = rubric.MatchSource,
                ConfidenceScore = rubric.ConfidenceScore,
                WhySuggested = explainability.WhySuggested,
                EngineVersion = rubric.EngineVersion,
                RequiresManualApproval = rubric.RequiresManualApproval,
                HomeopathicWeight = rubric.HomeopathicWeight,
                MatchLayer = explainability.MatchLayer,
                RubricTier = tier,
                RequiresDoctorReview = reviewRequired,
                SourceConceptId = explainability.SourceConceptId,
                InferenceReason = rubric.InferenceReason,
                Explainability = explainability,
                EvidenceChain = rubric.EvidenceChain,
                QualityScore = rubric.QualityScore,
                ValidationStatus = rubric.ValidationStatus,
                ValidationFlags = rubric.ValidationFlags,
                IsPrimarySymptomLinked = rubric.IsPrimarySymptomLinked,
            };
        }).ToList();
    }

    private static ClinicalConceptModel? FindBestConcept(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts)
    {
        // Bug B: never fuzzy-steal another concept's citation. Strict ID / exact MatchedFrom only.
        if (rubric.SourceConceptId.HasValue)
        {
            var linked = concepts.FirstOrDefault(c => c.ConceptId == rubric.SourceConceptId.Value);
            if (linked != null) return linked;
        }

        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
        {
            var exact = concepts.FirstOrDefault(c =>
                string.Equals(c.RawStatement, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.ClinicalMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || string.Equals(c.HomeopathicMeaning, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;
        }

        return null;
    }

    private static List<string> BuildCausationChain(
        ClinicalConceptModel? concept,
        IReadOnlyList<CausationLinkModel> causationLinks)
    {
        if (concept == null) return new List<string>();

        return causationLinks
            .Where(l => l.CauseConceptId == concept.ConceptId || l.EffectConceptId == concept.ConceptId)
            .Select(l => $"{l.CauseText} → {l.EffectText}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveTier(AudioCaseSuggestedRubricModel rubric)
    {
        if (string.Equals(rubric.MatchLayer, "Inference", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rubric.MatchSource, "Inference", StringComparison.OrdinalIgnoreCase))
        {
            return "Inference";
        }

        var score = rubric.ConfidenceScore ?? rubric.MatchScore;
        if (score >= 0.85m) return "Primary";
        if (score >= 0.70m) return "Secondary";
        return "Confirmatory";
    }
}
