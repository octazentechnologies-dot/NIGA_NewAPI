namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>Cheap, testable gates for wasted work on the fast clinical path.</summary>
public static class AudioCaseFastPathGuards
{
    public const int V1CombinedCap = 20;

    /// <summary>
    /// GPT <c>SuggestAiRubricsAsync</c> must not run on the fast path — those rows are dropped
    /// by <c>SubSectionId &gt; 0</c> anyway.
    /// </summary>
    public static bool ShouldSuggestAiRubrics(
        bool enableAiSuggestedRubrics,
        bool enableFastClinicalRetrievalPipeline,
        int dbHitCount,
        int maxAiSuggestedRubrics)
    {
        if (!enableAiSuggestedRubrics || enableFastClinicalRetrievalPipeline)
            return false;

        var aiSlots = Math.Min(maxAiSuggestedRubrics, Math.Max(0, V1CombinedCap - dbHitCount));
        if (dbHitCount == 0)
            aiSlots = maxAiSuggestedRubrics;

        return aiSlots > 0;
    }
}
