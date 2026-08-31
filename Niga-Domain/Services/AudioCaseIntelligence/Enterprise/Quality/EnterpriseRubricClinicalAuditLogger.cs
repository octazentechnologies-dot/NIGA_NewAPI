using Microsoft.Extensions.Logging;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Validation.Enterprise;

namespace Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>Phase 1: structured audit log for every clinical validation decision.</summary>
public interface IEnterpriseRubricClinicalAuditLogger
{
    void LogValidationDecision(
        Guid? sessionId,
        AudioCaseSuggestedRubricModel candidate,
        RubricEnterpriseValidationReport? report,
        string decision,
        string reason,
        RubricIntelligenceOptions options);
}

public class EnterpriseRubricClinicalAuditLogger : IEnterpriseRubricClinicalAuditLogger
{
    private readonly ILogger<EnterpriseRubricClinicalAuditLogger> _logger;

    public EnterpriseRubricClinicalAuditLogger(ILogger<EnterpriseRubricClinicalAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogValidationDecision(
        Guid? sessionId,
        AudioCaseSuggestedRubricModel candidate,
        RubricEnterpriseValidationReport? report,
        string decision,
        string reason,
        RubricIntelligenceOptions options)
    {
        var similarity = candidate.ConfidenceScore ?? candidate.MatchScore;
        var evidenceStrength = candidate.EvidenceChain?.EvidenceStrength;
        var transcript = candidate.EvidenceChain?.TranscriptExcerpt
            ?? candidate.MatchedFrom
            ?? string.Empty;

        foreach (var step in report?.Steps ?? [])
        {
            if (step.Passed)
            {
                continue;
            }

            _logger.LogWarning(
                "RubricValidationAudit Session={SessionId} Decision={Decision} Candidate={SubSectionId}|{Name} " +
                "Similarity={Similarity:0.000} EvidenceStrength={Evidence:0.000} Step={Step} Rule={Rule} " +
                "Threshold={Threshold} Reason={Reason} MatchSource={MatchSource} ResultKind={ResultKind}",
                sessionId,
                decision,
                candidate.SubSectionId,
                candidate.SubSectionName,
                similarity,
                evidenceStrength ?? 0m,
                step.StepName,
                step.Issue?.Code ?? "Unknown",
                DescribeThreshold(step.StepName, options),
                step.Issue?.Message ?? reason,
                candidate.MatchSource,
                candidate.ResultKind);
        }

        if (report == null || report.Steps.All(s => s.Passed))
        {
            _logger.LogInformation(
                "RubricValidationAudit Session={SessionId} Decision={Decision} Candidate={SubSectionId}|{Name} " +
                "Similarity={Similarity:0.000} EvidenceStrength={Evidence:0.000} Transcript={Transcript} " +
                "Reason={Reason} MatchSource={MatchSource}",
                sessionId,
                decision,
                candidate.SubSectionId,
                candidate.SubSectionName,
                similarity,
                evidenceStrength ?? 0m,
                Truncate(transcript, 120),
                reason,
                candidate.MatchSource);
        }
    }

    private static string DescribeThreshold(string stepName, RubricIntelligenceOptions options) =>
        stepName switch
        {
            EnterpriseValidationStepNames.Evidence => $"MinEvidenceStrength={options.MinEvidenceStrength:0.00}",
            EnterpriseValidationStepNames.Confidence => "MinConfidence=0.60",
            EnterpriseValidationStepNames.Domain => "MinTailSimilarity=0.42",
            EnterpriseValidationStepNames.Clinical => "MinConceptConfidence=0.50",
            _ => $"MinQuality={options.MinRubricQualityScore}",
        };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
