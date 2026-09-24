using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation.Enterprise;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class EnterpriseRubricEvidenceChainBuilderTests
{
    [Fact]
    public void Build_ProducesFullNineStepChain()
    {
        var builder = new EnterpriseRubricEvidenceChainBuilder();
        var graph = BuildSampleGraph();
        var rubric = new AudioCaseSuggestedRubricModel
        {
            SubSectionId = 101,
            SubSectionName = "MIND - FEAR of fit",
            MatchScore = 0.88m,
            ConfidenceScore = 0.88m,
            MatchedFrom = "fear before the fit",
            ValidationStatus = "Accepted",
            EngineVersion = "v8",
            EnterpriseValidation = new RubricEnterpriseValidationReport
            {
                SubSectionId = 101,
                SubSectionName = "MIND - FEAR of fit",
                PassedAllSteps = true,
            },
        };

        var chain = builder.Build(rubric, new RubricEvidenceChainEnrichmentContext
        {
            Transcript = "Patient says fear before the fit and shivering.",
            Graph = graph,
            Candidates =
            [
                new RubricCandidateModel
                {
                    SubSectionId = 101,
                    SubSectionName = "MIND - FEAR of fit",
                    SourceConceptName = "Fear Before Convulsion",
                    SimilarityScore = 0.91m,
                    ClinicalRelevanceScore = 0.85m,
                    EvidenceScore = 0.80m,
                    DoctorAcceptanceScore = 0.72m,
                    CompositeScore = 0.86m,
                    MatchMethod = "SemanticRubricEmbedding",
                    MappedFromConceptKey = "Fear Before Convulsion",
                },
            ],
        });

        Assert.True(chain.Steps.Count >= 9);
        Assert.Equal(RubricEvidenceChainStepKeys.Transcript, chain.Steps[0].StepKey);
        Assert.Equal(RubricEvidenceChainStepKeys.Meaning, chain.Steps[1].StepKey);
        Assert.Equal(RubricEvidenceChainStepKeys.ClinicalConcept, chain.Steps[2].StepKey);
        Assert.Equal(RubricEvidenceChainStepKeys.HomeopathicConcept, chain.Steps[3].StepKey);
        Assert.Equal(RubricEvidenceChainStepKeys.EmbeddingMatch, chain.Steps[4].StepKey);
        Assert.Equal(RubricEvidenceChainStepKeys.Rubric, chain.Steps[5].StepKey);

        var stepKeys = chain.Steps.Select(s => s.StepKey).ToList();
        Assert.Contains(RubricEvidenceChainStepKeys.Confidence, stepKeys);
        Assert.Contains(RubricEvidenceChainStepKeys.ValidationResult, stepKeys);
        Assert.Contains(RubricEvidenceChainStepKeys.DoctorFeedback, stepKeys);
        Assert.Contains("Transcript", chain.DisplayChain);
        Assert.Contains("Doctor Feedback", chain.DisplayChain);
        Assert.NotNull(chain.GetStep(RubricEvidenceChainStepKeys.HomeopathicConcept)?.Value);
    }

    private static ConceptGraphFullModel BuildSampleGraph() =>
        new()
        {
            EngineVersion = "v8",
            Meanings =
            [
                new PatientMeaningNodeModel
                {
                    RawStatement = "fear before the fit",
                    NormalizedMeaning = "anticipatory fear before epileptic fit",
                    Confidence = 0.92m,
                },
            ],
            ClinicalConcepts =
            [
                new ClinicalConceptNodeModel
                {
                    MeaningIndex = 0,
                    ConceptName = "Anticipatory fear before seizure",
                    Confidence = 0.90m,
                },
            ],
            HomeopathicConcepts =
            [
                new HomeopathicConceptNodeModel
                {
                    ClinicalConceptIndex = 0,
                    ConceptName = "Fear Before Convulsion",
                    Category = "Fear",
                    Confidence = 0.88m,
                },
            ],
        };
}
