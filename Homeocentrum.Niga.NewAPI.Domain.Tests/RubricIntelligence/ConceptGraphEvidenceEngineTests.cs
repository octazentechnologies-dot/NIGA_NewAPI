using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class ConceptGraphEvidenceEngineTests
{
    [Fact]
    public void ResolveStrictChain_UsesHomeopathicConceptId_NotFuzzyCrossMatch()
    {
        var graph = new ConceptGraphFullModel
        {
            Meanings = new List<PatientMeaningNodeModel>
            {
                new() { PatientMeaningId = 1, RawStatement = "There is a vibration.", NormalizedMeaning = "Vibration aura" },
                new() { PatientMeaningId = 2, RawStatement = "I get scared of my feet.", NormalizedMeaning = "Fear of feet" },
            },
            ClinicalConcepts = new List<ClinicalConceptNodeModel>
            {
                new() { ClinicalConceptId = 10, PatientMeaningId = 1, MeaningIndex = 0, ConceptName = "Vibration sensation" },
                new() { ClinicalConceptId = 20, PatientMeaningId = 2, MeaningIndex = 1, ConceptName = "Fear of feet" },
            },
            HomeopathicConcepts = new List<HomeopathicConceptNodeModel>
            {
                new() { HomeopathicConceptId = 100, ClinicalConceptIndex = 0, ConceptName = "Vibration aura" },
                new() { HomeopathicConceptId = 200, ClinicalConceptIndex = 1, ConceptName = "Fear of feet" },
            },
        };

        var discovery = new RubricDiscoveryNodeModel
        {
            HomeopathicConceptId = 200,
            SubSectionId = 999,
            SubSectionName = "MIND - FEAR - happen, something will",
            MatchReason = "Exact repertory match for 'fear'",
        };

        var (homeo, clinical, meaning) = ConceptGraphEvidenceEngine.ResolveStrictChain(discovery, graph);

        Assert.NotNull(homeo);
        Assert.Equal(200, homeo!.HomeopathicConceptId);
        Assert.Equal("Fear of feet", homeo.ConceptName);
        Assert.NotNull(meaning);
        Assert.Equal("I get scared of my feet.", meaning!.RawStatement);
        Assert.Equal("Fear of feet", meaning.NormalizedMeaning);
        Assert.NotNull(clinical);
        Assert.Equal("Fear of feet", clinical!.ConceptName);
    }

    [Fact]
    public void BuildEvidenceChains_KeepsTranscriptAndMeaningFromSameConcept()
    {
        var engine = new ConceptGraphEvidenceEngine();
        var graph = new ConceptGraphFullModel
        {
            Meanings = new List<PatientMeaningNodeModel>
            {
                new() { PatientMeaningId = 1, RawStatement = "There is a vibration.", NormalizedMeaning = "Vibration aura" },
                new() { PatientMeaningId = 2, RawStatement = "I get scared of my feet.", NormalizedMeaning = "Fear of feet" },
            },
            ClinicalConcepts = new List<ClinicalConceptNodeModel>
            {
                new() { ClinicalConceptId = 10, PatientMeaningId = 1, MeaningIndex = 0, ConceptName = "Vibration sensation" },
                new() { ClinicalConceptId = 20, PatientMeaningId = 2, MeaningIndex = 1, ConceptName = "Fear of feet" },
            },
            HomeopathicConcepts = new List<HomeopathicConceptNodeModel>
            {
                new() { HomeopathicConceptId = 100, ClinicalConceptIndex = 0, ConceptName = "Vibration aura" },
                new() { HomeopathicConceptId = 200, ClinicalConceptIndex = 1, ConceptName = "Fear of feet" },
            },
        };

        var result = engine.BuildEvidenceChains(new[]
        {
            new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = 200,
                SubSectionId = 1,
                SubSectionName = "MIND - FEAR - epilepsy, of",
                Confidence = 0.8m,
            },
            new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = 100,
                SubSectionId = 2,
                SubSectionName = "GENERALS - CONVULSIONS - aura",
                Confidence = 0.75m,
            },
        }, graph);

        Assert.Equal(2, result.Count);
        Assert.Equal("I get scared of my feet.", result[0].EvidenceChain!.TranscriptStatement);
        Assert.Equal("Fear of feet", result[0].EvidenceChain!.PatientMeaning);
        Assert.Equal("There is a vibration.", result[1].EvidenceChain!.TranscriptStatement);
        Assert.Equal("Vibration aura", result[1].EvidenceChain!.PatientMeaning);
    }
}
