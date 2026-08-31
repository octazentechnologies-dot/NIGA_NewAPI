using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services;

namespace Niga_Domain.Services.AudioCaseIntelligence.Validation;

public static class RubricHallucinationDetector
{
    public static RubricValidationIssueModel? Detect(
        AudioCaseSuggestedRubricModel rubric,
        RubricEvidenceChainModel evidenceChain,
        RubricIntelligenceOptions options)
    {
        var rubricTail = ExtractTail(rubric.SubSectionName);
        var similarity = MaxEvidenceSimilarity(rubricTail, evidenceChain, rubric);
        var minSimilarity = IsInferenceLayer(rubric)
            ? options.MinEvidenceSimilarityForInference
            : options.MinEvidenceSimilarityForRubric;

        if (similarity <= 0m)
        {
            return new RubricValidationIssueModel
            {
                Code = "Hallucination",
                Message = "No patient evidence supports this rubric suggestion.",
                PenaltyPoints = 55,
                IsHardReject = true,
            };
        }

        if (similarity < minSimilarity)
        {
            return new RubricValidationIssueModel
            {
                Code = "Hallucination",
                Message = $"Evidence similarity ({similarity:0.00}) is below threshold ({minSimilarity:0.00}) for '{rubric.SubSectionName}'.",
                PenaltyPoints = 50,
                IsHardReject = similarity < minSimilarity - 0.15m,
            };
        }

        if (evidenceChain.EvidenceStrength < options.MinEvidenceStrength
            && similarity < minSimilarity + 0.05m)
        {
            return new RubricValidationIssueModel
            {
                Code = "WeakEvidence",
                Message = "Linked evidence strength is too weak for confident rubric mapping.",
                PenaltyPoints = 20,
                IsHardReject = false,
            };
        }

        return null;
    }

    private static bool IsInferenceLayer(AudioCaseSuggestedRubricModel rubric) =>
        string.Equals(rubric.MatchLayer, "Inference", StringComparison.OrdinalIgnoreCase)
        || string.Equals(rubric.MatchSource, "Inference", StringComparison.OrdinalIgnoreCase);

    private static string ExtractTail(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName)) return string.Empty;
        var dash = rubricName.IndexOf('-');
        return dash > 0 && dash < rubricName.Length - 1
            ? rubricName[(dash + 1)..].Trim()
            : rubricName.Trim();
    }

    private static decimal MaxEvidenceSimilarity(
        string rubricTail,
        RubricEvidenceChainModel evidenceChain,
        AudioCaseSuggestedRubricModel rubric)
    {
        var parts = evidenceChain.PatientStatements
            .Concat(evidenceChain.ClinicalMeanings)
            .Concat(new[] { evidenceChain.MatchedSymptomPhrase, evidenceChain.TranscriptExcerpt, rubric.MatchedFrom })
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return parts.Select(p => Math.Max(
                AudioCaseAiProcessor.ComputeTextSimilarity(rubricTail, p!),
                KeywordCoverageScore(rubricTail, p!)))
            .DefaultIfEmpty(0m)
            .Max();
    }

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "OF", "A", "AN", "THE", "THAT", "THIS", "WILL", "WITH", "AND", "OR", "TO", "IN", "ON", "AT", "BEFORE", "AFTER",
    };

    private static decimal KeywordCoverageScore(string rubricTail, string evidencePart)
    {
        var tokens = (rubricTail ?? string.Empty).ToUpperInvariant()
            .Split(new[] { ' ', '-', ',', '.', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !StopWords.Contains(t))
            .ToList();

        if (tokens.Count == 0) return 0m;

        var evidenceUpper = evidencePart.ToUpperInvariant();
        var hits = tokens.Count(token => evidenceUpper.Contains(token, StringComparison.Ordinal));
        return Math.Round((decimal)hits / tokens.Count, 4);
    }

    private static string BuildEvidenceText(RubricEvidenceChainModel evidenceChain, AudioCaseSuggestedRubricModel rubric)
    {
        var parts = evidenceChain.PatientStatements
            .Concat(evidenceChain.ClinicalMeanings)
            .Concat(new[] { evidenceChain.MatchedSymptomPhrase, evidenceChain.TranscriptExcerpt, rubric.MatchedFrom })
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(' ', parts);
    }
}
