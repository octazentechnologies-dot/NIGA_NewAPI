using Niga_Domain.Configuration;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Validation;

public static class RubricReviewFallbackHelper
{
    public static List<AudioCaseSuggestedRubricModel> ApplyIfEmpty(
        IReadOnlyList<AudioCaseSuggestedRubricModel> accepted,
        IReadOnlyList<AudioCaseSuggestedRubricModel> rejected,
        RubricIntelligenceOptions options,
        int maxCount = 15)
    {
        if (accepted.Count > 0 || !options.EnableV3ReviewFallback || rejected.Count == 0)
            return accepted.ToList();

        return rejected
            .Where(r =>
                (r.QualityScore ?? 0) >= options.MinRubricQualityScoreReviewFallback
                || (r.ConfidenceScore ?? r.MatchScore) >= 0.60m)
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .ThenByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .Take(maxCount)
            .Select(MarkReviewTier)
            .ToList();
    }

    public static AudioCaseSuggestedRubricModel MarkReviewTier(AudioCaseSuggestedRubricModel rubric)
    {
        rubric.RubricTier = "Review";
        rubric.RequiresManualApproval = true;
        rubric.RequiresDoctorReview = true;
        rubric.ValidationStatus = "ReviewSuggested";
        if (string.IsNullOrWhiteSpace(rubric.WhySuggested))
            rubric.WhySuggested = "Suggested for doctor review — did not pass strict auto-validation.";
        else if (!rubric.WhySuggested.Contains("review", StringComparison.OrdinalIgnoreCase))
            rubric.WhySuggested += " (Review tier — confirm before repertorizing.)";
        return rubric;
    }
}
