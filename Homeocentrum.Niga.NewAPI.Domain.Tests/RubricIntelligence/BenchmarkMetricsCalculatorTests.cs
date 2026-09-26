using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Learning;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class BenchmarkMetricsCalculatorTests
{
    [Fact]
    public void Compute_CalculatesAcceptancePrecisionAndPrimaryInTop5()
    {
        var suggested = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 101, SubSectionName = "Aura", MatchScore = 0.95m, ConfidenceScore = 0.92m },
            new() { SubSectionId = 102, SubSectionName = "Convulsion", MatchScore = 0.80m },
            new() { SubSectionId = 103, SubSectionName = "Sleep", MatchScore = 0.70m },
        };

        var feedbacks = new List<AudioCaseRubricFeedback>
        {
            new() { FeedbackType = "Accepted", SubSectionId = 101, ConfidenceAtFeedback = 0.92m },
            new() { FeedbackType = "Rejected", SubSectionId = 102, ConfidenceAtFeedback = 0.80m },
        };

        var metrics = BenchmarkMetricsCalculator.Compute(suggested, feedbacks, "v2");

        Assert.Equal(3, metrics.AiSuggestedCount);
        Assert.Equal(1, metrics.DoctorAcceptedCount);
        Assert.Equal(1, metrics.DoctorRejectedCount);
        Assert.Equal(0.3333m, metrics.AcceptanceRate);
        Assert.Equal(0.5m, metrics.PrecisionScore);
        Assert.True(metrics.PrimaryInTop5);
        Assert.Equal(0.3333m, metrics.FalsePositiveRate);
    }

    [Fact]
    public void Compute_ReturnsNullPrimaryInTop5_WhenNoSuggestions()
    {
        var metrics = BenchmarkMetricsCalculator.Compute(
            new List<AudioCaseSuggestedRubricModel>(),
            new List<AudioCaseRubricFeedback>(),
            "v2");

        Assert.Null(metrics.PrimaryInTop5);
        Assert.Null(metrics.AcceptanceRate);
    }

    [Fact]
    public void Compute_F1_IsHarmonicMeanOfPrecisionAndRecall()
    {
        var suggested = Enumerable.Range(1, 4)
            .Select(i => new AudioCaseSuggestedRubricModel { SubSectionId = i, MatchScore = i })
            .ToList();

        var feedbacks = new List<AudioCaseRubricFeedback>
        {
            new() { FeedbackType = "Accepted", SubSectionId = 1 },
            new() { FeedbackType = "Accepted", SubSectionId = 2 },
            new() { FeedbackType = "Rejected", SubSectionId = 3 },
        };

        var metrics = BenchmarkMetricsCalculator.Compute(suggested, feedbacks, "v2");

        Assert.Equal(0.6667m, metrics.PrecisionScore);
        Assert.Equal(0.5m, metrics.RecallScore);
        Assert.Equal(0.5714m, metrics.F1Score);
    }
}
