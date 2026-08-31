using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class MultiConceptDiscoveryTests
{
    [Fact]
    public void CategoryKeywordDetector_CompoundStatement_DetectsAllCategories()
    {
        var text = "I have fear before fits, forget names, desire sweets, sleep late, dream of snakes, worse in cold, sexual desire increased, weakness and headache";

        var categories = CategoryKeywordDetector.DetectCategories(text, null).ToList();

        Assert.Contains("Fear", categories);
        Assert.Contains("Forgetfulness", categories);
        Assert.Contains("Food Desire", categories);
        Assert.Contains("Sleep", categories);
        Assert.Contains("Dream", categories);
        Assert.Contains("Modality", categories);
        Assert.Contains("Sexual", categories);
        Assert.Contains("Physical", categories);
    }

    [Fact]
    public void ConceptGraphAssemblyEngine_MultiCategoryStatement_ProducesMultipleConceptsAndEdges()
    {
        var items = new List<MultiConceptDiscoveryItemGptModel>
        {
            new()
            {
                MeaningIndex = 1,
                Category = "Fear",
                ClinicalConceptName = "Anticipatory fear before seizure",
                HomeopathicConceptName = "Fear Before Convulsion",
                Importance = "High",
                SymptomClass = "MentalGeneral",
                Confidence = 0.92m,
                EvidenceSpan = "fear before fit",
            },
            new()
            {
                MeaningIndex = 1,
                Category = "Forgetfulness",
                ClinicalConceptName = "Memory weakness for names",
                HomeopathicConceptName = "Forgetfulness Names",
                Importance = "Medium",
                SymptomClass = "MentalGeneral",
                Confidence = 0.88m,
                EvidenceSpan = "forgets names",
            },
            new()
            {
                MeaningIndex = 1,
                Category = "Food Desire",
                ClinicalConceptName = "Craving for sweets",
                HomeopathicConceptName = "Desire For Sweets",
                Importance = "Medium",
                SymptomClass = "Food",
                Confidence = 0.85m,
                EvidenceSpan = "desires sweets",
            },
        };

        var meanings = new List<PatientMeaningNodeModel>
        {
            new()
            {
                RawStatement = "fear before fit, forgets names, desires sweets",
                NormalizedMeaning = "fear before fit, forgets names, desires sweets",
                Confidence = 0.90m,
            },
        };

        var result = ConceptGraphAssemblyEngine.Assemble(items, meanings, "epilepsy");

        Assert.Equal(3, result.ClinicalConcepts.Count);
        Assert.Equal(3, result.HomeopathicConcepts.Count);
        Assert.True(result.Edges.Count >= 9);
        Assert.NotEmpty(result.PrimaryConcepts);
        Assert.Contains(result.PrimaryConcepts, c => c.Category == "Fear");
        Assert.Contains(result.SecondaryConcepts.Concat(result.SupportingConcepts), c => c.Category == "Food Desire");
    }

    [Fact]
    public void ConceptTierAssignmentEngine_SRPConcept_IsPrimary()
    {
        var discovery = new MultiConceptDiscoveryResult
        {
            ClinicalConcepts =
            [
                new ClinicalConceptNodeModel { ConceptName = "Aura", Confidence = 0.95m, SymptomCategory = "General", MeaningIndex = 0 },
                new ClinicalConceptNodeModel { ConceptName = "Fear", Confidence = 0.80m, SymptomCategory = "Fear", MeaningIndex = 0 },
            ],
            HomeopathicConcepts =
            [
                new HomeopathicConceptNodeModel
                {
                    ClinicalConceptIndex = 0,
                    ConceptName = "Convulsion Aura",
                    Category = "General",
                    IsSRP = true,
                    Weight = 3m,
                    Confidence = 0.95m,
                },
                new HomeopathicConceptNodeModel
                {
                    ClinicalConceptIndex = 1,
                    ConceptName = "Anticipatory Fear",
                    Category = "Fear",
                    Weight = 1.5m,
                    Confidence = 0.80m,
                },
            ],
        };

        var tiers = ConceptTierAssignmentEngine.Assign(discovery, "epilepsy with aura");

        Assert.Contains(tiers.Primary, c => c.IsSRP && c.HomeopathicConceptName == "Convulsion Aura");
    }
}
