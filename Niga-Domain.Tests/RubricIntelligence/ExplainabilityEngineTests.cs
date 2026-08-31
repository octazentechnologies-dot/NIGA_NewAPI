using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class ExplainabilityEngineTests
{
    private readonly ExplainabilityEngine _engine = new();

    [Fact]
    public void Enrich_PopulatesExplainabilityFromMatchingConcept()
    {
        var conceptId = Guid.NewGuid();
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 10,
                SubSectionName = "MIND - FEAR - fit, before",
                MatchScore = 0.88m,
                ConfidenceScore = 0.9m,
                MatchedFrom = "fear before fit",
                MatchLayer = "Hybrid",
                WhySuggested = "Hybrid match",
            },
        };

        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = conceptId,
                RawStatement = "fear before fit",
                ClinicalMeaning = "anticipatory fear before convulsion",
                HomeopathicMeaning = "SRP fear prodrome",
                SearchTerms = new List<string> { "fear", "fit" },
            },
        };

        var enriched = _engine.Enrich(rubrics, concepts, Array.Empty<CausationLinkModel>());

        Assert.NotNull(enriched[0].Explainability);
        Assert.Equal("fear before fit", enriched[0].Explainability!.PatientStatement);
        Assert.Equal("Primary", enriched[0].RubricTier);
    }
}
