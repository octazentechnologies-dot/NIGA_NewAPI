using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Explanation;

/// <summary>V7: builds full explainability chain for every rubric.</summary>
public interface IExplanationService
{
    V7RubricExplainability BuildExplanation(
        AudioCaseSuggestedRubricModel rubric,
        V7ExtractedSymptom? symptom,
        V7SymptomSearchAudit? audit);

    void ApplyToRubric(
        AudioCaseSuggestedRubricModel rubric,
        V7RubricExplainability explainability);
}

public class ExplanationService : IExplanationService
{
    public V7RubricExplainability BuildExplanation(
        AudioCaseSuggestedRubricModel rubric,
        V7ExtractedSymptom? symptom,
        V7SymptomSearchAudit? audit)
    {
        var candidate = audit?.Candidates.FirstOrDefault(c => c.SubSectionId == rubric.SubSectionId);
        var strategies = audit?.SearchStrategiesUsed ?? new List<string>();
        if (candidate != null && !strategies.Contains(candidate.SearchStrategy))
        {
            strategies = strategies.Concat(new[] { candidate.SearchStrategy }).ToList();
        }

        var strategyLabel = strategies.Count > 1
            ? V7SearchStrategyNames.Hybrid
            : strategies.FirstOrDefault() ?? candidate?.SearchStrategy ?? "Database";

        var explainability = new V7RubricExplainability
        {
            OriginalTranscript = symptom?.TranscriptEvidence ?? rubric.MatchedFrom,
            ExtractedSymptom = symptom?.Text,
            NormalizedSymptom = symptom?.Normalized ?? audit?.NormalizationChain.LastOrDefault(),
            VocabularyExpansion = audit?.VocabularyTerms ?? new List<string>(),
            SynonymExpansion = audit?.SynonymTerms ?? new List<string>(),
            OntologyPath = audit?.OntologyTerms ?? new List<string>(),
            SearchStrategy = strategyLabel,
            MatchedRubric = rubric.SubSectionName,
            Confidence = rubric.EnterpriseConfidenceScore ?? rubric.QualityScore ?? (rubric.ConfidenceScore ?? 0) * 100m,
        };

        explainability.FinalExplanation = BuildChain(explainability);
        return explainability;
    }

    public void ApplyToRubric(
        AudioCaseSuggestedRubricModel rubric,
        V7RubricExplainability explainability)
    {
        rubric.V7Explainability = explainability;
        rubric.SelectionReason = explainability.FinalExplanation;
        rubric.WhySuggested = explainability.FinalExplanation;

        if (rubric.V6Explainability == null)
        {
            rubric.V6Explainability = new V6RubricExplainabilityModel
            {
                TranscriptEvidence = explainability.OriginalTranscript,
                ClinicalReasoning = explainability.ExtractedSymptom,
                OntologyPath = string.Join(" → ", explainability.OntologyPath.Take(4)),
                SqlMatchPath = explainability.SearchStrategy,
                FinalExplanation = explainability.FinalExplanation,
                ConfidenceScore = explainability.Confidence,
                SearchStages = new List<string> { explainability.SearchStrategy ?? "Database" },
            };
        }
    }

    private static string BuildChain(V7RubricExplainability e)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(e.OriginalTranscript))
        {
            parts.Add($"Transcript: \"{Truncate(e.OriginalTranscript, 80)}\"");
        }

        if (!string.IsNullOrWhiteSpace(e.NormalizedSymptom))
        {
            parts.Add($"Normalized: {e.NormalizedSymptom}");
        }

        if (e.SynonymExpansion.Count > 0)
        {
            parts.Add($"Synonyms: {string.Join(", ", e.SynonymExpansion.Take(4))}");
        }

        if (!string.IsNullOrWhiteSpace(e.MatchedRubric))
        {
            parts.Add($"Rubric: {e.MatchedRubric}");
        }

        parts.Add($"Strategy: {e.SearchStrategy}");
        parts.Add($"Confidence: {e.Confidence:0}%");
        return string.Join(" → ", parts);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
