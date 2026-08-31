using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Learning;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class DoctorLearningScoringTests
{
    private static readonly RubricIntelligenceOptions Options = new()
    {
        EnableDoctorLearningEngine = true,
        DoctorLearningMaxAccumulatedWeight = 1.0m,
    };

    [Fact]
    public void ApplyConceptRankingBoost_IncreasesScoreForPositiveWeight()
    {
        var weights = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Fear Before Convulsion"] = 0.50m,
        };

        var boosted = DoctorLearningScoring.ApplyConceptRankingBoost(
            2.0m,
            "Fear Before Convulsion",
            weights,
            Options);

        Assert.True(boosted > 2.0m);
    }

    [Fact]
    public void ApplyClinicalRelevanceBoost_ClampsToOne()
    {
        var weights = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["Anticipatory fear"] = 2.0m,
        };

        var boosted = DoctorLearningScoring.ApplyClinicalRelevanceBoost(
            0.95m,
            "Anticipatory fear",
            "Fear Before Convulsion",
            weights,
            Options);

        Assert.Equal(1m, boosted);
    }

    [Fact]
    public void ApplyDoctorAcceptanceBoost_BlendsRubricRateAndMapping()
    {
        var snapshot = new DoctorLearningWeightsSnapshot
        {
            ConceptRubricMapping = { [("Fear Before Convulsion", 101)] = 0.40m },
            RubricAcceptanceRates = { [101] = 0.80m },
        };

        var boosted = DoctorLearningScoring.ApplyDoctorAcceptanceBoost(
            0.50m,
            "Fear Before Convulsion",
            101,
            snapshot,
            Options);

        Assert.True(boosted > 0.50m);
    }

    [Fact]
    public void ConceptTierAssignmentEngine_AppliesLearnedRankingBoost()
    {
        var discovery = new MultiConceptDiscoveryResult
        {
            ClinicalConcepts =
            [
                new ClinicalConceptNodeModel
                {
                    ConceptName = "Anticipatory fear",
                    Confidence = 0.80m,
                },
                new ClinicalConceptNodeModel
                {
                    ConceptName = "Forgetfulness",
                    Confidence = 0.90m,
                },
            ],
            HomeopathicConcepts =
            [
                new HomeopathicConceptNodeModel
                {
                    ClinicalConceptIndex = 0,
                    ConceptName = "Fear Before Convulsion",
                    Weight = 2.0m,
                    Confidence = 0.85m,
                },
                new HomeopathicConceptNodeModel
                {
                    ClinicalConceptIndex = 1,
                    ConceptName = "Memory Weakness",
                    Weight = 2.0m,
                    Confidence = 0.85m,
                },
            ],
        };

        var learned = new DoctorLearningWeightsSnapshot
        {
            ConceptRanking = { ["Fear Before Convulsion"] = 1.0m },
        };
        var withLearning = ConceptTierAssignmentEngine.Assign(discovery, null, learned, Options);

        Assert.Contains(
            withLearning.Primary,
            x => x.HomeopathicConceptName == "Fear Before Convulsion");
    }
}

public class DoctorLearningTypesTests
{
    [Theory]
    [InlineData(RejectReasonStages.Meaning)]
    [InlineData(RejectReasonStages.Metaphor)]
    [InlineData(RejectReasonStages.ClinicalConcept)]
    [InlineData(RejectReasonStages.HomeopathicConcept)]
    [InlineData(RejectReasonStages.RubricMapping)]
    [InlineData(RejectReasonStages.Other)]
    public void RejectReasonStages_RecognizesAllowedStages(string stage)
    {
        Assert.True(RejectReasonStages.IsValid(stage));
        Assert.False(RejectReasonStages.IsValid("Unknown"));
    }

    [Theory]
    [InlineData("Approved", "Accepted")]
    [InlineData("Edited", "Corrected")]
    [InlineData("Rejected", "Rejected")]
    public void FeedbackTypeAliases_AreRecognized(string input, string expectedNormalized)
    {
        var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Accepted", "Approved", "Rejected", "Corrected", "Edited",
        };

        Assert.Contains(input, valid);

        var normalized = input switch
        {
            var t when string.Equals(t, "Approved", StringComparison.OrdinalIgnoreCase) => "Accepted",
            var t when string.Equals(t, "Accepted", StringComparison.OrdinalIgnoreCase) => "Accepted",
            var t when string.Equals(t, "Rejected", StringComparison.OrdinalIgnoreCase) => "Rejected",
            var t when string.Equals(t, "Edited", StringComparison.OrdinalIgnoreCase) => "Corrected",
            _ => "Corrected",
        };

        Assert.Equal(expectedNormalized, normalized);
    }
}
