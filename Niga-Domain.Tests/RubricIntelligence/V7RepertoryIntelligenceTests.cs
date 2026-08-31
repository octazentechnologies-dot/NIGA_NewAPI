using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Normalization;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Synonyms;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class V7SynonymEngineTests
{
    private readonly SynonymEngine _engine = new();

    [Theory]
    [InlineData("fit", "convulsion")]
    [InlineData("fear", "terror")]
    [InlineData("salt", "craves salt")]
    public void ExpandSynonyms_IncludesRelatedTerms(string term, string expectedFragment)
    {
        var synonyms = _engine.ExpandSynonyms(term);
        Assert.Contains(synonyms, s => s.Contains(expectedFragment, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExpandAll_RespectsMaxTerms()
    {
        var expanded = _engine.ExpandAll(new[] { "fit", "fear", "salt" }, maxTerms: 10);
        Assert.True(expanded.Count <= 10);
        Assert.NotEmpty(expanded);
    }
}

public class V7ClinicalNormalizerTests
{
    private readonly ClinicalNormalizer _normalizer = new();

    [Fact]
    public void Normalize_StrongShock_ProducesAuraChain()
    {
        var result = _normalizer.Normalize(new V7ExtractedSymptom
        {
            Text = "strong shock before the fit",
        });

        Assert.Contains(result.NormalizationChain, c => c.Contains("Aura", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.FinalConcept.Contains("Aura", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Normalize_DropsObjects_ProducesDropsThingsChain()
    {
        var result = _normalizer.Normalize(new V7ExtractedSymptom
        {
            Text = "drops objects before fit",
        });

        Assert.Contains(result.NormalizationChain, c => c.Contains("Drops things", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Normalize_ForgetEverything_ProducesMemoryChain()
    {
        var result = _normalizer.Normalize(new V7ExtractedSymptom
        {
            Text = "forgets everything after the attack",
        });

        Assert.Contains(result.NormalizationChain, c => c.Contains("Forgetfulness", StringComparison.OrdinalIgnoreCase));
    }
}
