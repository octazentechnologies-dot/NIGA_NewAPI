using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Validation.Enterprise;

public static class EnterpriseValidationStepNames
{
    public const string Evidence = "EvidenceValidation";
    public const string Clinical = "ClinicalValidation";
    public const string Gender = "GenderValidation";
    public const string Age = "AgeValidation";
    public const string Domain = "DomainValidation";
    public const string Hallucination = "HallucinationDetection";
    public const string Duplicate = "DuplicateDetection";
    public const string Confidence = "ConfidenceValidation";
}

public class EvidenceValidationStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Evidence;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context)
    {
        var chain = context.EvidenceChain;
        var isDbBacked = IsDatabaseBackedMatch(context.Rubric);
        var hasPatientEvidence = chain.PatientStatements.Any(x => !string.IsNullOrWhiteSpace(x))
            || !string.IsNullOrWhiteSpace(chain.TranscriptExcerpt)
            || !string.IsNullOrWhiteSpace(chain.MatchedSymptomPhrase)
            || !string.IsNullOrWhiteSpace(context.Rubric.MatchedFrom);

        if (!hasPatientEvidence)
        {
            return new RubricValidationIssueModel
            {
                Code = "MissingEvidence",
                Message = "Evidence validation failed: no patient statement supports this rubric.",
                PenaltyPoints = 100,
                IsHardReject = true,
            };
        }

        if (chain.ClinicalMeanings.Count == 0
            && context.LinkedConcept == null
            && string.IsNullOrWhiteSpace(context.Rubric.WhySuggested))
        {
            if (isDbBacked && hasPatientEvidence)
            {
                return null;
            }

            return new RubricValidationIssueModel
            {
                Code = "MissingClinicalEvidence",
                Message = "Evidence validation failed: no clinical meaning linked to this rubric.",
                PenaltyPoints = 80,
                IsHardReject = true,
            };
        }

        var minEvidence = isDbBacked
            ? Math.Min(context.Options.MinEvidenceStrength, 0.28m)
            : context.Options.MinEvidenceStrength;

        if (chain.EvidenceStrength < minEvidence)
        {
            // DB-backed hits (ConceptKeyword/Hybrid/Database/RepertoryDb) already have patient MatchedFrom;
            // Jaccard strength against long rubric tails is often low — soft warn, do not wipe the list.
            if (isDbBacked && hasPatientEvidence)
            {
                return new RubricValidationIssueModel
                {
                    Code = "WeakEvidence",
                    Message = $"Evidence strength ({chain.EvidenceStrength:0.00}) is below minimum ({minEvidence:0.00}) but DB-backed match retained.",
                    PenaltyPoints = 20,
                    IsHardReject = false,
                };
            }

            return new RubricValidationIssueModel
            {
                Code = "WeakEvidence",
                Message = $"Evidence strength ({chain.EvidenceStrength:0.00}) is below minimum ({minEvidence:0.00}).",
                PenaltyPoints = 60,
                IsHardReject = true,
            };
        }

        return null;
    }

    /// <summary>
    /// True for any real SubSectionMaster hit — keyword, hybrid, alias, embedding, or concept-graph repertory.
    /// These must not be rejected solely for incomplete V3 evidence-chain construction.
    /// </summary>
    internal static bool IsDatabaseBackedMatch(AudioCaseSuggestedRubricModel rubric)
    {
        if (rubric.SubSectionId <= 0)
            return false;

        if (string.Equals(rubric.ResultKind, "RepertoryRubric", StringComparison.OrdinalIgnoreCase))
            return true;

        var source = rubric.MatchSource ?? string.Empty;
        return source.Equals(RubricDiscoverySources.RepertoryDb, StringComparison.OrdinalIgnoreCase)
            || source.Equals("ConceptKeyword", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Database", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Hybrid", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Alias", StringComparison.OrdinalIgnoreCase)
            || source.Equals("Embedding", StringComparison.OrdinalIgnoreCase)
            || source.Equals("ConceptGraph", StringComparison.OrdinalIgnoreCase)
            || source.Equals("RubricCandidateEngine", StringComparison.OrdinalIgnoreCase)
            || source.Equals("AiReconciled", StringComparison.OrdinalIgnoreCase)
            || source.Contains("REPERTORY", StringComparison.OrdinalIgnoreCase);
    }
}

public class ClinicalConceptValidationStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Clinical;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context)
    {
        var rulesIssue = HomeopathicRulesEngine.Validate(
            context.Rubric,
            context.LinkedConcept,
            context.PrimarySymptom);

        if (rulesIssue != null)
            return rulesIssue;

        if (context.LinkedConcept == null)
        {
            var isDbBacked = EvidenceValidationStep.IsDatabaseBackedMatch(context.Rubric);

            // Keyword/hybrid/DB hits carry MatchedFrom / WhySuggested as the clinical link.
            if (isDbBacked
                && (!string.IsNullOrWhiteSpace(context.Rubric.MatchedFrom)
                    || !string.IsNullOrWhiteSpace(context.Rubric.WhySuggested)
                    || context.Rubric.SourceConceptId.HasValue))
                return null;

            return new RubricValidationIssueModel
            {
                Code = "ClinicalUnlinked",
                Message = "Clinical validation failed: rubric is not linked to a clinical concept.",
                PenaltyPoints = 70,
                IsHardReject = true,
            };
        }

        if (context.LinkedConcept.Confidence < 0.50m)
        {
            return new RubricValidationIssueModel
            {
                Code = "ClinicalLowConfidence",
                Message = $"Clinical concept confidence ({context.LinkedConcept.Confidence:0.00}) is too low.",
                PenaltyPoints = 65,
                IsHardReject = true,
            };
        }

        return null;
    }
}

