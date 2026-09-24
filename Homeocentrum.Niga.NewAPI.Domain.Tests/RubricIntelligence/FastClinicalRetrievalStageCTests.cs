using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class FastClinicalRetrievalStageCTests
{
    [Fact]
    public void FromSymptoms_Builds_Searchable_Concepts_Without_Duplicates()
    {
        var symptoms = new List<AudioCaseSymptomModel>
        {
            new()
            {
                Phrase = "vibration in hands",
                Category = "Particular",
                SearchTerms = { "vibration", "hands" },
            },
            new()
            {
                Phrase = "vibration in hands",
                Category = "Particular",
            },
            new()
            {
                Phrase = "increased sexual desire",
                Category = "Mental",
                SearchTerms = { "sexual desire" },
            },
        };

        var summary = new AudioCaseSummaryModel
        {
            ChiefComplaint = "fits with aura",
            Mentals = { "fear of heights" },
        };

        var concepts = ConceptGraphConceptMapper.FromSymptoms(symptoms, summary);

        Assert.Contains(concepts, c => c.RawStatement.Contains("vibration", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(concepts, c => c.RawStatement.Contains("sexual", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(concepts, c => c.RawStatement.Contains("heights", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(concepts, c => c.Category == "ChiefComplaint");
        Assert.Equal(1, concepts.Count(c => c.RawStatement.Contains("vibration", StringComparison.OrdinalIgnoreCase)));
        Assert.All(concepts, c => Assert.NotEmpty(c.SearchTerms));
    }

    [Fact]
    public void FromSymptoms_DoesNot_Inject_Convulsion_From_Fit_Alone()
    {
        var symptoms = new List<AudioCaseSymptomModel>
        {
            new()
            {
                Phrase = "fear before fit",
                Category = "mental",
                SearchTerms = { "fear", "before", "fit", "convulsion", "fear before fit" },
            },
        };

        var concepts = ConceptGraphConceptMapper.FromSymptoms(symptoms);
        var terms = concepts.Single().SearchTerms;

        Assert.Contains(terms, t => t.Equals("fit", StringComparison.OrdinalIgnoreCase) || t.Contains("fit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(terms, t => t.Equals("convulsion", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(terms, t => t.Contains("epilepsy", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FromSymptoms_Keeps_Convulsion_When_Patient_Said_It()
    {
        var symptoms = new List<AudioCaseSymptomModel>
        {
            new()
            {
                Phrase = "fear before convulsion",
                Category = "mental",
                SearchTerms = { "fear", "convulsion" },
            },
        };

        var concepts = ConceptGraphConceptMapper.FromSymptoms(symptoms);
        Assert.Contains(concepts.Single().SearchTerms, t => t.Equals("convulsion", StringComparison.OrdinalIgnoreCase));
    }
}
