using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;

public static class HomeopathicRulesEngine
{
    public static RubricValidationIssueModel? Validate(
        AudioCaseSuggestedRubricModel rubric,
        ClinicalConceptModel? linkedConcept,
        PrimarySymptomModel? primarySymptom)
    {
        var isInference = string.Equals(rubric.MatchLayer, "Inference", StringComparison.OrdinalIgnoreCase)
            || string.Equals(rubric.MatchSource, "Inference", StringComparison.OrdinalIgnoreCase);

        if (isInference && linkedConcept == null)
        {
            return new RubricValidationIssueModel
            {
                Code = "InferenceUnlinked",
                Message = "Inferred rubric has no linked clinical concept.",
                PenaltyPoints = 25,
                IsHardReject = false,
            };
        }

        if (isInference && linkedConcept != null && !linkedConcept.IsSRP && linkedConcept.Confidence < 0.80m)
        {
            return new RubricValidationIssueModel
            {
                Code = "InferenceLowConfidenceConcept",
                Message = "Inferred rubric linked to low-confidence non-SRP concept.",
                PenaltyPoints = 15,
                IsHardReject = false,
            };
        }

        if (primarySymptom != null
            && linkedConcept != null
            && primarySymptom.SourceConceptId.HasValue
            && linkedConcept.ConceptId != primarySymptom.SourceConceptId.Value
            && isInference
            && (rubric.ConfidenceScore ?? rubric.MatchScore) < 0.75m)
        {
            return new RubricValidationIssueModel
            {
                Code = "OffPrimarySymptom",
                Message = "Inferred rubric is not linked to the primary symptom.",
                PenaltyPoints = 10,
                IsHardReject = false,
            };
        }

        return null;
    }
}
