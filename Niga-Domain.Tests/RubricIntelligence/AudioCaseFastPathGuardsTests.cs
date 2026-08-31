using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class AudioCaseFastPathGuardsTests
{
    [Fact]
    public void Fast_pipeline_never_calls_gpt_ai_suggestions()
    {
        Assert.False(AudioCaseFastPathGuards.ShouldSuggestAiRubrics(
            enableAiSuggestedRubrics: true,
            enableFastClinicalRetrievalPipeline: true,
            dbHitCount: 0,
            maxAiSuggestedRubrics: 25));

        Assert.False(AudioCaseFastPathGuards.ShouldSuggestAiRubrics(
            enableAiSuggestedRubrics: true,
            enableFastClinicalRetrievalPipeline: true,
            dbHitCount: 5,
            maxAiSuggestedRubrics: 25));
    }

    [Fact]
    public void Legacy_path_still_suggests_when_db_hits_are_under_cap()
    {
        Assert.True(AudioCaseFastPathGuards.ShouldSuggestAiRubrics(
            enableAiSuggestedRubrics: true,
            enableFastClinicalRetrievalPipeline: false,
            dbHitCount: 5,
            maxAiSuggestedRubrics: 25));
    }

    [Fact]
    public void Legacy_path_skips_when_db_hits_fill_the_cap()
    {
        Assert.False(AudioCaseFastPathGuards.ShouldSuggestAiRubrics(
            enableAiSuggestedRubrics: true,
            enableFastClinicalRetrievalPipeline: false,
            dbHitCount: 20,
            maxAiSuggestedRubrics: 25));
    }

    [Fact]
    public void Disabled_flag_never_suggests()
    {
        Assert.False(AudioCaseFastPathGuards.ShouldSuggestAiRubrics(
            enableAiSuggestedRubrics: false,
            enableFastClinicalRetrievalPipeline: false,
            dbHitCount: 0,
            maxAiSuggestedRubrics: 25));
    }
}
