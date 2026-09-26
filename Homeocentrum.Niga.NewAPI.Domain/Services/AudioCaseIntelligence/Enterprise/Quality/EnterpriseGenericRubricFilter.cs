using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>Phase 9: suppress generic single-word rubrics unless strongly evidenced.</summary>
public static class EnterpriseGenericRubricFilter
{
    private static readonly HashSet<string> GenericTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "fear", "pain", "heat", "cold", "epilepsy", "convulsions", "anxiety", "anger",
        "sadness", "grief", "thirst", "hunger", "weakness", "fatigue", "sleep",
    };

    public static bool IsGenericRubric(string? rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName))
        {
            return true;
        }

        var tail = ExtractTail(rubricName).Trim();
        if (tail.Contains(' ') || tail.Contains(';') || tail.Contains(','))
        {
            return false;
        }

        return GenericTerms.Contains(tail);
    }

    public static bool HasStrongEvidence(AudioCaseSuggestedRubricModel rubric)
    {
        var evidence = rubric.EvidenceChain?.EvidenceStrength ?? 0m;
        var confidence = rubric.ConfidenceScore ?? rubric.MatchScore;
        var isRepertory = rubric.SubSectionId > 0
            && !string.Equals(rubric.ResultKind, "AiClinicalConcept", StringComparison.OrdinalIgnoreCase);

        if (isRepertory && string.Equals(rubric.MatchSource, RubricDiscoverySources.RepertoryDb, StringComparison.OrdinalIgnoreCase))
        {
            return confidence >= 0.65m || evidence >= 0.40m;
        }

        return evidence >= 0.55m && confidence >= 0.75m;
    }

    public static bool ShouldSuppress(AudioCaseSuggestedRubricModel rubric) =>
        IsGenericRubric(rubric.SubSectionName) && !HasStrongEvidence(rubric);

    private static string ExtractTail(string rubricName)
    {
        var dash = rubricName.LastIndexOf('-');
        return dash > 0 && dash < rubricName.Length - 1
            ? rubricName[(dash + 1)..].Trim()
            : rubricName.Trim();
    }
}
