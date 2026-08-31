using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Validation.Enterprise;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class EnterpriseClinicalValidationPipelineTests
{
    [Fact]
    public void ValidateRubric_RunsAllSevenPerRubricSteps()
    {
        var pipeline = new EnterpriseClinicalValidationPipeline();
        var context = BuildValidContext();

        var report = pipeline.ValidateRubric(context);

        Assert.Equal(7, report.Steps.Count);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Evidence);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Clinical);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Gender);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Age);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Domain);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Hallucination);
        Assert.Contains(report.Steps, s => s.StepName == EnterpriseValidationStepNames.Confidence);
    }

    [Fact]
    public void ValidateRubric_ValidFearRubric_PassesAllSteps()
    {
        var pipeline = new EnterpriseClinicalValidationPipeline();
        var context = BuildValidContext();

        var report = pipeline.ValidateRubric(context);

        Assert.True(report.PassedAllSteps);
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void ValidateRubric_MalePatientMensesRubric_FailsGenderStep()
    {
        var pipeline = new EnterpriseClinicalValidationPipeline();
        var context = BuildValidContext();
        context.Rubric.SubSectionName = "MENSES - painful";
        context.Context.Patient = new PatientClinicalContext { Gender = 0, AgeYears = 30 };

        var report = pipeline.ValidateRubric(context);

        Assert.False(report.PassedAllSteps);
        Assert.Contains(report.Steps, s =>
            s.StepName == EnterpriseValidationStepNames.Gender && !s.Passed);
    }

    [Fact]
    public void ApplyBatchDuplicateDetection_RejectsDuplicateSubSection()
    {
        var pipeline = new EnterpriseClinicalValidationPipeline();
        var accepted = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 10, SubSectionName = "MIND - FEAR", MatchScore = 0.90m, EnterpriseValidation = new RubricEnterpriseValidationReport { Steps = new List<ValidationStepResult>() } },
            new() { SubSectionId = 10, SubSectionName = "MIND - FEAR", MatchScore = 0.80m, EnterpriseValidation = new RubricEnterpriseValidationReport { Steps = new List<ValidationStepResult>() } },
        };
        var rejected = new List<AudioCaseSuggestedRubricModel>();

        pipeline.ApplyBatchDuplicateDetection(accepted, rejected);

        Assert.Single(accepted);
        Assert.Single(rejected);
        Assert.Contains(rejected[0].ValidationFlags, f => f == "DuplicateRubric");
    }

    private static RubricValidationStepContext BuildValidContext() =>
        new()
        {
            Rubric = new AudioCaseSuggestedRubricModel
            {
                SubSectionId = 1,
                SubSectionName = "MIND - FEAR of fit",
                MatchScore = 0.88m,
                ConfidenceScore = 0.88m,
                MatchedFrom = "fear that a fit will come",
            },
            Context = new ClinicalValidationContext
            {
                Patient = new PatientClinicalContext { Gender = 0, AgeYears = 28 },
                Transcript = "Patient fears epileptic fits before attacks.",
            },
            LinkedConcept = new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "fear that a fit will come",
                ClinicalMeaning = "anticipatory fear of epileptic seizure",
                Confidence = 0.92m,
                IsSRP = true,
            },
            EvidenceChain = new RubricEvidenceChainModel
            {
                PatientStatements = { "fear that a fit will come" },
                ClinicalMeanings = { "anticipatory fear of epileptic seizure" },
                EvidenceStrength = 0.75m,
                TranscriptExcerpt = "Patient fears epileptic fits",
            },
            PrimarySymptom = new PrimarySymptomModel { Text = "fear before fit", Confidence = 0.95m },
            Options = new RubricIntelligenceOptions
            {
                MinEvidenceStrength = 0.30m,
                MinEvidenceSimilarityForRubric = 0.38m,
                MinConceptConfidenceForInference = 0.85m,
            },
        };
}
