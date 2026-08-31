using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class SymptomExtractionEngineTests
{
    private readonly SymptomExtractionEngine _engine = new();

    [Fact]
    public void Merge_CombinesV1SymptomsAndConceptSearchTerms()
    {
        var v1 = new List<AudioCaseSymptomModel>
        {
            new() { Phrase = "headache", SearchTerms = new List<string> { "headache" }, Category = "particular", IntensityHint = 2 },
        };

        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "vibration before fit",
                ClinicalMeaning = "Prodromal aura before convulsion",
                SearchTerms = new List<string> { "aura", "convulsion", "prodrome" },
                Category = "particular",
                IsSRP = true,
            },
        };

        var merged = _engine.Merge(v1, concepts, new List<AudioCaseSymptomModel>());

        Assert.Contains(merged, s => s.Phrase.Contains("aura", StringComparison.OrdinalIgnoreCase) || s.SearchTerms.Contains("aura"));
        Assert.Contains(merged, s => s.Phrase.Equals("headache", StringComparison.OrdinalIgnoreCase));
    }
}
