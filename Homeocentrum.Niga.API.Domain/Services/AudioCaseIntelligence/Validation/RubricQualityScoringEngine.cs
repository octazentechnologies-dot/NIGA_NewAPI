using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Interfaces;

namespace Homeocentrum.Niga.API.Domain.Services.AudioCaseIntelligence.Validation;

public class RubricQualityScoringEngine : IRubricQualityScoringEngine
{
    public RubricValidationResultModel Score(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<RubricValidationIssueModel> issues,
        bool isPrimaryLinked,
        RubricIntelligenceOptions options)
    {
        var baseScore = (rubric.ConfidenceScore ?? rubric.MatchScore) * 100m;
        if (isPrimaryLinked) baseScore += 12m;

        var penalty = issues.Sum(i => i.PenaltyPoints);
        var qualityScore = Math.Clamp(Math.Round(baseScore - penalty, 2), 0m, 100m);
        var hardReject = issues.Any(i => i.IsHardReject);

        return new RubricValidationResultModel
        {
            QualityScore = qualityScore,
            IsAccepted = !hardReject && qualityScore >= options.MinRubricQualityScore,
            Issues = issues.ToList(),
        };
    }
}
