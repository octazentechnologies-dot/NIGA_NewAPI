using System.Text.RegularExpressions;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Normalization;

/// <summary>V7: converts raw symptoms into standardized clinical concepts via deterministic chains.</summary>
public interface IClinicalNormalizer
{
    V7NormalizedConcept Normalize(V7ExtractedSymptom symptom);
}

public class ClinicalNormalizer : IClinicalNormalizer
{
    private static readonly (Regex Pattern, string[] Chain)[] NormalizationRules =
    {
        (new Regex(@"\b(strong\s+)?shock\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Aura", "Premonitory sensation", "Convulsion aura" }),
        (new Regex(@"\bvibration\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Aura", "Electric shock sensation", "Premonitory symptom" }),
        (new Regex(@"\bforget(s|ting)?\s+everything\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Loss of memory", "Memory after convulsion", "Forgetfulness" }),
        (new Regex(@"\bdrops?\s+(objects?|things?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Drops things", "Awkward hands", "Cannot hold things" }),
        (new Regex(@"\bfear\b.*\b(before|prior)\b.*\b(fit|convulsion|seizure)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Fear before convulsion", "Fear epilepsy", "Fear attack" }),
        (new Regex(@"\b(thirst|drinks?)\b.*\b(large|much|lot)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Thirst large quantities", "Thirst", "Generals thirst" }),
        (new Regex(@"\b(desire|craves?|wants?)\s+salt\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Desire salt", "Craves salt", "Salt appetite" }),
        (new Regex(@"\btalk(s|ing)?\s+in\s+sleep\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Talking in sleep", "Somnambulism", "Sleep talking" }),
        (new Regex(@"\bfear\b.*\b(height|high\s+places?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Fear of heights", "Fear high places", "Vertigo height" }),
        (new Regex(@"\b(desire|craves?|wants?)\s+meat\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new[] { "Desire meat", "Craves meat", "Appetite meat" }),
    };

    public V7NormalizedConcept Normalize(V7ExtractedSymptom symptom)
    {
        var text = !string.IsNullOrWhiteSpace(symptom.Text) ? symptom.Text : symptom.Normalized;
        var chain = new List<string> { text.Trim() };

        foreach (var (pattern, steps) in NormalizationRules)
        {
            if (pattern.IsMatch(text))
            {
                chain.AddRange(steps);
                break;
            }
        }

        if (chain.Count == 1 && !string.IsNullOrWhiteSpace(symptom.Normalized))
        {
            chain.Add(symptom.Normalized.Trim());
        }

        if (!string.IsNullOrWhiteSpace(symptom.Timing))
        {
            chain.Add($"{symptom.Timing} {chain.Last()}");
        }

        if (!string.IsNullOrWhiteSpace(symptom.Location))
        {
            chain.Add($"{chain.Last()} - {symptom.Location}");
        }

        var final = chain.LastOrDefault() ?? text;
        return new V7NormalizedConcept
        {
            OriginalText = text,
            NormalizationChain = chain.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            FinalConcept = final,
        };
    }
}
