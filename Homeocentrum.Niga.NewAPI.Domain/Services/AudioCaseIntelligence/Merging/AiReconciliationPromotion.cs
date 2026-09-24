using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>Exact vs fuzzy promotion of GPT-invented names onto SubSectionMaster.</summary>
public static class AiReconciliationPromotion
{
    public const string ExactMatchSource = "AiReconciledExact";
    public const string FuzzyMatchSource = "AiReconciledFuzzy";

    public static bool IsExactNameMatch(string? proposed, string? actual) =>
        string.Equals(proposed?.Trim(), actual?.Trim(), StringComparison.OrdinalIgnoreCase);

    public static bool ShouldPromote(bool isExact, decimal score, decimal exactMinConfidence, decimal fuzzyMinConfidence) =>
        isExact
            ? score >= exactMinConfidence
            : score >= fuzzyMinConfidence;

    public static void Apply(
        AudioCaseSuggestedRubricModel rubric,
        int subSectionId,
        string? liveName,
        int? sectionId,
        int remedyCount,
        decimal score,
        bool isExact)
    {
        var beforeName = rubric.SubSectionName;
        var afterName = string.IsNullOrWhiteSpace(liveName) ? beforeName : liveName.Trim();

        rubric.SubSectionId = subSectionId;
        rubric.SubSectionName = afterName;
        rubric.SectionId = sectionId;
        rubric.RemedyCountForSort = remedyCount;
        rubric.RemedyCount = remedyCount > 0 ? remedyCount : null;
        rubric.Scores ??= new RubricUnifiedScoresModel();
        rubric.Scores.ConceptMatchConfidence = score;
        rubric.Scores.FinalHybridScore = Math.Max(rubric.MatchScore, score);
        rubric.ConfidenceScore = rubric.Scores.FinalHybridScore;
        rubric.MatchScore = rubric.Scores.FinalHybridScore ?? rubric.MatchScore;

        if (isExact)
        {
            rubric.IsAiSuggested = false;
            rubric.Source = "Database";
            rubric.MatchSource = ExactMatchSource;
            rubric.MatchLayer = ExactMatchSource;
        }
        else
        {
            rubric.IsAiSuggested = true;
            rubric.Source = "AiSuggested";
            rubric.MatchSource = FuzzyMatchSource;
            rubric.MatchLayer = FuzzyMatchSource;
            rubric.RequiresManualApproval = true;
            rubric.RequiresDoctorReview = true;
        }

        var kind = isExact ? "exact" : "fuzzy";
        var audit = $"Reconciled {kind} '{beforeName}' → '{afterName}' (id {subSectionId}, score {score:F2}).";
        rubric.WhySuggested = string.IsNullOrWhiteSpace(rubric.WhySuggested)
            ? audit
            : $"{rubric.WhySuggested} | {audit}";
    }
}
