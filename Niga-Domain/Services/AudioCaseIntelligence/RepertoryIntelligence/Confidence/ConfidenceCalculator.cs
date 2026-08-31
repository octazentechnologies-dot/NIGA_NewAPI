using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Confidence;

/// <summary>V7: composite confidence from search strategy weights and symptom confidence.</summary>
public interface IConfidenceCalculator
{
    decimal Compute(V7RubricCandidate candidate, V7ExtractedSymptom symptom);

    decimal ComputeForRubric(AudioCaseSuggestedRubricModel rubric, V7RubricExplainability? explainability);
}

public class ConfidenceCalculator : IConfidenceCalculator
{
    public decimal Compute(V7RubricCandidate candidate, V7ExtractedSymptom symptom)
    {
        var baseScore = candidate.NormalizedScore > 0 ? candidate.NormalizedScore : candidate.RawScore;
        var symptomBoost = symptom.Confidence * 0.1m;
        var embeddingBoost = candidate.EmbeddingSimilarity * 10m;

        return Math.Round(Math.Clamp(baseScore + symptomBoost + embeddingBoost, 0m, 100m), 2);
    }

    public decimal ComputeForRubric(AudioCaseSuggestedRubricModel rubric, V7RubricExplainability? explainability)
    {
        if (explainability?.Confidence > 0)
        {
            return explainability.Confidence;
        }

        var quality = rubric.QualityScore ?? 0m;
        var match = (rubric.ConfidenceScore ?? rubric.MatchScore) * 100m;
        return Math.Round(Math.Max(quality, match), 2);
    }
}
