using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Monitoring;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class AiMonitoringMetricsCalculatorTests
{
    [Fact]
    public void ComputeDaily_AggregatesBenchmarkAndCoverageMetrics()
    {
        var benchmarks = new List<AudioCaseRubricBenchmark>
        {
            new()
            {
                PrecisionScore = 0.80m,
                RecallScore = 0.75m,
                AcceptanceRate = 0.70m,
                PrimaryInTop5 = true,
                F1Score = 0.77m,
            },
            new()
            {
                PrecisionScore = 0.60m,
                RecallScore = 0.65m,
                AcceptanceRate = 0.50m,
                PrimaryInTop5 = false,
                F1Score = 0.62m,
            },
        };

        var coverage = new List<AiCaseCoverageMetrics>
        {
            new() { TranscriptCoverage = 0.90m },
            new() { TranscriptCoverage = 0.80m },
        };

        var confidence = new List<AiRubricConfidence>
        {
            new() { FinalScore = 0.88m },
            new() { FinalScore = 0.72m },
        };

        var validations = new List<AiRubricValidationV3>
        {
            new()
            {
                ValidationStatus = "Rejected",
                ValidationFlagsJson = """{"issueCodes":["Hallucination"]}""",
            },
            new() { ValidationStatus = "Accepted" },
        };

        var embedding = new AiMonitoringEmbeddingSnapshot
        {
            FreshnessHours = 12m,
            RubricCoverage = 0.95m,
            ConceptCoverage = 1m,
        };

        var metrics = AiMonitoringMetricsCalculator.ComputeDaily(
            benchmarks,
            coverage,
            confidence,
            validations,
            feedbackCount: 4,
            embedding);

        Assert.Equal(2, metrics.SessionsAnalyzed);
        Assert.Equal(0.70m, metrics.PrecisionScore);
        Assert.Equal(0.70m, metrics.RecallScore);
        Assert.Equal(0.60m, metrics.DoctorAcceptanceRate);
        Assert.Equal(0.85m, metrics.TranscriptCoverage);
        Assert.Equal(0.80m, metrics.AverageConfidence);
        Assert.Equal(0.50m, metrics.PrimaryRubricAccuracy);
        Assert.Equal(1, metrics.HallucinationCount);
        Assert.Equal(0.50m, metrics.HallucinationRate);
        Assert.Equal(12m, metrics.EmbeddingFreshnessHours);
    }

    [Fact]
    public void BuildKpiCards_MarksLowPrecisionAsAttention()
    {
        var current = new AiMonitoringComputedMetrics { PrecisionScore = 0.55m };
        var previous = new AiMonitoringComputedMetrics { PrecisionScore = 0.60m };

        var cards = AiMonitoringMetricsCalculator.BuildKpiCards(current, previous);
        var precision = cards.First(x => x.MetricKey == AiMonitoringMetricKeys.Precision);

        Assert.Equal("Attention", precision.Status);
        Assert.Equal("Down", precision.TrendDirection);
    }

    [Fact]
    public void BuildEmbeddingHealth_ResolvesFreshnessStatus()
    {
        var health = AiMonitoringMetricsCalculator.BuildEmbeddingHealth(new AiMonitoringEmbeddingSnapshot
        {
            FreshnessHours = 24m,
            RubricCoverage = 0.92m,
            EmbeddedRubrics = 920,
            TotalActiveRubrics = 1000,
        });

        Assert.Equal("Fresh", health.FreshnessStatus);
        Assert.Equal("Healthy", health.CoverageStatus);
        Assert.Equal(0.92m, health.RubricEmbeddingCoverage);
    }
}
