using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Live <c>SubSectionMaster</c> check — not a cache proxy.
/// Unresolved positive IDs are downgraded to AI Clinical Concept so they cannot render as repertory.
/// </summary>
public static class LiveSubSectionMasterGate
{
    public const string UnresolvedMatchSource = "UnresolvedMaster";

    public static List<AudioCaseSuggestedRubricModel> Apply(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyDictionary<int, string> liveById,
        out int unresolvedCount,
        out int renamedCount)
    {
        unresolvedCount = 0;
        renamedCount = 0;
        var result = new List<AudioCaseSuggestedRubricModel>(rubrics.Count);

        foreach (var rubric in rubrics)
        {
            if (rubric.SubSectionId <= 0)
            {
                rubric.IsDbBacked = false;
                result.Add(rubric);
                continue;
            }

            if (!liveById.TryGetValue(rubric.SubSectionId, out var liveName)
                || string.IsNullOrWhiteSpace(liveName))
            {
                unresolvedCount++;
                result.Add(DowngradeUnresolved(rubric));
                continue;
            }

            if (!string.Equals(rubric.SubSectionName?.Trim(), liveName.Trim(), StringComparison.Ordinal))
            {
                renamedCount++;
                rubric.SubSectionName = liveName.Trim();
            }

            rubric.IsDbBacked = true;
            result.Add(rubric);
        }

        return result;
    }

    public static AudioCaseSuggestedRubricModel DowngradeUnresolved(AudioCaseSuggestedRubricModel rubric)
    {
        var originalId = rubric.SubSectionId;
        var originalName = rubric.SubSectionName;
        rubric.SubSectionId = 0;
        rubric.IsDbBacked = false;
        rubric.IsAiSuggested = true;
        rubric.ResultKind = EnterpriseRubricPresentationHelper.ResultKindAiConcept;
        rubric.MatchSource = UnresolvedMatchSource;
        rubric.MatchLayer = UnresolvedMatchSource;
        rubric.Source = "AiSuggested";
        rubric.RequiresManualApproval = true;
        rubric.RequiresDoctorReview = true;
        rubric.WhySuggested = string.IsNullOrWhiteSpace(rubric.WhySuggested)
            ? $"Not in live SubSectionMaster (was id {originalId})."
            : $"{rubric.WhySuggested} | Not in live SubSectionMaster (was id {originalId}).";
        rubric.SelectionReason = "Not found in repertory. Display under AI Clinical Concepts; do not repertorize as rubric.";
        if (string.IsNullOrWhiteSpace(rubric.SubSectionName))
            rubric.SubSectionName = originalName ?? $"Unresolved rubric {originalId}";
        return rubric;
    }
}
