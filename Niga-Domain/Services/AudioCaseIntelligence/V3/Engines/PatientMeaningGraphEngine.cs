using Microsoft.Extensions.Logging;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

public class PatientMeaningGraphEngine : IPatientMeaningGraphEngine
{
    public const string ModelId = "v3-m1";
    public const string StageName = "PatientMeaningGraph";

    private const string SystemPrompt = """
        You are Model M1 — Patient Meaning Graph Engine for homeopathic case taking.

        Purpose: Convert patient language into structured MEANINGS only.

        Input: consultation transcript (Marathi, Hindi, English, or mixed).
        Optional: dual-language sensation hints with englishTranslation + originalLanguageText.

        Output strict JSON only:
        {
          "meanings":[
            {
              "rawStatement":"exact patient quote or faithful paraphrase from transcript",
              "normalizedMeaning":"concise English clinical meaning",
              "languageCode":"mr|hi|en|mixed",
              "confidence":0.0-1.0,
              "isSensationBearing":false
            }
          ]
        }

        CRITICAL RULES:
        - Extract EVERY distinct clinical expression the patient uses.
        - Do NOT generate rubrics, repertory terms, searchTerms, or SubSection names.
        - Do NOT invent symptoms not in the transcript.
        - Preserve clinical specificity: timing (before/after), location, sensation type.
        - For Marathi/Hindi, rawStatement may stay in source language; normalizedMeaning MUST be English.
        - When originalLanguageText is provided for a sensation, prefer it as rawStatement and enrich
          normalizedMeaning using BOTH the English translation and the original idiom nuance.
        - For metaphors ("current in body", "heart jumps"), normalizedMeaning states the clinical sensation.
        - One meaning per distinct symptom expression; do not merge unrelated statements.
        - confidence: how clearly the transcript supports this meaning (0.9+ for explicit statements).
        - isSensationBearing: true for sensations, emotions, idiomatic bodily feelings (burning, crawling ants, fear, grief).
        """;

    private readonly IIntelligenceGptClient _gptClient;
    private readonly ILogger<PatientMeaningGraphEngine> _logger;

    public PatientMeaningGraphEngine(
        IIntelligenceGptClient gptClient,
        ILogger<PatientMeaningGraphEngine> logger)
    {
        _gptClient = gptClient;
        _logger = logger;
    }

    public Task<PatientMeaningGraphResult> BuildAsync(
        string transcript,
        string? detectedLanguage,
        CancellationToken cancellationToken = default) =>
        BuildAsync(transcript, detectedLanguage, dualLanguage: null, cancellationToken);

    public async Task<PatientMeaningGraphResult> BuildAsync(
        string transcript,
        string? detectedLanguage,
        DualLanguageMeaningContext? dualLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new PatientMeaningGraphResult
            {
                Success = false,
                Error = "Transcript is empty.",
            };
        }

        var dualSection = BuildDualLanguagePromptSection(dualLanguage);

        var userPrompt = $"""
            Detected session language hint: {detectedLanguage ?? dualLanguage?.LanguageCode ?? "unknown"}

            Transcript:
            {transcript}
            {dualSection}
            """;

        var gptResult = await _gptClient.CompleteJsonAsync<PatientMeaningExtractionGptModel>(
            SystemPrompt,
            userPrompt,
            StageName,
            cancellationToken);

        if (!gptResult.Success || gptResult.Result == null)
        {
            _logger.LogWarning("Patient meaning graph M1 failed: {Error}", gptResult.Error);
            return new PatientMeaningGraphResult
            {
                Success = false,
                Error = gptResult.Error ?? "Patient meaning extraction failed.",
                LatencyMs = gptResult.LatencyMs,
            };
        }

        var meanings = gptResult.Result.Meanings
            .Where(m => !string.IsNullOrWhiteSpace(m.NormalizedMeaning))
            .Select((m, index) => MapMeaning(m, index, detectedLanguage, dualLanguage))
            .ToList();

        if (meanings.Count == 0)
        {
            return new PatientMeaningGraphResult
            {
                Success = false,
                Error = "No patient meanings extracted.",
                LatencyMs = gptResult.LatencyMs,
            };
        }

