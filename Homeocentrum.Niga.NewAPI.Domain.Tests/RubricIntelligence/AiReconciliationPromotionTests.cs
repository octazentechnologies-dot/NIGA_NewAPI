using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class AiReconciliationPromotionTests
{
    [Fact]
    public void Exact_name_match_is_case_and_whitespace_insensitive()
    {
        Assert.True(AiReconciliationPromotion.IsExactNameMatch("  Mind - Anxiety ", "MIND - ANXIETY"));
        Assert.False(AiReconciliationPromotion.IsExactNameMatch("Mind - Anxiety", "MIND - ANXIETY - morning"));
    }

    [Fact]
    public void Fuzzy_needs_higher_floor_than_exact()
    {
        Assert.True(AiReconciliationPromotion.ShouldPromote(isExact: true, score: 1.0m, exactMinConfidence: 0.7m, fuzzyMinConfidence: 0.85m));
        Assert.False(AiReconciliationPromotion.ShouldPromote(isExact: false, score: 0.70m, exactMinConfidence: 0.7m, fuzzyMinConfidence: 0.85m));
        Assert.True(AiReconciliationPromotion.ShouldPromote(isExact: false, score: 0.86m, exactMinConfidence: 0.7m, fuzzyMinConfidence: 0.85m));
    }

    [Fact]
    public void Exact_promotion_clears_ai_suggested_and_uses_exact_source()
    {
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 0,
            SubSectionName = "mind - anxiety",
            IsAiSuggested = true,
            Source = "AiSuggested",
            MatchScore = 0.4m,
        };

        AiReconciliationPromotion.Apply(rubric, 55, "MIND - ANXIETY", 1, 12, 1.0m, isExact: true);

        Assert.False(rubric.IsAiSuggested);
        Assert.Equal("Database", rubric.Source);
        Assert.Equal(AiReconciliationPromotion.ExactMatchSource, rubric.MatchSource);
        Assert.Equal(55, rubric.SubSectionId);
        Assert.Equal("MIND - ANXIETY", rubric.SubSectionName);
        Assert.Contains("exact", rubric.WhySuggested, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mind - anxiety", rubric.WhySuggested, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fuzzy_promotion_keeps_ai_suggested_badge()
    {
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 0,
            SubSectionName = "anxiety in the morning",
            IsAiSuggested = true,
            Source = "AiSuggested",
            MatchScore = 0.5m,
        };

        AiReconciliationPromotion.Apply(rubric, 88, "MIND - ANXIETY - morning", 1, 4, 0.88m, isExact: false);

        Assert.True(rubric.IsAiSuggested);
        Assert.Equal("AiSuggested", rubric.Source);
        Assert.Equal(AiReconciliationPromotion.FuzzyMatchSource, rubric.MatchSource);
        Assert.True(rubric.RequiresManualApproval);
        Assert.Equal(88, rubric.SubSectionId);
        Assert.Contains("fuzzy", rubric.WhySuggested, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AiReconciledExact", rubric.MatchSource);
    }
}