public class GenderValidationStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Gender;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context) =>
        GenderRubricValidator.Validate(context.Rubric, context.Context.Patient);
}

public class AgeValidationStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Age;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context) =>
        AgeRubricValidator.Validate(context.Rubric, context.Context.Patient);
}

public class DomainValidationStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Domain;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context)
    {
        var issue = RepertoryDomainValidator.Validate(
            context.Rubric,
            context.LinkedConcept,
            context.EvidenceChain);

        if (issue == null)
            return null;

        return new RubricValidationIssueModel
        {
            Code = issue.Code,
            Message = issue.Message,
            PenaltyPoints = issue.PenaltyPoints,
            IsHardReject = true,
        };
    }
}

public class HallucinationDetectionStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Hallucination;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context)
    {
        var issue = RubricHallucinationDetector.Detect(
            context.Rubric,
            context.EvidenceChain,
            context.Options);

        if (issue == null)
            return null;

        // Preserve soft vs hard from the detector (do not force soft WeakEvidence into hard reject).
        return new RubricValidationIssueModel
        {
            Code = issue.Code,
            Message = issue.Message,
            PenaltyPoints = issue.PenaltyPoints,
            IsHardReject = issue.IsHardReject,
        };
    }
}

public class ConfidenceValidationStep : IRubricValidationStep
{
    public string StepName => EnterpriseValidationStepNames.Confidence;

    public RubricValidationIssueModel? Validate(RubricValidationStepContext context)
    {
        // Normalize: EnterpriseConfidenceScore is 0–100; ConfidenceScore/MatchScore are 0–1.
        var confidence = NormalizeConfidence01(
            context.Rubric.ConfidenceScore ?? context.Rubric.MatchScore,
            context.Rubric.EnterpriseConfidenceScore);
        var isDbBacked = EvidenceValidationStep.IsDatabaseBackedMatch(context.Rubric);
        var isInference = string.Equals(context.Rubric.MatchLayer, "Inference", StringComparison.OrdinalIgnoreCase)
            || string.Equals(context.Rubric.MatchSource, "Inference", StringComparison.OrdinalIgnoreCase);

        var minConfidence = isInference
            ? Math.Max(0.60m, context.Options.MinConceptConfidenceForInference)
            : isDbBacked ? 0.55m : 0.60m;

        if (confidence < minConfidence)
        {
            return new RubricValidationIssueModel
            {
                Code = "LowConfidence",
                Message = $"Confidence validation failed: score ({confidence:0.00}) is below minimum ({minConfidence:0.00}).",
                PenaltyPoints = 75,
                IsHardReject = true,
            };
        }

        if (context.LinkedConcept != null
            && context.LinkedConcept.Confidence < context.Options.MinConceptConfidenceForInference
            && isInference)
        {
            return new RubricValidationIssueModel
            {
                Code = "ConceptConfidenceTooLow",
                Message = $"Linked concept confidence ({context.LinkedConcept.Confidence:0.00}) is below inference threshold.",
                PenaltyPoints = 70,
                IsHardReject = true,
            };
        }

        return null;
    }

    /// <summary>
    /// Internal confidence is always 0–1. EnterpriseConfidenceScore is 0–100 and must not be compared to 0.60 raw.
    /// </summary>
    public static decimal NormalizeConfidence01(decimal? score01, decimal? enterpriseScore0To100)
    {
        if (score01 is > 0 and <= 1.5m)
            return score01.Value;

        if (enterpriseScore0To100 is > 1.5m)
            return Math.Clamp(enterpriseScore0To100.Value / 100m, 0m, 1m);

        if (score01 is > 1.5m)
            return Math.Clamp(score01.Value / 100m, 0m, 1m);

        return score01 ?? 0m;
    }
}

public static class RubricDuplicateDetector
{
    public static RubricValidationIssueModel? DetectWithinBatch(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyCollection<int> acceptedSubSectionIds,
        IReadOnlyCollection<string> acceptedNormalizedTails)
    {
        if (acceptedSubSectionIds.Contains(rubric.SubSectionId))
        {
            return new RubricValidationIssueModel
            {
                Code = "DuplicateRubric",
                Message = $"Duplicate detection failed: rubric '{rubric.SubSectionName}' already accepted in this case.",
                PenaltyPoints = 100,
                IsHardReject = true,
            };
        }

        var tail = NormalizeTail(rubric.SubSectionName);
        if (!string.IsNullOrWhiteSpace(tail) && acceptedNormalizedTails.Contains(tail))
        {
            return new RubricValidationIssueModel
            {
                Code = "DuplicateSymptomTail",
                Message = $"Duplicate detection failed: symptom tail '{tail}' already represented by another rubric.",
                PenaltyPoints = 90,
                IsHardReject = true,
            };
        }

        return null;
    }

    public static string NormalizeTail(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName)) return string.Empty;
        var dash = rubricName.LastIndexOf('-');
        var tail = dash > 0 && dash < rubricName.Length - 1
            ? rubricName[(dash + 1)..]
            : rubricName;
        return tail.Trim().ToUpperInvariant();
    }
}
