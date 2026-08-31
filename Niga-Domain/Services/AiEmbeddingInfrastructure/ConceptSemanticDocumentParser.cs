using System.Text;
using System.Text.RegularExpressions;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AiEmbeddingInfrastructure;

public static partial class ConceptSemanticDocumentParser
{
    /// <summary>Matches dbo.AIConceptEmbedding.SourceText NVARCHAR(2000).</summary>
    public const int MaxSourceTextLength = 2000;

    private const int MaxLinkedRubricIdsInDocument = 100;
    public static ParsedConceptSemanticDocument Parse(string? sourceText)
    {
        var result = new ParsedConceptSemanticDocument();
        if (string.IsNullOrWhiteSpace(sourceText))
            return result;

        foreach (var rawLine in sourceText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var line = rawLine.Trim();
            if (TryReadLabel(line, "Clinical Concept", out var clinical))
            {
                result.ClinicalConcept ??= clinical;
                continue;
            }

            if (TryReadLabel(line, "Homeopathic Concept", out var homeopathic))
            {
                result.HomeopathicConcept ??= homeopathic;
                continue;
            }

            if (TryReadLabel(line, "Known Synonyms", out var synonyms))
            {
                result.KnownSynonyms.AddRange(SplitValues(synonyms));
                continue;
            }

            if (TryReadLabel(line, "Meaning", out var meaning))
            {
                result.Meanings.AddRange(SplitValues(meaning));
                continue;
            }

            if (TryReadLabel(line, "Linked Rubrics", out var linked))
            {
                result.LinkedRubricIds.AddRange(ParseRubricIds(linked));
            }
        }

        result.KnownSynonyms = Distinct(result.KnownSynonyms);
        result.Meanings = Distinct(result.Meanings);
        result.LinkedRubricIds = result.LinkedRubricIds.Distinct().ToList();
        return result;
    }

    public static string Compose(
        string clinicalConcept,
        string? homeopathicConcept,
        IEnumerable<string>? synonyms,
        IEnumerable<string>? meanings,
        IEnumerable<int>? linkedRubricIds)
    {
        var sb = new StringBuilder(512);
        AppendLine(sb, "Clinical Concept", clinicalConcept);
        AppendLine(sb, "Homeopathic Concept", homeopathicConcept);
        AppendJoined(sb, "Known Synonyms", synonyms);
        AppendJoined(sb, "Meaning", meanings);

        var rubricIds = linkedRubricIds?.Distinct().ToList();
        if (rubricIds is { Count: > 0 })
        {
            if (rubricIds.Count <= MaxLinkedRubricIdsInDocument)
            {
                sb.Append("Linked Rubrics: ").AppendLine(string.Join("; ", rubricIds));
            }
            else
            {
                var shown = rubricIds.Take(MaxLinkedRubricIdsInDocument);
                var remaining = rubricIds.Count - MaxLinkedRubricIdsInDocument;
                sb.Append("Linked Rubrics: ")
                    .Append(string.Join("; ", shown))
                    .Append("; ... and ")
                    .Append(remaining)
                    .AppendLine(" more rubrics");
            }
        }

        var text = sb.ToString().Trim();
        if (text.Length <= MaxSourceTextLength)
            return text;

        return text[..MaxSourceTextLength];
    }

    private static bool TryReadLabel(string line, string label, out string value)
    {
        value = string.Empty;
        if (!line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            return false;

        value = line[label.Length..].TrimStart(':', ' ').Trim();
        return !string.IsNullOrWhiteSpace(value);
    }

    private static IEnumerable<string> SplitValues(string value) =>
        value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static IEnumerable<int> ParseRubricIds(string value)
    {
        foreach (Match match in RubricIdRegex().Matches(value))
        {
            if (int.TryParse(match.Value, out var rubricId))
                yield return rubricId;
        }
    }

    private static void AppendLine(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        sb.Append(label).Append(": ").AppendLine(value.Trim());
    }

    private static void AppendJoined(StringBuilder sb, string label, IEnumerable<string>? values)
    {
        var list = values?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (list is not { Count: > 0 })
            return;
        sb.Append(label).Append(": ").AppendLine(string.Join("; ", list));
    }

    private static List<string> Distinct(IEnumerable<string> values) =>
        values.Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    [GeneratedRegex(@"\b\d+\b")]
    private static partial Regex RubricIdRegex();
}
