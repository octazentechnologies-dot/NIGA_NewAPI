using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Extraction;

public interface IEciStructuredSymptomExtractor
{
    Task<EciStructuredSymptomExtractionResult> ExtractAsync(
        string cleanTranscript,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// ECI v8: GPT extraction is strictly language understanding and returns structured symptoms ONLY.
/// It must never output rubrics or remedies.
/// </summary>
public sealed class EciStructuredSymptomExtractor : IEciStructuredSymptomExtractor
{
    public const string ModelId = "eci-v8-symptom-extractor";

    private const string SystemPrompt = """
You are an enterprise clinical symptom extraction engine for homeopathic case taking.

Output JSON only with the shape:
{
  "symptoms": [
    {
      "symptom": "...",
      "evidence": "...",
      "speaker": "Patient|Doctor|System|Unknown",
      "confidence": 0.0-1.0,
      "time": "Before|During|After|Since childhood|Recently|Previously|Currently|Improving|Worsening|...",
      "location": "...",
      "modality": "...",
      "sensation": "...",
      "emotion": "...",
      "trigger": "...",
      "amelioration": "...",
      "aggravation": "...",
      "intensity": "...",
      "duration": "...",
      "frequency": "..."
    }
  ]
}

Mandatory rules:
- NEVER output repertory rubrics.
- NEVER output repertory section names (e.g. "MIND -", "GENERALITIES -").
- NEVER output remedies or medicine names.
- NEVER invent SQL queries or database logic.
- Every symptom must include a direct transcript evidence quote/span.
""";

    private readonly IIntelligenceGptClient _gpt;
    private readonly OpenAiOptions _openAi;
    private readonly ILogger<EciStructuredSymptomExtractor> _logger;

    public EciStructuredSymptomExtractor(
        IIntelligenceGptClient gpt,
        IOptions<OpenAiOptions> openAi,
        ILogger<EciStructuredSymptomExtractor> logger)
    {
        _gpt = gpt;
        _openAi = openAi.Value;
        _logger = logger;
    }

    public async Task<EciStructuredSymptomExtractionResult> ExtractAsync(
        string cleanTranscript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cleanTranscript))
        {
            return new EciStructuredSymptomExtractionResult();
        }

        if (string.IsNullOrWhiteSpace(_openAi.ApiKey))
        {
            _logger.LogWarning("ECI v8 symptom extractor skipped (OpenAI API key not configured).");
            return new EciStructuredSymptomExtractionResult();
        }

        var result = await _gpt.CompleteJsonAsync<EciStructuredSymptomExtractionResult>(
            SystemPrompt,
            $"Transcript:\n{cleanTranscript}",
            ModelId,
            cancellationToken);

        if (!result.Success || result.Result?.Symptoms == null)
        {
            _logger.LogWarning("ECI v8 symptom extraction failed: {Error}", result.Error);
            return new EciStructuredSymptomExtractionResult();
        }

        // Normalize confidence and speaker enums defensively (deterministic).
        foreach (var s in result.Result.Symptoms)
        {
            s.Confidence = Math.Clamp(s.Confidence, 0m, 1m);
            s.Speaker = s.Speaker switch
            {
                EciSpeakerRole.Doctor or EciSpeakerRole.Patient or EciSpeakerRole.System => s.Speaker,
                _ => EciSpeakerRole.Patient,
            };
        }

        // Remove any entries missing evidence (will be recorded by validator later too).
        result.Result.Symptoms = result.Result.Symptoms
            .Where(s => !string.IsNullOrWhiteSpace(s.Symptom))
            .Where(s => !string.IsNullOrWhiteSpace(s.Evidence))
            .Take(60)
            .ToList();

        return result.Result;
    }
}

