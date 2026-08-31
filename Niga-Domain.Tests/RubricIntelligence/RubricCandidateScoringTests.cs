using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class RubricCandidateScoringTests
{
    [Fact]
    public void ComputeCompositeScore_WeightsAllSignals()
    {
        var composite = RubricCandidateScoring.ComputeCompositeScore(
            similarity: 0.90m,
            clinicalRelevance: 0.80m,
            evidence: 0.70m,
            doctorAcceptance: 0.60m);

        Assert.Equal(0.785m, composite);
    }

    [Fact]
    public void ComputeEvidenceScore_FullChainScoresHigher()
    {
        var full = new RubricEvidenceChainV3Model
        {
            TranscriptStatement = "fear before fit",
            PatientMeaning = "anticipatory fear",
            ClinicalConcept = "Anticipatory Anxiety",
            HomeopathicConcept = "Fear Before Convulsion",
            IsComplete = true,
        };

        var partial = new RubricEvidenceChainV3Model
        {
            HomeopathicConcept = "Fear Before Convulsion",
        };

        var fullScore = RubricCandidateScoring.ComputeEvidenceScore(full, null);
        var partialScore = RubricCandidateScoring.ComputeEvidenceScore(partial, null);

        Assert.True(fullScore > partialScore);
        Assert.True(fullScore >= 0.90m);
    }

    [Fact]
    public void TierAssigner_AssignsTier1Tier2Tier3()
    {
        var candidates = new List<RubricCandidateModel>
        {
            new() { SubSectionId = 1, SubSectionName = "A", CompositeScore = 0.92m },
            new() { SubSectionId = 2, SubSectionName = "B", CompositeScore = 0.80m },
            new() { SubSectionId = 3, SubSectionName = "C", CompositeScore = 0.65m },
        };

        RubricCandidateTierAssigner.Assign(candidates, new Configuration.RubricIntelligenceOptions
        {
            MaxRubricsTier1 = 5,
            MaxRubricsTier2 = 5,
            MaxRubricsTier3 = 5,
        });

        Assert.Equal("Tier1", candidates[0].RubricTier);
        Assert.Equal("Tier2", candidates[1].RubricTier);
        Assert.Equal("Tier3", candidates[2].RubricTier);
    }
}
