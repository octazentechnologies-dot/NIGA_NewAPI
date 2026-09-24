using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V6;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Extraction;

/// <summary>V7: GPT structured symptom extraction — language understanding only, no rubrics.</summary>
public interface IClinicalExtractionService
{
    Task<IReadOnlyList<V7ExtractedSymptom>> ExtractFromGraphAsync(
        ConceptGraphFullModel graph,
        string transcript,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<V7ExtractedSymptom>> ExtractFromGptAsync(
        string transcript,
        CancellationToken cancellationToken = default);
}

public class ClinicalExtractionService : IClinicalExtractionService
{
    public const string ModelId = "v7-extraction";

    private const string SystemPrompt = """
        You are a clinical language extraction engine for homeopathic case taking.
        Extract symptoms from the transcript as structured JSON only.
        NEVER output repertory rubric names.
        NEVER invent rubric names from Kent or any repertory.
        NEVER suggest remedies.
        Output symptoms with: text, normalized, category (Mental|General|Particular|Modality|Etiology|Concomitant), timing, location, confidence (0-100).
        """;

    private readonly IIntelligenceGptClient _gptClient;
    private readonly OpenAiOptions _openAiOptions;
    private readonly ILogger<ClinicalExtractionService> _logger;

    public ClinicalExtractionService(
        IIntelligenceGptClient gptClient,
        IOptions<OpenAiOptions> openAiOptions,
        ILogger<ClinicalExtractionService> logger)
    {
        _gptClient = gptClient;
        _openAiOptions = openAiOptions.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<V7ExtractedSymptom>> ExtractFromGraphAsync(
        ConceptGraphFullModel graph,
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var symptoms = new List<V7ExtractedSymptom>();

        foreach (var homeo in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
        {
            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            var meaning = clinical != null
                ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                    ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                : null;

            symptoms.Add(new V7ExtractedSymptom
            {
                Text = meaning?.RawStatement ?? homeo.EvidenceSpan ?? homeo.ConceptName,
                Normalized = homeo.ConceptName,
                Category = ClassifyCategory(homeo, clinical),
                Timing = InferTiming(meaning?.RawStatement ?? transcript),
                Location = InferLocation(homeo.ConceptName, clinical?.ConceptName),
                Confidence = Math.Round(homeo.Confidence * 100m, 2),
                SourceConceptId = homeo.HomeopathicConceptId,
                TranscriptEvidence = meaning?.RawStatement ?? homeo.EvidenceSpan,
            });
        }

        return Task.FromResult<IReadOnlyList<V7ExtractedSymptom>>(symptoms);
    }

    public async Task<IReadOnlyList<V7ExtractedSymptom>> ExtractFromGptAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_openAiOptions.ApiKey) || string.IsNullOrWhiteSpace(transcript))
        {
            return Array.Empty<V7ExtractedSymptom>();
        }

        var result = await _gptClient.CompleteJsonAsync<V7GptSymptomExtractionResult>(
            SystemPrompt,
            $"Extract all clinical symptoms from this transcript:\n\n{transcript}",
            ModelId,
            cancellationToken);

        if (!result.Success || result.Result?.Symptoms == null)
        {
            _logger.LogWarning("V7 GPT extraction failed: {Error}", result.Error);
            return Array.Empty<V7ExtractedSymptom>();
        }

        return result.Result.Symptoms
            .Where(s => !string.IsNullOrWhiteSpace(s.Text))
            .Select(s =>
            {
                s.TranscriptEvidence ??= s.Text;
                s.Confidence = s.Confidence > 0 ? s.Confidence : 75m;
                return s;
            })
            .ToList();
    }

    private static string ClassifyCategory(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical)
    {
        var category = (homeo.Category ?? clinical?.SymptomCategory ?? clinical?.Domain ?? "Clinical").ToLowerInvariant();
        return category switch
        {
            var c when c.Contains("mental") => "Mental",
            var c when c.Contains("general") => "General",
            var c when c.Contains("modality") => "Modality",
            var c when c.Contains("cause") || c.Contains("etiology") => "Etiology",
            var c when c.Contains("concomitant") => "Concomitant",
            _ => "Particular",
        };
    }

    private static string? InferTiming(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var lower = text.ToLowerInvariant();
        if (lower.Contains("before") || lower.Contains("prior") || lower.Contains("aura"))
        {
            return "Before";
        }

        if (lower.Contains("during") || lower.Contains("while"))
        {
            return "During";
        }

        if (lower.Contains("after") || lower.Contains("following"))
        {
            return "After";
        }

        return null;
    }

    private static string? InferLocation(string? homeoName, string? clinicalName)
    {
        var combined = $"{homeoName} {clinicalName}".ToLowerInvariant();
        if (combined.Contains("hand"))
        {
            return "Hands";
        }

        if (combined.Contains("head"))
        {
            return "Head";
        }

        if (combined.Contains("stomach") || combined.Contains("abdomen"))
        {
            return "Abdomen";
        }

        return null;
    }
}
