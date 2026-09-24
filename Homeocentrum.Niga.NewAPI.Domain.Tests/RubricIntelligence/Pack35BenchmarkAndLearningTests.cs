using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Benchmark;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class Pack35BenchmarkAndLearningTests
{
    [Fact]
    public void PrecisionAt10_Computes_From_Accepted_Set()
    {
        var suggested = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var accepted = new HashSet<int> { 1, 3, 5, 99 };

        var m = RubricBenchmarkMetrics.ComputeAtK(suggested, accepted, k: 10);

        Assert.Equal(3, m.HitCount);
        Assert.Equal(0.3m, m.PrecisionAtK);
        Assert.Equal(0.75m, m.RecallAtK); // 3/4
    }

    [Fact]
    public void DoctorLearning_Does_Not_Rescue_Zero_Evidence()
    {
        var options = new RubricIntelligenceOptions
        {
            EnableDoctorLearningEngine = true,
            FastPipelineEnableDoctorLearning = true,
            FastPipelineMinEvidenceScore = 0.15m,
            FastPipelineMinCanonicalScore = 0.45m,
        };
        var learned = new DoctorLearningWeightsSnapshot
        {
            RubricAcceptanceRates = { [9] = 1.0m },
        };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 9,
                SubSectionName = "HEART - PALPITATION",
                ConfidenceScore = 0.2m,
                EvidenceScore = 0.05m,
                MatchScore = 20,
            },
        };

        FastClinicalRanking.ApplyDoctorLearningBoost(rubrics, Array.Empty<ClinicalConceptModel>(), learned, options);

        Assert.Equal(0.2m, rubrics[0].ConfidenceScore);
        Assert.DoesNotContain("DoctorLearningBoost", rubrics[0].ValidationFlags);
    }

    [Fact]
    public void DoctorLearning_Boosts_Evidence_Backed_Rubric()
    {
        var options = new RubricIntelligenceOptions
        {
            EnableDoctorLearningEngine = true,
            FastPipelineEnableDoctorLearning = true,
            FastPipelineMinEvidenceScore = 0.15m,
            FastPipelineMinCanonicalScore = 0.45m,
        };
        var learned = new DoctorLearningWeightsSnapshot
        {
            RubricAcceptanceRates = { [10] = 1.0m },
        };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 10,
                SubSectionName = "EXTREMITIES - VIBRATION - Hands",
                ConfidenceScore = 0.80m,
                EvidenceScore = 0.70m,
                MatchScore = 80,
                MatchedFrom = "vibration in hands",
            },
        };

        FastClinicalRanking.ApplyDoctorLearningBoost(rubrics, Array.Empty<ClinicalConceptModel>(), learned, options);

        Assert.True(rubrics[0].ConfidenceScore > 0.80m);
        Assert.Contains("DoctorLearningBoost", rubrics[0].ValidationFlags);
    }

    [Fact]
    public void SessionGate_Prevents_Duplicate_Enter()
    {
        var gate = new Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseSessionProcessingGate();
        var id = Guid.NewGuid();
        Assert.True(gate.TryEnter(id));
        Assert.False(gate.TryEnter(id));
        Assert.True(gate.IsInFlight(id));
        gate.Exit(id);
        Assert.False(gate.IsInFlight(id));
        Assert.True(gate.TryEnter(id));
        gate.Exit(id);
    }
}
