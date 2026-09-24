using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Validation;

public interface IEciClinicalValidator
{
    EciClinicalValidationResult Validate(
        IReadOnlyList<EciStructuredSymptom> extractedSymptoms,
        string cleanTranscript);
}

/// <summary>
/// ECI v8: deterministic validation layer (negations, contradictions, duplicates, weak evidence).
/// This module is intentionally conservative and explainable.
/// </summary>
public sealed class EciClinicalValidator : IEciClinicalValidator
{
    private static readonly Regex Negation = new(@"\b(no|not|never|without|denies|denied|doesn't|dont)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ILogger<EciClinicalValidator> _logger;

    public EciClinicalValidator(ILogger<EciClinicalValidator> logger)
    {
        _logger = logger;
    }

    public EciClinicalValidationResult Validate(
        IReadOnlyList<EciStructuredSymptom> extractedSymptoms,
        string cleanTranscript)
    {
        var result = new EciClinicalValidationResult();

        if (extractedSymptoms.Count == 0)
        {
            return result;
        }

        var dedup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var symptom in extractedSymptoms)
        {
            var validated = new EciValidatedSymptom { Symptom = symptom };

            if (string.IsNullOrWhiteSpace(symptom.Symptom))
            {
                validated.Accepted = false;
                validated.RejectReasons.Add("Missing symptom text.");
                result.Validated.Add(validated);
                continue;
            }

            if (string.IsNullOrWhiteSpace(symptom.Evidence))
            {
                validated.Accepted = false;
                validated.RejectReasons.Add("Missing transcript evidence.");
                result.Validated.Add(validated);
                continue;
            }

            if (symptom.Confidence <= 0.40m)
            {
                validated.RejectReasons.Add("Low extraction confidence.");
            }

            if (Negation.IsMatch(symptom.Evidence) && Negation.IsMatch(symptom.Symptom))
            {
                validated.RejectReasons.Add("Negated symptom evidence.");
            }

            var dedupKey = $"{Normalize(symptom.Symptom)}|{Normalize(symptom.Time)}|{Normalize(symptom.Location)}";
            if (!dedup.Add(dedupKey))
            {
                validated.RejectReasons.Add("Duplicate symptom.");
            }

            validated.Accepted = validated.RejectReasons.Count == 0;
            result.Validated.Add(validated);
        }

        return result;
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value.Trim().ToLowerInvariant(), @"\s+", " ");
    }
}

