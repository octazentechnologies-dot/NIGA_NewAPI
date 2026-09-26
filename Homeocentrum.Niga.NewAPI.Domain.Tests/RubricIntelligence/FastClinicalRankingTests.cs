using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class FastClinicalRankingTests
{
    [Fact]
    public void Mmr_Prefers_Diverse_Rubrics_Over_Near_Duplicates()
    {
        var ranked = new List<AudioCaseSuggestedRubricModel>
        {
            Rubric(1, "EXTREMITIES-VIBRATION-Hands", 0.95m),
            Rubric(2, "EXTREMITIES-VIBRATION-sensation", 0.94m),
            Rubric(3, "EXTREMITIES-VIBRATION", 0.93m),
            Rubric(4, "MIND-FEAR-before fit", 0.90m),
            Rubric(5, "STOMACH-THIRST-large quantities", 0.88m),
            Rubric(6, "MIND-AWKWARD-drops things", 0.86m),
        };

        var selected = FastClinicalRanking.SelectWithMmr(ranked, targetCount: 4, lambda: 0.80m, minCanonicalScore: 0.45m);

        Assert.True(selected.Count >= 3);
        Assert.Contains(selected, r => r.SubSectionId == 1);
        Assert.Contains(selected, r => r.SubSectionId == 4);
        // Should not take all three vibration variants first
        var vibrationCount = selected.Count(r => r.SubSectionName.Contains("VIBRATION", StringComparison.OrdinalIgnoreCase));
        Assert.True(vibrationCount <= 2);
    }

    [Fact]
    public void Mmr_Stops_On_Score_Cliff_Instead_Of_Padding()
    {
        var ranked = new List<AudioCaseSuggestedRubricModel>
        {
            Rubric(1, "A", 0.95m),
            Rubric(2, "B", 0.93m),
            Rubric(3, "C", 0.91m),
            Rubric(4, "D", 0.90m),
            Rubric(5, "E", 0.89m),
            Rubric(6, "F", 0.40m), // cliff
            Rubric(7, "G", 0.38m),
        };

        var selected = FastClinicalRanking.SelectWithMmr(
            ranked, targetCount: 12, minCanonicalScore: 0.45m, scoreCliffRatio: 0.70m);

        Assert.Equal(5, selected.Count);
        Assert.DoesNotContain(selected, r => r.SubSectionId is 6 or 7);
    }

    [Fact]
    public void CanonicalScore_Penalizes_No_Evidence()
    {
        var concepts = new List<ClinicalConceptModel>
        {
            new() { RawStatement = "thirst for large quantities", SearchTerms = { "thirst", "water" } },
        };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 1,
                SubSectionName = "HEART-PALPITATION",
                MatchSource = "Embedding",
                ConfidenceScore = 0.99m,
                MatchScore = 99,
            },
            new()
            {
                SubSectionId = 2,
                SubSectionName = "STOMACH-THIRST-large quantities",
                MatchSource = "Keyword",
                ConfidenceScore = 0.80m,
                MatchScore = 80,
                MatchedFrom = "thirst",
            },
        };

        FastClinicalRanking.ApplyCanonicalScores(rubrics, concepts);
        var heart = rubrics.Single(r => r.SubSectionId == 1);
        var thirst = rubrics.Single(r => r.SubSectionId == 2);
        Assert.True(thirst.ConfidenceScore > heart.ConfidenceScore);
    }

    private static AudioCaseSuggestedRubricModel Rubric(int id, string name, decimal score) => new()
    {
        SubSectionId = id,
        SubSectionName = name,
        ConfidenceScore = score,
        MatchScore = score * 100,
        MatchSource = "Keyword",
    };
}
