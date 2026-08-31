using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class CaseUnderstandingEngine : ICaseUnderstandingEngine
{
    private const string SystemPrompt = """
        You are a homeopathic clinical case understanding engine. The repertory uses ENGLISH rubric names only.
        Analyze the consultation transcript and extracted case data. Return strict JSON only:

        {
          "concepts":[
            {
              "rawStatement":"exact patient or doctor quote or paraphrase from transcript",
              "clinicalMeaning":"plain English clinical interpretation",
              "homeopathicMeaning":"homeopathic significance (SRP, strange/rare/peculiar, hierarchy)",
              "category":"mental|general|particular|causation|concomitant",
              "isSRP":true|false,
              "modalities":["better/worse/time/weather triggers"],
              "concomitants":["co-occurring symptoms linked to this concept"],
              "searchTerms":["english","repertory","keywords"],
              "confidence":0.0-1.0
            }
          ],
          "enhancedSymptoms":[
            {
              "phrase":"short English symptom phrase",
              "searchTerms":["english","keywords","for","repertory"],
              "category":"particular|general|mental",
              "intensityHint":1-4
            }
          ]
        }

        Rules:
        - Never invent symptoms not supported by the transcript.
        - Identify metaphors and translate to clinical meaning (e.g. "vibration before fit" → prodromal aura before convulsion).
        - Mark isSRP=true for strange, rare, or peculiar symptoms with high homeopathic value.
        - searchTerms must be English words likely to match homeopathic repertory rubrics.
        - Include causation chains as separate concepts when present (ailments from grief, fright, etc.).
        - enhancedSymptoms should improve on basic extraction with clinically accurate English search terms.
        """;

    private readonly IIntelligenceGptClient _gptClient;
    private readonly AudioCaseTakingOptions _audioOptions;

    public CaseUnderstandingEngine(
        IIntelligenceGptClient gptClient,
        IOptions<AudioCaseTakingOptions> audioOptions)
    {
        _gptClient = gptClient;
        _audioOptions = audioOptions.Value;
    }

    public async Task<CaseUnderstandingEngineResult> AnalyzeAsync(
        string transcript,
        IReadOnlyList<AudioCaseSymptomModel> existingSymptoms,
        AudioCaseSummaryModel? summary,
        string? detectedLanguage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return new CaseUnderstandingEngineResult
            {
                Success = false,
                Error = "Transcript is empty.",
            };
        }

        if (_gptClient is IntelligenceGptClient { IsConfigured: false } && _audioOptions.UseMockWhenNoApiKey)
        {
            return BuildMockResult(transcript, existingSymptoms, detectedLanguage);
        }

        var symptomLines = existingSymptoms.Count == 0
            ? "None"
            : string.Join("\n", existingSymptoms.Select(s =>
                $"- {s.Phrase} [{s.Category}] terms: {string.Join(", ", s.SearchTerms ?? [])}"));

        var summaryText = summary == null
            ? "N/A"
            : $"""
               Chief complaint: {summary.ChiefComplaint}
               HPI: {summary.HistoryOfPresentIllness}
               Mentals: {string.Join("; ", summary.Mentals ?? [])}
               Generals: {string.Join("; ", summary.Generals ?? [])}
               Modalities: {string.Join("; ", summary.Modalities ?? [])}
               Particulars: {string.Join("; ", summary.Particulars ?? [])}
               """;

        var userPrompt = $"""
            Detected language: {detectedLanguage ?? "unknown"}

            Transcript:
            {transcript}

            Previously extracted symptoms:
            {symptomLines}

            Case summary:
            {summaryText}
            """;

        var gptResult = await _gptClient.CompleteJsonAsync<CaseUnderstandingGptModel>(
            SystemPrompt,
            userPrompt,
            "CaseUnderstanding",
            cancellationToken);

        if (!gptResult.Success || gptResult.Result == null)
        {
            if (_audioOptions.UseMockWhenNoApiKey)
            {
                return BuildMockResult(transcript, existingSymptoms, detectedLanguage);
            }

            return new CaseUnderstandingEngineResult
            {
                Success = false,
                RequestJson = gptResult.RequestJson,
                ResponseJson = gptResult.ResponseJson,
                PromptTokens = gptResult.PromptTokens,
                CompletionTokens = gptResult.CompletionTokens,
                LatencyMs = gptResult.LatencyMs,
                Error = gptResult.Error ?? "Case understanding failed.",
            };
        }

        var concepts = MapConcepts(gptResult.Result.Concepts, detectedLanguage);
        var enhancedSymptoms = MapSymptoms(gptResult.Result.EnhancedSymptoms);

        return new CaseUnderstandingEngineResult
        {
            Success = true,
            Concepts = concepts,
            EnhancedSymptoms = enhancedSymptoms,
            RequestJson = gptResult.RequestJson,
            ResponseJson = gptResult.ResponseJson,
            PromptTokens = gptResult.PromptTokens,
            CompletionTokens = gptResult.CompletionTokens,
            LatencyMs = gptResult.LatencyMs,
        };
    }

    internal static CaseUnderstandingEngineResult BuildMockResult(
        string transcript,
        IReadOnlyList<AudioCaseSymptomModel> existingSymptoms,
        string? detectedLanguage)
    {
        var concepts = new List<ClinicalConceptModel>();
        var lower = transcript.ToLowerInvariant();

        if (lower.Contains("vibration") && (lower.Contains("fit") || lower.Contains("convulsion") || lower.Contains("seizure")))
        {
            concepts.Add(new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = "Vibration in hands before fit",
                ClinicalMeaning = "Prodromal aura — sensory warning before convulsive episode",
                HomeopathicMeaning = "Strange warning symptom before convulsion — high SRP value",
                Category = "particular",
                IsSRP = true,
                Modalities = new List<string> { "before convulsion", "hands" },
                Concomitants = new List<string>(),
                SearchTerms = new List<string> { "aura", "convulsion", "warning", "prodrome", "vibration" },
                Confidence = 0.92m,
                SourceLanguage = detectedLanguage ?? "en",
            });
        }

        if (concepts.Count == 0 && existingSymptoms.Count > 0)
        {
            foreach (var symptom in existingSymptoms.Take(5))
            {
                concepts.Add(new ClinicalConceptModel
                {
                    ConceptId = Guid.NewGuid(),
                    RawStatement = symptom.Phrase,
                    ClinicalMeaning = symptom.Phrase,
                    HomeopathicMeaning = symptom.Phrase,
                    Category = symptom.Category ?? "particular",
                    IsSRP = false,
                    SearchTerms = symptom.SearchTerms?.ToList() ?? new List<string>(),
                    Confidence = 0.75m,
                    SourceLanguage = detectedLanguage ?? "en",
                });
            }
        }

        var enhancedSymptoms = existingSymptoms.Select(s => new AudioCaseSymptomModel
        {
            Phrase = s.Phrase,
            SearchTerms = s.SearchTerms?.ToList() ?? new List<string>(),
            Category = s.Category,
            IntensityHint = s.IntensityHint,
        }).ToList();

        if (concepts.Count > 0 && concepts[0].SearchTerms.Count > 0)
        {
            enhancedSymptoms.Insert(0, new AudioCaseSymptomModel
            {
                Phrase = concepts[0].ClinicalMeaning ?? concepts[0].RawStatement,
                SearchTerms = concepts[0].SearchTerms,
                Category = concepts[0].Category ?? "particular",
                IntensityHint = 3,
            });
        }

        return new CaseUnderstandingEngineResult
        {
            Success = true,
            Concepts = concepts,
            EnhancedSymptoms = enhancedSymptoms,
            LatencyMs = 0,
        };
    }

    private static List<ClinicalConceptModel> MapConcepts(
        IReadOnlyList<CaseUnderstandingConceptGptModel> items,
        string? detectedLanguage)
    {
        return items
            .Where(c => !string.IsNullOrWhiteSpace(c.RawStatement))
            .Select(c => new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = c.RawStatement.Trim(),
                ClinicalMeaning = c.ClinicalMeaning?.Trim(),
                HomeopathicMeaning = c.HomeopathicMeaning?.Trim(),
                Category = NormalizeCategory(c.Category),
                IsSRP = c.IsSRP,
                Modalities = c.Modalities?.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct().ToList() ?? new(),
                Concomitants = c.Concomitants?.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct().ToList() ?? new(),
                SearchTerms = c.SearchTerms?.Where(m => !string.IsNullOrWhiteSpace(m)).Select(m => m.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new(),
                Confidence = ClampConfidence(c.Confidence),
                SourceLanguage = detectedLanguage,
            })
            .ToList();
    }

    private static List<AudioCaseSymptomModel> MapSymptoms(IReadOnlyList<CaseUnderstandingSymptomGptModel> items)
    {
        return items
            .Where(s => !string.IsNullOrWhiteSpace(s.Phrase))
            .Select(s => new AudioCaseSymptomModel
            {
                Phrase = s.Phrase.Trim(),
                SearchTerms = s.SearchTerms?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new(),
                Category = NormalizeCategory(s.Category),
                IntensityHint = s.IntensityHint is >= 1 and <= 4 ? s.IntensityHint : 2,
            })
            .ToList();
    }

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return "particular";
        var c = category.Trim().ToLowerInvariant();
        return c switch
        {
            "mental" or "mind" => "mental",
            "general" or "generals" => "general",
            "causation" or "cause" => "causation",
            "concomitant" => "concomitant",
            _ => "particular",
        };
    }

    private static decimal ClampConfidence(decimal value) =>
        value < 0 ? 0 : value > 1 ? 1 : value;
}
