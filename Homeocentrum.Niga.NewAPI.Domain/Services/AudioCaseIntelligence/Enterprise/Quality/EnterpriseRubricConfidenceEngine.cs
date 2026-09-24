using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>Phase 10: enterprise rubric confidence combining all evidence signals.</summary>
public static class EnterpriseRubricConfidenceEngine
{
    public static decimal Compute(
        AudioCaseSuggestedRubricModel rubric,
        RubricIntelligenceOptions options)
    {
        var embedding = rubric.ConfidenceScore ?? rubric.MatchScore;
        var evidence = rubric.EvidenceChain?.EvidenceStrength ?? 0m;
        var transcriptCoverage = ComputeTranscriptCoverage(rubric);
        var kgScore = rubric.EnterpriseEvidenceChain?.Steps
            .FirstOrDefault(s => s.StepKey == RubricEvidenceChainStepKeys.EmbeddingMatch)
            ?.Score ?? 0m;
        var doctorLearning = rubric.MatchSource == RubricDiscoverySources.DoctorLearning ? 0.15m : 0m;
        var srpWeight = rubric.SuggestedIntensityNo >= 3 ? 0.08m : 0m;
        var repertoryMatch = rubric.SubSectionId > 0
            && !string.Equals(rubric.ResultKind, "AiClinicalConcept", StringComparison.OrdinalIgnoreCase)
            ? 0.20m
            : 0m;

        var clinicalEvidence = rubric.EvidenceChain?.ClinicalMeanings.Count > 0 ? 0.12m : 0m;

        var score =
            (embedding * 0.22m)
            + (evidence * 0.18m)
            + (transcriptCoverage * 0.15m)
            + (kgScore * 0.10m)
            + (doctorLearning)
            + (srpWeight)
            + (repertoryMatch)
            + (clinicalEvidence)
            + SourceAuthorityBoost(rubric);

        return Math.Round(Math.Clamp(score, 0m, 1m) * 100m, 2);
    }

    public static bool MeetsDisplayThreshold(decimal enterpriseConfidence, RubricIntelligenceOptions options) =>
        enterpriseConfidence >= options.MinEnterpriseRubricConfidenceScore;

    private static decimal SourceAuthorityBoost(AudioCaseSuggestedRubricModel rubric) =>
        rubric.MatchSource switch
        {
            RubricDiscoverySources.RepertoryDb => 0.18m,
            RubricDiscoverySources.Bootstrap => 0.14m,
            RubricDiscoverySources.DoctorLearning => 0.12m,
            RubricDiscoverySources.KnowledgeGraph => 0.10m,
            RubricDiscoverySources.EnterpriseEmbedding => 0.08m,
            _ => 0m,
        };

    private static decimal ComputeTranscriptCoverage(AudioCaseSuggestedRubricModel rubric)
    {
        if (string.IsNullOrWhiteSpace(rubric.EvidenceChain?.TranscriptExcerpt)
            && string.IsNullOrWhiteSpace(rubric.MatchedFrom))
        {
            return 0m;
        }

        return Math.Min(1m, (rubric.EvidenceChain?.PatientStatements.Count ?? 0) * 0.25m + 0.50m);
    }
}
