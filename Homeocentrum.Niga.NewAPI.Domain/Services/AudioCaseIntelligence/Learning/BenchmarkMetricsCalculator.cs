using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Learning;

public static class BenchmarkMetricsCalculator
{
    public static BenchmarkMetrics Compute(
        IReadOnlyList<AudioCaseSuggestedRubricModel> suggested,
        IReadOnlyList<AudioCaseRubricFeedback> feedbacks,
        string engineVersion)
    {
        var aiSuggestedCount = suggested.Count;
        var accepted = feedbacks.Count(x => x.FeedbackType == "Accepted");
        var rejected = feedbacks.Count(x => x.FeedbackType == "Rejected");
        var corrected = feedbacks.Count(x => x.FeedbackType == "Corrected");

        decimal? acceptanceRate = aiSuggestedCount > 0
            ? Math.Round((decimal)accepted / aiSuggestedCount, 4)
            : null;

        var precisionDenominator = accepted + rejected;
        decimal? precision = precisionDenominator > 0
            ? Math.Round((decimal)accepted / precisionDenominator, 4)
            : null;

        decimal? recall = aiSuggestedCount > 0
            ? Math.Round((decimal)accepted / aiSuggestedCount, 4)
            : null;

        decimal? f1 = precision.HasValue && recall.HasValue && precision + recall > 0
            ? Math.Round(2m * precision.Value * recall.Value / (precision.Value + recall.Value), 4)
            : null;

        decimal? falsePositiveRate = aiSuggestedCount > 0
            ? Math.Round((decimal)rejected / aiSuggestedCount, 4)
            : null;

        var topFiveIds = suggested
            .OrderByDescending(x => x.MatchScore)
            .ThenByDescending(x => x.ConfidenceScore ?? 0m)
            .Take(5)
            .Select(x => x.SubSectionId)
            .ToHashSet();

        var acceptedIds = feedbacks
            .Where(x => x.FeedbackType == "Accepted" && x.SubSectionId.HasValue)
            .Select(x => x.SubSectionId!.Value);

        bool? primaryInTop5 = topFiveIds.Count == 0
            ? null
            : acceptedIds.Any(topFiveIds.Contains);

        decimal? confidenceCalibration = null;
        if (accepted + rejected > 0)
        {
            var acceptedConfidence = feedbacks
                .Where(x => x.FeedbackType == "Accepted" && x.ConfidenceAtFeedback.HasValue)
                .Select(x => x.ConfidenceAtFeedback!.Value)
                .DefaultIfEmpty(0m)
                .Average();
            var expectedRate = accepted / (decimal)(accepted + rejected);
            confidenceCalibration = Math.Round(1m - Math.Abs(acceptedConfidence - expectedRate), 4);
        }

        return new BenchmarkMetrics
        {
            EngineVersion = string.IsNullOrWhiteSpace(engineVersion) ? "v1" : engineVersion,
            AiSuggestedCount = aiSuggestedCount,
            DoctorAcceptedCount = accepted,
            DoctorRejectedCount = rejected,
            DoctorCorrectedCount = corrected,
            PrimaryInTop5 = primaryInTop5,
            PrecisionScore = precision,
            RecallScore = recall,
            F1Score = f1,
            AcceptanceRate = acceptanceRate,
            FalsePositiveRate = falsePositiveRate,
            ConfidenceCalibration = confidenceCalibration,
        };
    }
}

public sealed class BenchmarkMetrics
{
    public string EngineVersion { get; init; } = "v1";

    public int AiSuggestedCount { get; init; }

    public int DoctorAcceptedCount { get; init; }

    public int DoctorRejectedCount { get; init; }

    public int DoctorCorrectedCount { get; init; }

    public bool? PrimaryInTop5 { get; init; }

    public decimal? PrecisionScore { get; init; }

    public decimal? RecallScore { get; init; }

    public decimal? F1Score { get; init; }

    public decimal? AcceptanceRate { get; init; }

    public decimal? FalsePositiveRate { get; init; }

    public decimal? ConfidenceCalibration { get; init; }
}
