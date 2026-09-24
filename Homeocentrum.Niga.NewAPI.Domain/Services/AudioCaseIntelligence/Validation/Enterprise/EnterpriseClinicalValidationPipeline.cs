using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation.Enterprise;

public class EnterpriseClinicalValidationPipeline : IEnterpriseClinicalValidationPipeline
{
    private readonly IReadOnlyList<IRubricValidationStep> _perRubricSteps;

    public EnterpriseClinicalValidationPipeline()
    {
        _perRubricSteps =
        [
            new EvidenceValidationStep(),
            new ClinicalConceptValidationStep(),
            new GenderValidationStep(),
            new AgeValidationStep(),
            new DomainValidationStep(),
            new HallucinationDetectionStep(),
            new ConfidenceValidationStep(),
        ];
    }

    public RubricEnterpriseValidationReport ValidateRubric(RubricValidationStepContext context)
    {
        var report = new RubricEnterpriseValidationReport
        {
            SubSectionId = context.Rubric.SubSectionId,
            SubSectionName = context.Rubric.SubSectionName,
        };

        foreach (var step in _perRubricSteps)
        {
            var issue = step.Validate(context);
            // Soft issues are recorded but do not fail the step / overall acceptance.
            var hardFail = issue is { IsHardReject: true };
            report.Steps.Add(new ValidationStepResult
            {
                StepName = step.StepName,
                Passed = !hardFail,
                Issue = issue,
            });

            if (issue != null)
                report.Issues.Add(issue);
        }

        report.PassedAllSteps = !report.Issues.Any(i => i.IsHardReject);
        return report;
    }

    public void ApplyBatchDuplicateDetection(
        IList<AudioCaseSuggestedRubricModel> accepted,
        IList<AudioCaseSuggestedRubricModel> rejected)
    {
        var kept = new List<AudioCaseSuggestedRubricModel>();
        var acceptedIds = new HashSet<int>();
        var acceptedTails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rubric in accepted.OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore))
        {
            var duplicateIssue = RubricDuplicateDetector.DetectWithinBatch(rubric, acceptedIds, acceptedTails);
            if (duplicateIssue != null)
            {
                rubric.ValidationStatus = "Rejected";
                rubric.RequiresDoctorReview = true;
                rubric.ValidationFlags = rubric.ValidationFlags
                    .Append(duplicateIssue.Code)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var report = rubric.EnterpriseValidation ?? new RubricEnterpriseValidationReport
                {
                    SubSectionId = rubric.SubSectionId,
                    SubSectionName = rubric.SubSectionName,
                    Steps = new List<ValidationStepResult>(),
                };

                report.Steps.Add(new ValidationStepResult
                {
                    StepName = EnterpriseValidationStepNames.Duplicate,
                    Passed = false,
                    Issue = duplicateIssue,
                });
                report.Issues.Add(duplicateIssue);
                report.PassedAllSteps = false;
                rubric.EnterpriseValidation = report;
                rejected.Add(rubric);
                continue;
            }

            acceptedIds.Add(rubric.SubSectionId);
            var tail = RubricDuplicateDetector.NormalizeTail(rubric.SubSectionName);
            if (!string.IsNullOrWhiteSpace(tail))
                acceptedTails.Add(tail);

            var dupStep = new ValidationStepResult
            {
                StepName = EnterpriseValidationStepNames.Duplicate,
                Passed = true,
            };
            rubric.EnterpriseValidation?.Steps.Add(dupStep);
            if (rubric.EnterpriseValidation != null && !rubric.EnterpriseValidation.Steps.Any(s =>
                    s.StepName == EnterpriseValidationStepNames.Duplicate))
            {
                rubric.EnterpriseValidation.Steps.Add(dupStep);
            }

            kept.Add(rubric);
        }

        accepted.Clear();
        foreach (var rubric in kept)
            accepted.Add(rubric);
    }
}
