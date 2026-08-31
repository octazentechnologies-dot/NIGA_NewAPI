using System.Text.RegularExpressions;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Groups modality sub-variants (e.g. mutton / mutton-agg. / mutton-amel.) under one primary display row.
/// Does not remove variants from the candidate set — only collapses top-level API/UI ranking slots.
/// </summary>
public static class RubricModalityVariantGrouper
{
    private static readonly Regex ModalitySuffixPattern = new(
        @"[-\s](agg\.?|amel\.?|worse|better|before|after|during)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static List<AudioCaseSuggestedRubricModel> GroupForDisplay(IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics)
    {
        if (rubrics.Count <= 1)
            return rubrics.ToList();

        var groups = new Dictionary<string, List<AudioCaseSuggestedRubricModel>>(StringComparer.OrdinalIgnoreCase);
        foreach (var rubric in rubrics)
        {
            var familyKey = GetFamilyKey(rubric.SubSectionName);
            if (!groups.TryGetValue(familyKey, out var list))
            {
                list = new List<AudioCaseSuggestedRubricModel>();
                groups[familyKey] = list;
            }

            list.Add(rubric);
        }

        var output = new List<AudioCaseSuggestedRubricModel>();
        foreach (var group in groups.Values)
        {
            if (group.Count == 1)
            {
                output.Add(group[0]);
                continue;
            }

            var ordered = group
                .OrderByDescending(Score)
                .ThenBy(r => ModalitySuffixPattern.IsMatch(r.SubSectionName ?? string.Empty) ? 1 : 0)
                .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var primary = ordered[0];
            var variants = ordered.Skip(1).Select(ToVariant).ToList();
            if (variants.Count > 0)
            {
                primary.ModalityVariants = variants;
                primary.ModalityVariantCount = variants.Count;
            }

            output.Add(primary);
        }

        return output
            .OrderByDescending(Score)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static RubricModalityVariantModel ToVariant(AudioCaseSuggestedRubricModel rubric) =>
        new()
        {
            SubSectionId = rubric.SubSectionId,
            SubSectionName = rubric.SubSectionName,
            ModalityLabel = ExtractModalityLabel(rubric.SubSectionName),
            MatchScore = rubric.ConfidenceScore ?? rubric.MatchScore,
            MatchSource = rubric.MatchSource,
        };

    private static string GetFamilyKey(string? rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName))
            return string.Empty;

        var normalized = rubricName.Trim();
        var withoutModality = ModalitySuffixPattern.Replace(normalized, string.Empty).TrimEnd('-', ' ', '.');
        return withoutModality.ToUpperInvariant();
    }

    private static string? ExtractModalityLabel(string? rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName))
            return null;

        var match = ModalitySuffixPattern.Match(rubricName);
        return match.Success ? match.Groups[1].Value.Trim('.') : null;
    }

    private static decimal Score(AudioCaseSuggestedRubricModel rubric) =>
        rubric.Scores?.FinalHybridScore ?? rubric.ConfidenceScore ?? rubric.MatchScore;
}
