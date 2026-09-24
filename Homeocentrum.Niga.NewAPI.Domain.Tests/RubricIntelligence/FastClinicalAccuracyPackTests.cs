using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class FastClinicalAccuracyPackTests
{
    [Fact]
    public void SpeakerNegation_Drops_DoctorOnly_Negated_Symptoms()
    {
        var symptoms = new List<AudioCaseSymptomModel>
        {
            new() { Phrase = "chest pain" },
            new() { Phrase = "vibration in hands" },
        };
        var messages = new List<AudioCaseMessageModel>
        {
            new() { Role = "Doctor", Text = "Do you have chest pain?" },
            new() { Role = "Patient", Text = "No chest pain at all." },
            new() { Role = "Patient", Text = "I feel vibration in hands." },
        };

        var kept = FastClinicalEvidenceGate.FilterSpeakerNegatedSymptoms(symptoms, messages);

        Assert.DoesNotContain(kept, s => s.Phrase.Contains("chest", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(kept, s => s.Phrase.Contains("vibration", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenderGate_Rejects_FemaleOnly_Rubric_For_Male()
    {
        var patient = new PatientClinicalContext { Gender = 0 }; // male
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 1, SubSectionName = "GENITALIA FEMALE - MENSES - painful" },
            new() { SubSectionId = 2, SubSectionName = "EXTREMITIES - VIBRATION - Hands" },
        };

        var kept = FastClinicalEvidenceGate.ApplyGenderGate(rubrics, patient);

        Assert.Single(kept);
        Assert.Equal(2, kept[0].SubSectionId);
    }

    [Fact]
    public void HierarchyGate_Rejects_Unsupported_Deep_Leaf()
    {
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                RawStatement = "vibration in hands",
                SearchTerms = { "vibration", "hands" },
            },
        };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 1,
                SubSectionName = "EXTREMITIES - VIBRATION - Hands",
                MatchedFrom = "vibration in hands",
            },
            new()
            {
                SubSectionId = 2,
                SubSectionName = "EXTREMITIES - VIBRATION - Hands - Left - Index finger",
                MatchedFrom = "vibration",
            },
        };

        var kept = FastClinicalEvidenceGate.ApplyHierarchySpecificityGate(rubrics, concepts);

        Assert.Contains(kept, r => r.SubSectionId == 1);
        Assert.DoesNotContain(kept, r => r.SubSectionId == 2);
    }

    [Fact]
    public void HallucinationGate_Drops_NoEvidence_And_NonDb()
    {
        var concepts = new List<ClinicalConceptModel>
        {
            new() { RawStatement = "thirst for large quantities", SearchTerms = { "thirst" } },
        };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 0,
                SubSectionName = "Invented thirst concept",
                MatchScore = 99,
            },
            new()
            {
                SubSectionId = 9,
                SubSectionName = "HEART - PALPITATION",
                MatchedFrom = "unrelated",
                ConfidenceScore = 0.99m,
            },
            new()
            {
                SubSectionId = 10,
                SubSectionName = "STOMACH - THIRST - large quantities",
                MatchedFrom = "thirst",
                ConfidenceScore = 0.8m,
            },
        };

        var kept = FastClinicalEvidenceGate.ApplyHallucinationHardGate(rubrics, concepts);

        Assert.Single(kept);
        Assert.Equal(10, kept[0].SubSectionId);
        Assert.False(kept[0].Validation?.Hallucination);
    }

    [Fact]
    public void EvidenceContract_Sets_PatientEvidence_And_DbFlags()
    {
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                ConceptId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                RawStatement = "vibration in hands",
                ClinicalMeaning = "vibration hands",
                SearchTerms = { "vibration", "hands" },
            },
        };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 42,
                SubSectionName = "EXTREMITIES - VIBRATION - Hands",
                MatchedFrom = "vibration in hands",
                MatchSource = "Keyword",
                ConfidenceScore = 0.82m,
                SourceConceptId = concepts[0].ConceptId,
            },
        };

        FastClinicalEvidenceGate.AttachEvidenceContract(rubrics, concepts);

        Assert.Equal("vibration in hands", rubrics[0].PatientEvidence);
        Assert.True(rubrics[0].IsDbBacked);
        Assert.True(rubrics[0].EvidenceChainComplete);
        Assert.False(string.IsNullOrWhiteSpace(rubrics[0].HierarchyPath));
        Assert.Equal("Lexical", rubrics[0].EvidenceType);
    }

    /// <summary>
    /// Phase 52 regression: vibration/hands/fit case must keep hands vibration
    /// and must not invent epilepsy from "fit" alone. No hard-coded SubSectionIds.
    /// </summary>
    [Fact]
    public void VibrationHandsFit_Regression_PreservesEvidence_NoDiseaseInjection()
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
                Phrase = "fear before fit",
                Category = "Mental",
                SearchTerms = { "fear", "fit", "convulsion" },
            },
        };

        var concepts = ConceptGraphConceptMapper.FromSymptoms(symptoms);
        concepts = FastClinicalSymptomBlockBuilder.ExpandMultiQuery(concepts);

        Assert.Contains(concepts, c =>
            c.RawStatement.Contains("vibration", StringComparison.OrdinalIgnoreCase)
            && (c.RawStatement.Contains("hand", StringComparison.OrdinalIgnoreCase)
                || c.SearchTerms.Any(t => t.Contains("hand", StringComparison.OrdinalIgnoreCase))));

        Assert.Contains(concepts, c =>
            c.SearchTerms.Any(t => t.Contains("vibration", StringComparison.OrdinalIgnoreCase)
                                   && t.Contains("hand", StringComparison.OrdinalIgnoreCase))
            || c.RawStatement.Contains("vibration", StringComparison.OrdinalIgnoreCase));

        var fitConcept = concepts.First(c => c.RawStatement.Contains("fit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fitConcept.SearchTerms, t =>
            t.Equals("convulsion", StringComparison.OrdinalIgnoreCase)
            || t.Contains("epilepsy", StringComparison.OrdinalIgnoreCase));

        var candidates = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 101,
                SubSectionName = "EXTREMITIES - VIBRATION - Hands",
                MatchSource = "Keyword",
                MatchedFrom = "vibration in hands",
                ConfidenceScore = 0.9m,
                MatchScore = 90,
            },
            new()
            {
                SubSectionId = 102,
                SubSectionName = "MIND - FEAR - before fit",
                MatchSource = "Keyword",
                MatchedFrom = "fear before fit",
                ConfidenceScore = 0.88m,
                MatchScore = 88,
            },
            new()
            {
                SubSectionId = 103,
                SubSectionName = "GENERALITIES - CONVULSIONS - epileptic",
                MatchSource = "Embedding",
                MatchedFrom = "fit",
                ConfidenceScore = 0.95m,
                MatchScore = 95,
            },
        };

        var gated = RubricCandidateQualityGate.Apply(candidates, concepts);
        gated = FastClinicalEvidenceGate.ApplyHallucinationHardGate(gated, concepts, minEvidenceScore: 0.15m);
        FastClinicalRanking.ApplyCanonicalScores(gated, concepts);
        var selected = FastClinicalRanking.SelectWithMmr(gated, targetCount: 8, minCanonicalScore: 0.35m);
        FastClinicalEvidenceGate.AttachEvidenceContract(selected, concepts);

        Assert.Contains(selected, r =>
            r.SubSectionName.Contains("VIBRATION", StringComparison.OrdinalIgnoreCase)
            && r.SubSectionName.Contains("Hands", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(selected, r => r.SubSectionName.Contains("FEAR", StringComparison.OrdinalIgnoreCase));
        // Epilepsy leaf should lose to evidence-gated ranking when patient never said epilepsy/convulsion
        Assert.All(selected, r => Assert.True(r.IsDbBacked == true));
        Assert.All(selected, r => Assert.False(string.IsNullOrWhiteSpace(r.PatientEvidence)));
    }

    [Fact]
    public void MultiQuery_Adds_SensationLocation_Block()
    {
        var concepts = new List<ClinicalConceptModel>
        {
            new()
            {
                SequenceOrder = 1,
                RawStatement = "vibration sensation in the hands",
                SearchTerms = { "vibration", "hands", "sensation" },
                Confidence = 0.85m,
            },
        };

        var expanded = FastClinicalSymptomBlockBuilder.ExpandMultiQuery(concepts);
        Assert.True(expanded.Count > concepts.Count);
        Assert.Contains(expanded, c =>
            c.RawStatement.Contains("vibration", StringComparison.OrdinalIgnoreCase)
            && c.RawStatement.Contains("hand", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void HierarchyParser_Splits_Path()
    {
        var path = FastClinicalHierarchyParser.Parse("EXTREMITIES - VIBRATION - Hands");
        Assert.Equal(3, path.Depth);
        Assert.Equal("EXTREMITIES", path.Root);
        Assert.Equal("Hands", path.Leaf);
    }
}
