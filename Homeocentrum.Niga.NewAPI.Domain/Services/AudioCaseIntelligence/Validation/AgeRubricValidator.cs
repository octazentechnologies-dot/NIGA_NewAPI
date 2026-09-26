using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;

public static class AgeRubricValidator
{
    public static RubricValidationIssueModel? Validate(
        AudioCaseSuggestedRubricModel rubric,
        PatientClinicalContext? patient)
    {
        if (patient?.AgeYears is not > 0) return null;

        var rubricUpper = rubric.SubSectionName.ToUpperInvariant();
        var age = patient.AgeYears.Value;

        if (age >= 13 && rubricUpper.Contains("CHILDREN", StringComparison.Ordinal))
        {
            return new RubricValidationIssueModel
            {
                Code = "AgeMismatch",
                Message = $"Pediatric rubric suggested for adult patient (age {age}).",
                PenaltyPoints = 35,
                IsHardReject = false,
            };
        }

        if (age < 10 && rubricUpper.Contains("MENOPAUSE", StringComparison.Ordinal))
        {
            return new RubricValidationIssueModel
            {
                Code = "AgeMismatch",
                Message = $"Menopause rubric inappropriate for age {age}.",
                PenaltyPoints = 100,
                IsHardReject = true,
            };
        }

        if (patient.Gender == 1 && age < 12
            && (rubricUpper.Contains("MENSES", StringComparison.Ordinal)
                || rubricUpper.Contains("PREGNAN", StringComparison.Ordinal)))
        {
            return new RubricValidationIssueModel
            {
                Code = "AgeMismatch",
                Message = $"Female reproductive rubric inappropriate for age {age}.",
                PenaltyPoints = 100,
                IsHardReject = true,
            };
        }

        return null;
    }
}
