using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class RepertoryTierEngineTests
{
    [Fact]
    public void ResolveTierWithRepertory_KentHighConfidence_ReturnsPrimary()
    {
        var maps = new List<RepertoryMapModel>
        {
            new() { SourceCode = "KENT", PriorityOrder = 1, IsPrimarySource = true, MappingConfidence = 1m },
        };

        var tier = RepertoryTierEngine.ResolveTierWithRepertory(null, maps, 0.90m);

        Assert.Equal("Primary", tier);
    }

    [Fact]
    public void EnrichRubric_AddsKentAndCompleteSources()
    {
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 101,
            SubSectionName = "FEAR - dark",
            MatchScore = 0.75m,
            ConfidenceScore = 0.75m,
            RubricTier = "Secondary",
        };

        var maps = new Dictionary<int, List<RepertoryMapModel>>
        {
            [101] = new()
            {
                new() { SourceCode = "KENT", SourceName = "Kent Repertory", PriorityOrder = 1, IsPrimarySource = true, MappingConfidence = 1m },
                new() { SourceCode = "COMPLETE", SourceName = "Complete Repertory", PriorityOrder = 2, IsPrimarySource = false, MappingConfidence = 0.95m },
            },
        };

        var enriched = RepertoryTierEngine.EnrichRubric(rubric, maps);

        Assert.Equal("Secondary", enriched.RubricTier);
        Assert.Equal("KENT", enriched.PrimaryRepertorySource);
        Assert.Contains("KENT", enriched.RepertorySources);
        Assert.Contains("COMPLETE", enriched.RepertorySources);
    }

    [Fact]
    public void ResolveTierWithRepertory_InferenceTier_IsPreserved()
    {
        var tier = RepertoryTierEngine.ResolveTierWithRepertory(
            "Inference",
            new List<RepertoryMapModel> { new() { SourceCode = "KENT" } },
            0.95m);

        Assert.Equal("Inference", tier);
    }
}
