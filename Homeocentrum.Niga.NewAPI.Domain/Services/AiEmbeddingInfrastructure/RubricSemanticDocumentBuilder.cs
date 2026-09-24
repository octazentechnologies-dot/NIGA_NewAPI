using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public static class RubricSemanticDocumentBuilder
{
    public static RubricSemanticDocument Build(RepertoryRubricCatalogItem item)
    {
        var (section, subsection, rubricLeaf) = ParseRubricPath(item.RubricName, item.SectionName);
        var clinicalConcepts = new List<string>(item.ClinicalConcepts);
        var homeopathicConcepts = new List<string>(item.HomeopathicConcepts);
        var meanings = new List<string>(item.Meanings);
        var synonyms = new List<string>(item.KnownSynonyms);
        var symptomExamples = new List<string>(item.SymptomExamples);

        EnsureDerivedSemantics(
            item.RubricName,
            rubricLeaf,
            section,
            subsection,
            clinicalConcepts,
            homeopathicConcepts,
            meanings,
            synonyms,
            symptomExamples);

        var document = new RubricSemanticDocument
        {
            RubricId = item.RubricId,
            Section = section,
            Subsection = subsection,
            Rubric = item.RubricName.Trim(),
            ClinicalConcepts = Distinct(clinicalConcepts),
            HomeopathicConcepts = Distinct(homeopathicConcepts),
            KnownSynonyms = Distinct(synonyms),
            Meanings = Distinct(meanings),
            SymptomExamples = Distinct(symptomExamples),
        };

        document.SemanticFieldCount = CountSemanticFields(document);
        document.SourceText = ComposeSourceText(document);
        document.TextHash = AiEmbeddingHashHelper.ComputeSha256(document.SourceText);
        return document;
    }

    public static bool IsTitleOnly(RubricSemanticDocument document) =>
        document.SemanticFieldCount < 3;

    private static void EnsureDerivedSemantics(
        string rubricName,
        string rubricLeaf,
        string section,
        string subsection,
        List<string> clinicalConcepts,
        List<string> homeopathicConcepts,
        List<string> meanings,
        List<string> synonyms,
        List<string> symptomExamples)
    {
        if (clinicalConcepts.Count == 0 && !string.IsNullOrWhiteSpace(rubricLeaf))
            clinicalConcepts.Add($"{section}: {rubricLeaf}");

        if (homeopathicConcepts.Count == 0 && !string.IsNullOrWhiteSpace(subsection))
            homeopathicConcepts.Add(subsection);

        if (homeopathicConcepts.Count == 0 && !string.IsNullOrWhiteSpace(rubricLeaf))
            homeopathicConcepts.Add(rubricLeaf);

        if (meanings.Count == 0 && !string.IsNullOrWhiteSpace(rubricLeaf))
            meanings.Add($"Repertory rubric expressing {rubricLeaf.ToLowerInvariant()} within {section} / {subsection}.");

        if (synonyms.Count == 0 && !string.IsNullOrWhiteSpace(rubricLeaf) && !string.Equals(rubricLeaf, rubricName, StringComparison.OrdinalIgnoreCase))
            synonyms.Add(rubricLeaf);

        if (symptomExamples.Count == 0 && !string.IsNullOrWhiteSpace(rubricLeaf))
            symptomExamples.Add($"Patient may describe: {rubricLeaf.ToLowerInvariant()}.");
    }

    private static (string Section, string Subsection, string RubricLeaf) ParseRubricPath(
        string fullName,
        string sectionName)
    {
        var section = string.IsNullOrWhiteSpace(sectionName) ? "General" : sectionName.Trim();
        if (string.IsNullOrWhiteSpace(fullName))
            return (section, section, section);

        var parts = fullName.Split(" - ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length <= 1)
            return (section, section, fullName.Trim());

        var leaf = parts[^1];
        var subsection = parts.Length >= 3
            ? string.Join(" - ", parts.Skip(1).Take(parts.Length - 2))
            : parts[0];

        if (string.IsNullOrWhiteSpace(subsection))
            subsection = section;

        return (section, subsection, leaf);
    }

    private static string ComposeSourceText(RubricSemanticDocument document)
    {
        var sb = new StringBuilder(1024);
        AppendLine(sb, "Section", document.Section);
        AppendLine(sb, "Subsection", document.Subsection);
        AppendLine(sb, "Rubric", document.Rubric);
        AppendJoined(sb, "Clinical Concept", document.ClinicalConcepts);
        AppendJoined(sb, "Homeopathic Concept", document.HomeopathicConcepts);
        AppendJoined(sb, "Known Synonyms", document.KnownSynonyms);
        AppendJoined(sb, "Meaning", document.Meanings);
        AppendJoined(sb, "Symptom Examples", document.SymptomExamples);
        return sb.ToString().Trim();
    }

    private static int CountSemanticFields(RubricSemanticDocument document)
    {
        var count = 0;
        if (!string.IsNullOrWhiteSpace(document.Section)) count++;
        if (!string.IsNullOrWhiteSpace(document.Subsection)) count++;
        if (!string.IsNullOrWhiteSpace(document.Rubric)) count++;
        if (document.ClinicalConcepts.Count > 0) count++;
        if (document.HomeopathicConcepts.Count > 0) count++;
        if (document.KnownSynonyms.Count > 0) count++;
        if (document.Meanings.Count > 0) count++;
        if (document.SymptomExamples.Count > 0) count++;
        return count;
    }

    private static void AppendLine(StringBuilder sb, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        sb.Append(label).Append(": ").AppendLine(value.Trim());
    }

    private static void AppendJoined(StringBuilder sb, string label, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
            return;
        sb.Append(label).Append(": ").AppendLine(string.Join("; ", values));
    }

    private static List<string> Distinct(IEnumerable<string> values) =>
        values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
}
