using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;

public static class GenderRubricValidator
{
    private static readonly string[] FemaleOnlyTokens =
    {
        "MENSES", "MENSTRU", "PREGNAN", "LABOR", "LABOUR", "DELIVERY", "OVAR", "UTER",
        "VAGIN", "LOCHIA", "MISCARRI", "CONTRACEP", "MENOPAUSE", "LEUCORR", "LACTATION",
        "BREAST - MILK", "BREAST - NURSING",
    };

    private static readonly string[] MaleOnlyTokens =
    {
        "PROSTATE", "TESTES", "TESTIC", "SCROTUM", "SEMINAL",
    };

    public static RubricValidationIssueModel? Validate(
        AudioCaseSuggestedRubricModel rubric,
        PatientClinicalContext? patient)
    {
        if (patient?.Gender is not (0 or 1)) return null;

        var rubricUpper = rubric.SubSectionName.ToUpperInvariant();

        if (patient.Gender == 0)
        {
            foreach (var token in FemaleOnlyTokens)
            {
                if (rubricUpper.Contains(token, StringComparison.Ordinal))
                {
                    return new RubricValidationIssueModel
                    {
                        Code = "GenderMismatch",
                        Message = $"Rubric '{rubric.SubSectionName}' is female-specific but patient is male.",
                        PenaltyPoints = 100,
                        IsHardReject = true,
                    };
                }
            }
        }

        if (patient.Gender == 1)
        {
            foreach (var token in MaleOnlyTokens)
            {
                if (rubricUpper.Contains(token, StringComparison.Ordinal))
                {
                    return new RubricValidationIssueModel
                    {
                        Code = "GenderMismatch",
                        Message = $"Rubric '{rubric.SubSectionName}' is male-specific but patient is female.",
                        PenaltyPoints = 100,
                        IsHardReject = true,
                    };
                }
            }
        }

        return null;
    }
}
