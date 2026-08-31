using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.V6;

/// <summary>V6: multi-factor rubric ranking — not cosine similarity alone.</summary>
public static class V6EvidenceRanker
{
    public static decimal ComputeRankScore(
        AudioCaseSuggestedRubricModel rubric,
        V6ClinicalSymptomUnit? symptom,
        V6RubricExplainabilityModel? explainability,
        RubricIntelligenceOptions options)
    {
        var clinicalEvidence = rubric.EvidenceChain?.EvidenceStrength ?? 0m;
        var transcriptEvidence = HasTranscriptEvidence(rubric) ? 0.85m : 0m;
        var temporalEvidence = !string.IsNullOrWhiteSpace(symptom?.TemporalContext) ? 0.75m : 0.5m;
        var frequency = (rubric.EvidenceChain?.PatientStatements.Count ?? 0) >= 2 ? 0.8m : 0.55m;
        var ontologyMatch = explainability?.SearchStages?.Count > 0 ? 0.82m : 0.5m;
        var embedding = explainability?.EmbeddingScore ?? rubric.ConfidenceScore ?? rubric.MatchScore;
        var hierarchyDepth = explainability?.HierarchyPath != null ? 0.7m : 0.55m;
        var bootstrap = explainability?.SearchStages?.Any(s =>
            s.Contains("Bootstrap", StringComparison.OrdinalIgnoreCase)) == true ? 0.88m : 0m;
        var semanticConfidence = (rubric.QualityScore ?? 0m) / 100m;
        var contradictions = rubric.ValidationFlags?.Any(f =>
            f.Contains("Mismatch", StringComparison.OrdinalIgnoreCase)
            || f.Contains("Hallucination", StringComparison.OrdinalIgnoreCase)) == true ? -0.25m : 0m;
        var repertoryAuthority = string.Equals(rubric.MatchSource, RubricDiscoverySources.RepertoryDb, StringComparison.OrdinalIgnoreCase)
            ? 0.15m
            : 0m;
        var doctorLearning = string.Equals(rubric.MatchSource, RubricDiscoverySources.DoctorLearning, StringComparison.OrdinalIgnoreCase)
            ? 0.08m
            : 0m;

        var score =
            (clinicalEvidence * 0.18m)
            + (transcriptEvidence * 0.14m)
            + (temporalEvidence * 0.08m)
            + (frequency * 0.06m)
            + (ontologyMatch * 0.12m)
            + (embedding * 0.14m)
            + (hierarchyDepth * 0.06m)
            + (bootstrap * 0.05m)
            + (semanticConfidence * 0.12m)
            + repertoryAuthority
            + doctorLearning
            + contradictions;

        return Math.Round(Math.Clamp(score, 0m, 1m) * 100m, 2);
    }

    public static V6RubricExplainabilityModel BuildExplainability(
        AudioCaseSuggestedRubricModel rubric,
        V6ClinicalSymptomUnit? symptom,
        V6SymptomDiscoveryAudit? audit)
    {
        var sqlPaths = audit?.SqlHits.Select(h => $"{h.SearchStage}: {h.MatchPath}").ToList() ?? new List<string>();
        var hierarchyPath = audit?.SqlHits
            .FirstOrDefault(h => h.HierarchyDepth > 0)
            ?.MatchPath;

        return new V6RubricExplainabilityModel
        {
            TranscriptEvidence = symptom?.TranscriptEvidence ?? rubric.MatchedFrom,
            ClinicalReasoning = rubric.WhySuggested ?? rubric.SelectionReason,
            OntologyPath = symptom != null
                ? $"{symptom.SymptomClass} → {symptom.ClinicalLabel} → {symptom.HomeopathicLabel}"
                : null,
            SqlMatchPath = sqlPaths.Count > 0 ? string.Join(" | ", sqlPaths.Take(3)) : null,
            HierarchyPath = hierarchyPath,
            EmbeddingScore = rubric.ConfidenceScore ?? rubric.MatchScore,
            ConfidenceScore = rubric.EnterpriseConfidenceScore,
            ValidationScore = rubric.QualityScore,
            SearchStages = audit?.SqlHits.Select(h => h.SearchStage).Distinct().ToList() ?? new List<string>(),
            FinalExplanation = BuildFinalExplanation(rubric, symptom, audit),
        };
    }

    private static string BuildFinalExplanation(
        AudioCaseSuggestedRubricModel rubric,
        V6ClinicalSymptomUnit? symptom,
        V6SymptomDiscoveryAudit? audit)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(symptom?.TranscriptEvidence))
        {
            parts.Add($"Transcript: \"{Truncate(symptom.TranscriptEvidence, 80)}\"");
        }

        if (symptom != null)
        {
            parts.Add($"Concept: {symptom.HomeopathicLabel} ({symptom.SymptomClass})");
        }

        var topSql = audit?.SqlHits.OrderByDescending(h => h.SqlConfidence).FirstOrDefault();
        if (topSql != null)
        {
            parts.Add($"SQL {topSql.SearchStage}: {topSql.SubSectionName}");
        }

        parts.Add($"Rubric: {rubric.SubSectionName}");
        return string.Join(" → ", parts);
    }

    private static bool HasTranscriptEvidence(AudioCaseSuggestedRubricModel rubric) =>
        !string.IsNullOrWhiteSpace(rubric.EvidenceChain?.TranscriptExcerpt)
        || !string.IsNullOrWhiteSpace(rubric.MatchedFrom);

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
