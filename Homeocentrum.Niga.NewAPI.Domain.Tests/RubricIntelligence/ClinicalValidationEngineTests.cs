using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation.Enterprise;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class GenderRubricValidatorTests
{
    [Fact]
    public void Validate_MalePatient_MensesRubric_HardReject()
    {
        var rubric = new AudioCaseSuggestedRubricModel { SubSectionName = "MENSES - painful" };
        var patient = new PatientClinicalContext { Gender = 0, AgeYears = 35 };

        var issue = GenderRubricValidator.Validate(rubric, patient);

        Assert.NotNull(issue);
        Assert.Equal("GenderMismatch", issue!.Code);
        Assert.True(issue.IsHardReject);
    }

    [Fact]
    public void Validate_FemalePatient_ProstateRubric_HardReject()
    {
        var rubric = new AudioCaseSuggestedRubricModel { SubSectionName = "PROSTATE - enlarged" };
        var patient = new PatientClinicalContext { Gender = 1, AgeYears = 40 };

        var issue = GenderRubricValidator.Validate(rubric, patient);

        Assert.NotNull(issue);
        Assert.True(issue!.IsHardReject);
    }

    [Fact]
    public void Validate_MalePatient_FearRubric_NoIssue()
    {
        var rubric = new AudioCaseSuggestedRubricModel { SubSectionName = "MIND - FEAR of epilepsy" };
        var patient = new PatientClinicalContext { Gender = 0, AgeYears = 30 };

        var issue = GenderRubricValidator.Validate(rubric, patient);

        Assert.Null(issue);
    }
}

public class RepertoryDomainValidatorTests
{
    [Fact]
    public void Validate_FearConcept_AbdomenRubric_DomainMismatch()
    {
        var rubric = new AudioCaseSuggestedRubricModel { SubSectionName = "ABDOMEN - FALLING sensation" };
        var concept = new ClinicalConceptModel
        {
            RawStatement = "fear that a fit will come",
            ClinicalMeaning = "anticipatory fear of epileptic seizure",
        };
        var evidence = new RubricEvidenceChainModel
        {
            PatientStatements = { concept.RawStatement },
            ClinicalMeanings = { concept.ClinicalMeaning! },
            EvidenceStrength = 0.2m,
        };

        var issue = RepertoryDomainValidator.Validate(rubric, concept, evidence);

        Assert.NotNull(issue);
        Assert.Equal("DomainMismatch", issue!.Code);
    }

    [Fact]
    public void Validate_ShiverConcept_EarRubric_DomainMismatch()
    {
        var rubric = new AudioCaseSuggestedRubricModel { SubSectionName = "EAR - RINGING before fit" };
        var concept = new ClinicalConceptModel
        {
            RawStatement = "shivering before the fit",
            ClinicalMeaning = "premonitory shivering before seizure",
        };
        var evidence = new RubricEvidenceChainModel
        {
            PatientStatements = { concept.RawStatement },
            ClinicalMeanings = { concept.ClinicalMeaning! },
            EvidenceStrength = 0.15m,
        };

        var issue = RepertoryDomainValidator.Validate(rubric, concept, evidence);

        Assert.NotNull(issue);
        Assert.Equal("DomainMismatch", issue!.Code);
    }
}

public class ClinicalValidationEngineTests
{
    [Fact]
    public void ValidateAndFilter_EpilepsyMaleCase_RejectsGenderAndDomainDrift()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new RubricIntelligenceOptions
        {
            EnableClinicalValidationV21 = true,
            EnableEnterpriseClinicalValidation = true,
            MinRubricQualityScore = 70,
            MinEvidenceSimilarityForRubric = 0.38m,
            MinEvidenceSimilarityForInference = 0.50m,
            MinEvidenceStrength = 0.30m,
        });

        var engine = new ClinicalValidationEngine(
            options,
            new PrimarySymptomEngine(),
            new EvidenceChainBuilder(),
            new RubricQualityScoringEngine(),
            new EnterpriseClinicalValidationPipeline(),
            new EnterpriseRubricClinicalAuditLogger(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<EnterpriseRubricClinicalAuditLogger>.Instance),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ClinicalValidationEngine>.Instance);

        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 1,
                SubSectionName = "MIND - FEAR of fit",
                MatchScore = 0.88m,
                ConfidenceScore = 0.88m,
                MatchedFrom = "fear that a fit will come",
            },
            new()
            {
                SubSectionId = 2,
                SubSectionName = "ABDOMEN - FALLING sensation",
                MatchScore = 0.75m,
                ConfidenceScore = 0.75m,
                MatchedFrom = "fear that a fit will come",
                MatchLayer = "Inference",
            },
            new()
            {
                SubSectionId = 3,
                SubSectionName = "MENSES - irregular",
                MatchScore = 0.70m,
                ConfidenceScore = 0.70m,
                MatchedFrom = "general weakness",
            },
        };

        var result = engine.ValidateAndFilter(
            rubrics,
            new ClinicalValidationContext
            {
                Patient = new PatientClinicalContext { Gender = 0, AgeYears = 28 },
                Transcript = "Patient fears epileptic fits. Shivering precedes attacks.",
                Summary = new AudioCaseSummaryModel { ChiefComplaint = "Epilepsy with fear before fit" },
                Concepts = new List<ClinicalConceptModel>
                {
                    new()
                    {
                        ConceptId = Guid.NewGuid(),
                        RawStatement = "fear that a fit will come",
                        ClinicalMeaning = "anticipatory fear of epileptic seizure",
                        Confidence = 0.92m,
                        IsSRP = true,
                    },
                },
                Symptoms = new List<AudioCaseSymptomModel>
                {
                    new() { Phrase = "fear before fit" },
                },
            });

        var rejectionSummary = string.Join("; ",
            result.RejectedRubrics.Select(r =>
                $"{r.SubSectionName} [{string.Join(',', r.ValidationFlags)}] score={r.QualityScore}"));

        Assert.True(result.AcceptedRubrics.Count > 0, $"No rubrics accepted. Rejected: {rejectionSummary}");
        Assert.Contains(result.AcceptedRubrics, r => r.SubSectionName.Contains("FEAR", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.AcceptedRubrics, r => r.SubSectionName.Contains("MENSES", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.AcceptedRubrics, r => r.SubSectionName.Contains("ABDOMEN", StringComparison.OrdinalIgnoreCase));
        Assert.True(result.RejectedCount >= 2);
    }
}