        return new PatientMeaningGraphResult
        {
            Success = true,
            Meanings = meanings,
            LatencyMs = gptResult.LatencyMs,
            ModelVersion = ModelId,
        };
    }

    private static PatientMeaningNodeModel MapMeaning(
        PatientMeaningExtractionItemGptModel m,
        int index,
        string? detectedLanguage,
        DualLanguageMeaningContext? dualLanguage)
    {
        var english = (m.NormalizedMeaning ?? string.Empty).Trim();
        var language = NormalizeLanguage(m.LanguageCode, detectedLanguage ?? dualLanguage?.LanguageCode);
        var sensation = m.IsSensationBearing || MatchesSensationHint(english, dualLanguage);
        var original = FindOriginalLanguageText(english, m.RawStatement, dualLanguage);

        string raw;
        if (sensation && !string.IsNullOrWhiteSpace(original))
            raw = original.Trim();
        else
            raw = (m.RawStatement ?? m.NormalizedMeaning ?? string.Empty).Trim();

        return new PatientMeaningNodeModel
        {
            RawStatement = raw,
            NormalizedMeaning = english,
            EnglishTranslation = english,
            OriginalLanguageText = original,
            IsSensationBearing = sensation,
            LanguageCode = language,
            Confidence = ClampConfidence(m.Confidence),
            SequenceOrder = index + 1,
            ModelVersion = ModelId,
        };
    }

    private static string BuildDualLanguagePromptSection(DualLanguageMeaningContext? dualLanguage)
    {
        if (dualLanguage == null)
            return string.Empty;

        var lines = new List<string>
        {
            "",
            "Dual-language sensation context (Task 4):",
            $"LanguageCode: {dualLanguage.LanguageCode ?? "unknown"}",
        };

        if (!string.IsNullOrWhiteSpace(dualLanguage.OriginalLanguageTranscript))
        {
            lines.Add("Original-language transcript (use for sensation rawStatement when idioms differ from English):");
            lines.Add(dualLanguage.OriginalLanguageTranscript.Trim());
        }

        if (dualLanguage.SensationHints.Count > 0)
        {
            lines.Add("Sensation-bearing hints:");
            foreach (var hint in dualLanguage.SensationHints.Take(20))
            {
                lines.Add(
                    $"- englishTranslation=\"{hint.EnglishPhrase}\"; originalLanguageText=\"{hint.OriginalLanguageText ?? ""}\"; languageCode=\"{hint.LanguageCode ?? dualLanguage.LanguageCode}\"");
            }
        }

        return string.Join('\n', lines);
    }

    private static bool MatchesSensationHint(string english, DualLanguageMeaningContext? dualLanguage)
    {
        if (dualLanguage == null || dualLanguage.SensationHints.Count == 0 || string.IsNullOrWhiteSpace(english))
            return false;

        return dualLanguage.SensationHints.Any(h =>
            !string.IsNullOrWhiteSpace(h.EnglishPhrase)
            && (english.Contains(h.EnglishPhrase, StringComparison.OrdinalIgnoreCase)
                || h.EnglishPhrase.Contains(english, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? FindOriginalLanguageText(
        string english,
        string? gptRaw,
        DualLanguageMeaningContext? dualLanguage)
    {
        if (dualLanguage == null)
            return null;

        var hint = dualLanguage.SensationHints.FirstOrDefault(h =>
            !string.IsNullOrWhiteSpace(h.EnglishPhrase)
            && (english.Contains(h.EnglishPhrase, StringComparison.OrdinalIgnoreCase)
                || h.EnglishPhrase.Contains(english, StringComparison.OrdinalIgnoreCase)));

        if (!string.IsNullOrWhiteSpace(hint?.OriginalLanguageText))
            return hint!.OriginalLanguageText;

        // Prefer non-ASCII GPT rawStatement when dual-language is active.
        if (!string.IsNullOrWhiteSpace(gptRaw) && gptRaw.Any(c => c > 127))
            return gptRaw;

        return null;
    }

    private static string NormalizeLanguage(string? code, string? fallback)
    {
        var value = (code ?? fallback ?? "en").Trim().ToLowerInvariant();
        return value switch
        {
            "marathi" or "mr" => "mr",
            "hindi" or "hi" => "hi",
            "english" or "en" => "en",
            "mixed" => "mixed",
            _ => value.Length <= 10 ? value : "en",
        };
    }

    private static decimal ClampConfidence(decimal value) =>
        Math.Clamp(value <= 0 ? 0.75m : value, 0m, 1m);
}
