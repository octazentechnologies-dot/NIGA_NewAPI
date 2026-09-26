using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation.Enterprise;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;

public class ClinicalValidationEngine : IClinicalValidationEngine
{
    private readonly RubricIntelligenceOptions _options;
    private readonly IPrimarySymptomEngine _primarySymptomEngine;
    private readonly IEvidenceChainBuilder _evidenceChainBuilder;
    private readonly IRubricQualityScoringEngine _qualityScoringEngine;
    private readonly IEnterpriseClinicalValidationPipeline _enterprisePipeline;
    private readonly IEnterpriseRubricClinicalAuditLogger _auditLogger;
    private readonly ILogger<ClinicalValidationEngine> _logger;

    public ClinicalValidationEngine(
        IOptions<RubricIntelligenceOptions> options,
        IPrimarySymptomEngine primarySymptomEngine,
        IEvidenceChainBuilder evidenceChainBuilder,
        IRubricQualityScoringEngine qualityScoringEngine,
        IEnterpriseClinicalValidationPipeline enterprisePipeline,
        IEnterpriseRubricClinicalAuditLogger auditLogger,
        ILogger<ClinicalValidationEngine> logger)
    {
        _options = options.Value;
        _primarySymptomEngine = primarySymptomEngine;
        _evidenceChainBuilder = evidenceChainBuilder;
        _qualityScoringEngine = qualityScoringEngine;
        _enterprisePipeline = enterprisePipeline;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public ClinicalValidationResult ValidateAndFilter(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        ClinicalValidationContext context)
    {
        if (!_options.RequiresStrictValidation)
        {
            return new ClinicalValidationResult
            {
                AcceptedRubrics = rubrics.ToList(),
                PrimarySymptom = context.PrimarySymptom
                    ?? _primarySymptomEngine.Resolve(context.Summary, context.Concepts, context.Symptoms),
            };
        }

        return _options.EnableEnterpriseClinicalValidation
            ? ValidateWithEnterprisePipeline(rubrics, context)
            : ValidateLegacy(rubrics, context);
    }

    private ClinicalValidationResult ValidateWithEnterprisePipeline(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        ClinicalValidationContext context)
    {
        var primarySymptom = context.PrimarySymptom
            ?? _primarySymptomEngine.Resolve(context.Summary, context.Concepts, context.Symptoms);

        var accepted = new List<AudioCaseSuggestedRubricModel>();
        var rejected = new List<AudioCaseSuggestedRubricModel>();
        var reports = new List<RubricEnterpriseValidationReport>();

        foreach (var rubric in rubrics)
        {
            var validated = ValidateWithEnterprisePipelineRubric(rubric, context, primarySymptom);
            reports.Add(validated.EnterpriseValidation!);

            if (string.Equals(validated.ValidationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                rejected.Add(validated);
                if (_options.EnableRubricValidationAuditLogging)
                {
                    _auditLogger.LogValidationDecision(
                        context.SessionId,
                        validated,
                        validated.EnterpriseValidation,
                        "Rejected",
                        validated.ValidationFlags?.FirstOrDefault() ?? "ValidationFailed",
                        _options);
                }
            }
            else
            {
                accepted.Add(validated);
                if (_options.EnableRubricValidationAuditLogging)
                {
                    _auditLogger.LogValidationDecision(
                        context.SessionId,
                        validated,
                        validated.EnterpriseValidation,
                        "Accepted",
                        "PassedAllSteps",
                        _options);
                }
            }
        }

        _enterprisePipeline.ApplyBatchDuplicateDetection(accepted, rejected);

        accepted = accepted
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .ThenByDescending(r => r.IsPrimarySymptomLinked == true)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation(
            "Enterprise clinical validation: accepted={Accepted}, rejected={Rejected}, primary='{Primary}', reports={ReportCount}",
            accepted.Count,
            rejected.Count,
            primarySymptom.Text,
            reports.Count);

        return new ClinicalValidationResult
        {
            AcceptedRubrics = accepted,
            RejectedRubrics = rejected,
            PrimarySymptom = primarySymptom,
            UsedEnterprisePipeline = true,
            ValidationReports = reports,
        };
    }

    private AudioCaseSuggestedRubricModel ValidateWithEnterprisePipelineRubric(
        AudioCaseSuggestedRubricModel rubric,
        ClinicalValidationContext context,
        PrimarySymptomModel primarySymptom)
    {
        var linkedConcept = _evidenceChainBuilder.FindLinkedConcept(rubric, context.Concepts);
        var evidenceChain = _evidenceChainBuilder.Build(
            rubric, context.Concepts, context.Symptoms, context.Transcript);

        var stepContext = new RubricValidationStepContext
        {
            Rubric = rubric,
            Context = context,
            LinkedConcept = linkedConcept,
            EvidenceChain = evidenceChain,
            PrimarySymptom = primarySymptom,
            Options = _options,
        };

        var report = _enterprisePipeline.ValidateRubric(stepContext);
        var isPrimaryLinked = _primarySymptomEngine.IsLinkedToPrimary(rubric, linkedConcept, primarySymptom);
        var quality = _qualityScoringEngine.Score(rubric, report.Issues, isPrimaryLinked, _options);
        var passed = report.PassedAllSteps;
        var rejectionReason = passed
            ? null
            : string.Join("; ", report.Issues.Where(i => i.IsHardReject)
                .Select(i => $"{i.Code}: {i.Message}"));

        return new AudioCaseSuggestedRubricModel
        {
            SubSectionId = rubric.SubSectionId,
            SubSectionName = rubric.SubSectionName,
            SectionId = rubric.SectionId,
            MatchScore = rubric.MatchScore,
            SuggestedIntensityNo = rubric.SuggestedIntensityNo,
            MatchedFrom = rubric.MatchedFrom,
            RemedyCountForSort = rubric.RemedyCountForSort,
            IsAiSuggested = rubric.IsAiSuggested,
            MatchSource = rubric.MatchSource,
            ConfidenceScore = rubric.ConfidenceScore,
            WhySuggested = rubric.WhySuggested,
            EngineVersion = rubric.EngineVersion,
            RequiresManualApproval = rubric.RequiresManualApproval,
            HomeopathicWeight = rubric.HomeopathicWeight,
            MatchLayer = rubric.MatchLayer,
            RubricTier = rubric.RubricTier,
            RequiresDoctorReview = !passed || rubric.RequiresDoctorReview,
            SourceConceptId = rubric.SourceConceptId ?? linkedConcept?.ConceptId,
            InferenceReason = rubric.InferenceReason,
            Explainability = rubric.Explainability,
            RepertorySources = rubric.RepertorySources,
            PrimaryRepertorySource = rubric.PrimaryRepertorySource,
            EvidenceChain = evidenceChain,
            QualityScore = quality.QualityScore,
            ValidationStatus = passed ? "Accepted" : "Rejected",
            ValidationFlags = report.Issues.Select(i => i.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            RejectionReason = rejectionReason,
            IsPrimarySymptomLinked = isPrimaryLinked,
            EnterpriseValidation = report,
            ResultKind = rubric.ResultKind,
            RepertoryPath = rubric.RepertoryPath,
            EnterpriseConfidenceScore = rubric.EnterpriseConfidenceScore,
            SelectionReason = rubric.SelectionReason ?? rubric.WhySuggested,
        };
    }

    private ClinicalValidationResult ValidateLegacy(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        ClinicalValidationContext context)
    {
        var primarySymptom = context.PrimarySymptom
            ?? _primarySymptomEngine.Resolve(context.Summary, context.Concepts, context.Symptoms);

        var accepted = new List<AudioCaseSuggestedRubricModel>();
        var rejected = new List<AudioCaseSuggestedRubricModel>();

        foreach (var rubric in rubrics)
        {
            var validated = ValidateLegacyRubric(rubric, context, primarySymptom);
            if (string.Equals(validated.ValidationStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
                rejected.Add(validated);
            else
                accepted.Add(validated);
        }

        accepted = accepted
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .ThenByDescending(r => r.IsPrimarySymptomLinked == true)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _logger.LogInformation(
            "Clinical validation V2.1: accepted={Accepted}, rejected={Rejected}, primary='{Primary}'",
            accepted.Count,
            rejected.Count,
            primarySymptom.Text);

        return new ClinicalValidationResult
        {
            AcceptedRubrics = accepted,
            RejectedRubrics = rejected,
            PrimarySymptom = primarySymptom,
        };
    }

    private AudioCaseSuggestedRubricModel ValidateLegacyRubric(
        AudioCaseSuggestedRubricModel rubric,
        ClinicalValidationContext context,
        PrimarySymptomModel primarySymptom)
    {
        var linkedConcept = _evidenceChainBuilder.FindLinkedConcept(rubric, context.Concepts);
        var evidenceChain = _evidenceChainBuilder.Build(
            rubric, context.Concepts, context.Symptoms, context.Transcript);

        var issues = new List<RubricValidationIssueModel>();

        AddIssue(issues, GenderRubricValidator.Validate(rubric, context.Patient));
        AddIssue(issues, AgeRubricValidator.Validate(rubric, context.Patient));
        AddIssue(issues, RepertoryDomainValidator.Validate(rubric, linkedConcept, evidenceChain));
        AddIssue(issues, RubricHallucinationDetector.Detect(rubric, evidenceChain, _options));
        AddIssue(issues, HomeopathicRulesEngine.Validate(rubric, linkedConcept, primarySymptom));

        var isPrimaryLinked = _primarySymptomEngine.IsLinkedToPrimary(rubric, linkedConcept, primarySymptom);
        var validation = _qualityScoringEngine.Score(rubric, issues, isPrimaryLinked, _options);

        var flags = validation.Issues.Select(i => i.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var status = validation.IsAccepted ? "Accepted" : "Rejected";

        return new AudioCaseSuggestedRubricModel
        {
            SubSectionId = rubric.SubSectionId,
            SubSectionName = rubric.SubSectionName,
            SectionId = rubric.SectionId,
            MatchScore = rubric.MatchScore,
            SuggestedIntensityNo = rubric.SuggestedIntensityNo,
            MatchedFrom = rubric.MatchedFrom,
            RemedyCountForSort = rubric.RemedyCountForSort,
            IsAiSuggested = rubric.IsAiSuggested,
            MatchSource = rubric.MatchSource,
            ConfidenceScore = rubric.ConfidenceScore,
            WhySuggested = rubric.WhySuggested,
            EngineVersion = rubric.EngineVersion,
            RequiresManualApproval = rubric.RequiresManualApproval,
            HomeopathicWeight = rubric.HomeopathicWeight,
            MatchLayer = rubric.MatchLayer,
            RubricTier = rubric.RubricTier,
            RequiresDoctorReview = rubric.RequiresDoctorReview || !validation.IsAccepted,
            SourceConceptId = rubric.SourceConceptId ?? linkedConcept?.ConceptId,
            InferenceReason = rubric.InferenceReason,
            Explainability = rubric.Explainability,
            RepertorySources = rubric.RepertorySources,
            PrimaryRepertorySource = rubric.PrimaryRepertorySource,
            EvidenceChain = evidenceChain,
            QualityScore = validation.QualityScore,
            ValidationStatus = status,
            ValidationFlags = flags,
            IsPrimarySymptomLinked = isPrimaryLinked,
        };
    }

    private static void AddIssue(List<RubricValidationIssueModel> issues, RubricValidationIssueModel? issue)
    {
        if (issue != null) issues.Add(issue);
    }
}
