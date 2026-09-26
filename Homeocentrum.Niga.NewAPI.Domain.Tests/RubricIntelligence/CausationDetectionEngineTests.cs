using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class CausationDetectionEngineTests
{
    private readonly CausationDetectionEngine _engine = new();

    [Fact]
    public void Detect_FindsPatternAfterAnger()
    {
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "patient became angry",
                ClinicalMeaning = "anger",
                Category = "mental",
                Confidence = 0.9m,
            },
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "convulsion followed",
                ClinicalMeaning = "convulsion",
                Category = "particular",
                Confidence = 0.88m,
            },
        };

        var result = _engine.Detect(
            concepts,
            "After anger the patient developed convulsion during sleep.");

        Assert.NotEmpty(result.Links);
        Assert.Contains(result.Links, l =>
            l.CauseText.Contains("anger", StringComparison.OrdinalIgnoreCase)
            && l.EffectText.Contains("convulsion", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Detect_LinksCausationCategoryConcepts()
    {
        var causeId = Guid.NewGuid();
        var effectId = Guid.NewGuid();
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = causeId,
                RawStatement = "since grief",
                ClinicalMeaning = "grief",
                Category = "causation",
                Confidence = 0.92m,
            },
            new()
            {
                ConceptId = effectId,
                RawStatement = "insomnia",
                ClinicalMeaning = "sleeplessness",
                Category = "particular",
                Confidence = 0.8m,
            },
        };

        var result = _engine.Detect(concepts, "Since grief patient cannot sleep.");

        Assert.Contains(result.Links, l => l.CauseConceptId == causeId && l.EffectConceptId == effectId);
    }
}
