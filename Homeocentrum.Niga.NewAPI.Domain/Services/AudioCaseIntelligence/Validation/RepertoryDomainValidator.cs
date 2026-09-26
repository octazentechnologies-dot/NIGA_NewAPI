using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Validation;

public static class RepertoryDomainValidator
{
    private static readonly Dictionary<string, string[]> SectionKeywordHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ABDOMEN"] = new[] { "abdomen", "stomach", "belly", "gastric", "intestinal", "nausea", "bloat", "appetite", "diarr", "constip" },
        ["EAR"] = new[] { "ear", "hearing", "tinnitus", "ringing", "deaf", "otalg" },
        ["EYE"] = new[] { "eye", "vision", "sight", "blind", "lacrim" },
        ["NOSE"] = new[] { "nose", "nasal", "smell", "sneez", "rhinit" },
        ["THROAT"] = new[] { "throat", "swallow", "larynx", "pharynx" },
        ["CHEST"] = new[] { "chest", "breast", "lung", "respir", "breath", "cough" },
        ["BACK"] = new[] { "back", "spine", "lumbar", "sacral" },
        ["EXTREMITIES"] = new[] { "limb", "arm", "leg", "hand", "foot", "joint", "extremit" },
        ["SKIN"] = new[] { "skin", "rash", "itch", "eruption", "eczema" },
        ["MIND"] = new[] { "fear", "anxiety", "mind", "mental", "delusion", "memory", "mood", "depress", "anger", " irrit" },
        ["VERTIGO"] = new[] { "vertigo", "dizzy", "giddiness" },
        ["HEAD"] = new[] { "head", "headache", "cephal" },
        ["FACE"] = new[] { "face", "facial", "cheek", "jaw" },
        ["MOUTH"] = new[] { "mouth", "tongue", "saliv", "teeth", "gum" },
        ["STOOL"] = new[] { "stool", "bowel", "diarr", "constip", "rect" },
        ["URINE"] = new[] { "urine", "urinat", "bladder", "mictur" },
        ["MALE"] = new[] { "male", "prostate", "penis", "erect", "semen" },
        ["FEMALE"] = new[] { "female", "menses", "menstru", "ovary", "uterus", "vagin", "breast", "labor" },
        ["PREGNANCY"] = new[] { "pregnan", "gestation", "trimester" },
        ["CHILDREN"] = new[] { "child", "infant", "baby", "pediatric" },
    };

    private static readonly HashSet<string> BroadSections = new(StringComparer.OrdinalIgnoreCase)
    {
        "GENERALS", "GENERALITIES", "CONCOMITANTS", "CAUSES", "MODALITIES", "TIME",
    };

    public static RubricValidationIssueModel? Validate(
        AudioCaseSuggestedRubricModel rubric,
        ClinicalConceptModel? linkedConcept,
        RubricEvidenceChainModel evidenceChain)
    {
        var section = ExtractSection(rubric.SubSectionName);
        if (string.IsNullOrWhiteSpace(section) || BroadSections.Contains(section))
        {
            return null;
        }

        if (!SectionKeywordHints.TryGetValue(section, out var hints))
        {
            return null;
        }

        var evidenceText = BuildEvidenceText(linkedConcept, evidenceChain, rubric);
        if (string.IsNullOrWhiteSpace(evidenceText))
        {
            return new RubricValidationIssueModel
            {
                Code = "DomainMismatch",
                Message = $"No clinical evidence supports repertory section '{section}' for rubric '{rubric.SubSectionName}'.",
                PenaltyPoints = 45,
                IsHardReject = false,
            };
        }

        var evidenceLower = evidenceText.ToLowerInvariant();
        var hasKeywordHint = hints.Any(h => evidenceLower.Contains(h, StringComparison.Ordinal));
        var tail = ExtractTail(rubric.SubSectionName);
        var tailSimilarity = AudioCaseAiProcessor.ComputeTextSimilarity(tail, evidenceText);

        if (!hasKeywordHint && tailSimilarity < 0.42m)
        {
            return new RubricValidationIssueModel
            {
                Code = "DomainMismatch",
                Message = $"Rubric section '{section}' does not align with patient evidence for '{rubric.SubSectionName}'.",
                PenaltyPoints = 40,
                IsHardReject = false,
            };
        }

        return null;
    }

    private static string ExtractSection(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName)) return string.Empty;
        var dash = rubricName.IndexOf('-');
        return dash > 0 ? rubricName[..dash].Trim() : rubricName.Trim();
    }

    private static string ExtractTail(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName)) return string.Empty;
        var dash = rubricName.IndexOf('-');
        return dash > 0 && dash < rubricName.Length - 1
            ? rubricName[(dash + 1)..].Trim()
            : rubricName.Trim();
    }

    private static string BuildEvidenceText(
        ClinicalConceptModel? concept,
        RubricEvidenceChainModel evidenceChain,
        AudioCaseSuggestedRubricModel rubric)
    {
        var parts = new List<string>();
        if (concept != null)
        {
            parts.Add(concept.RawStatement);
            if (!string.IsNullOrWhiteSpace(concept.ClinicalMeaning)) parts.Add(concept.ClinicalMeaning);
            if (!string.IsNullOrWhiteSpace(concept.HomeopathicMeaning)) parts.Add(concept.HomeopathicMeaning);
            parts.AddRange(concept.SearchTerms);
        }

        parts.AddRange(evidenceChain.PatientStatements);
        parts.AddRange(evidenceChain.ClinicalMeanings);
        if (!string.IsNullOrWhiteSpace(evidenceChain.MatchedSymptomPhrase)) parts.Add(evidenceChain.MatchedSymptomPhrase);
        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom)) parts.Add(rubric.MatchedFrom);

        return string.Join(' ', parts.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase));
    }
}
