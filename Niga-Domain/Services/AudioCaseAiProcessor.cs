using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services;

public class AudioCaseAiProcessor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAiOptions _openAiOptions;
    private readonly AudioCaseTakingOptions _audioOptions;
    private readonly ILogger<AudioCaseAiProcessor> _logger;

    public AudioCaseAiProcessor(
        IHttpClientFactory httpClientFactory,
        IOptions<OpenAiOptions> openAiOptions,
        IOptions<AudioCaseTakingOptions> audioOptions,
        ILogger<AudioCaseAiProcessor> logger)
    {
        _httpClientFactory = httpClientFactory;
        _openAiOptions = openAiOptions.Value;
        _audioOptions = audioOptions.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_openAiOptions.ApiKey);

    public async Task<(bool Success, string? Transcript, string? Language, string? RequestJson, string? ResponseJson, int LatencyMs, string? Error)>
        TranslateAudioToEnglishAsync(string audioFilePath, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            if (!_audioOptions.UseMockWhenNoApiKey)
            {
                return (false, null, null, null, null, 0, "OpenAI API key is not configured.");
            }

            var mockTranscript =
                "Doctor: Since when do you have this headache? Patient: About three days. It is worse in the morning.";
            return (true, mockTranscript, "en", null, mockTranscript, 0, null);
        }

        if (_audioOptions.EnableWhisperChunking)
        {
            var chunked = await TryTranslateChunkedAsync(audioFilePath, translate: true, language: null, cancellationToken);
            if (chunked != null)
                return chunked.Value;
        }

        return await TranslateOrTranscribeFullFileAsync(audioFilePath, translate: true, language: null, cancellationToken);
    }

    public async Task<(bool Success, string? Transcript, string? Language, string? RequestJson, string? ResponseJson, int LatencyMs, string? Error)>
        TranscribeAsync(string audioFilePath, string? language, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            if (!_audioOptions.UseMockWhenNoApiKey)
            {
                return (false, null, null, null, null, 0, "OpenAI API key is not configured.");
            }

            var mockTranscript =
                "Doctor: Since when do you have this headache? Patient: About three days. It is worse in the morning. Doctor: Any nausea? Patient: Yes, with the headache.";
            return (true, mockTranscript, language ?? "en", null, mockTranscript, 0, null);
        }

        if (_audioOptions.EnableWhisperChunking)
        {
            var chunked = await TryTranslateChunkedAsync(audioFilePath, translate: false, language, cancellationToken);
            if (chunked != null)
                return chunked.Value;
        }

        return await TranslateOrTranscribeFullFileAsync(audioFilePath, translate: false, language, cancellationToken);
    }

    private async Task<(bool Success, string? Transcript, string? Language, string? RequestJson, string? ResponseJson, int LatencyMs, string? Error)?>
        TryTranslateChunkedAsync(string audioFilePath, bool translate, string? language, CancellationToken cancellationToken)
    {
        var duration = await WhisperAudioChunker.TryGetDurationSecondsAsync(audioFilePath, _logger, cancellationToken);
        if (duration == null || duration.Value < _audioOptions.WhisperChunkThresholdSeconds)
            return null;

        var chunkPaths = await WhisperAudioChunker.TrySplitAsync(
            audioFilePath,
            duration.Value,
            _audioOptions.WhisperChunkSeconds,
            _audioOptions.WhisperChunkOverlapSeconds,
            _logger,
            cancellationToken);

        if (chunkPaths == null || chunkPaths.Count <= 1)
            return null;

        var sw = Stopwatch.StartNew();
        try
        {
            var maxParallel = Math.Clamp(_audioOptions.WhisperChunkMaxParallel, 1, 8);
            using var throttle = new SemaphoreSlim(maxParallel, maxParallel);
            var tasks = chunkPaths.Select(async (path, index) =>
            {
                await throttle.WaitAsync(cancellationToken);
                try
                {
                    var result = await TranslateOrTranscribeFullFileAsync(path, translate, language, cancellationToken);
                    return (Index: index, Result: result);
                }
                finally
                {
                    throttle.Release();
                }
            }).ToList();

            var completed = await Task.WhenAll(tasks);
            sw.Stop();

            if (completed.Any(x => !x.Result.Success || string.IsNullOrWhiteSpace(x.Result.Transcript)))
            {
                var firstError = completed.First(x => !x.Result.Success).Result.Error;
                _logger.LogWarning("Whisper chunked transcription had a failed chunk. Falling back to full file.");
                return null;
            }

            var ordered = completed
                .OrderBy(x => x.Index)
                .Select(x => (x.Index, x.Result.Transcript!, x.Result.ResponseJson))
                .ToList();
            var joined = WhisperAudioChunker.JoinTranscripts(ordered, _audioOptions.WhisperChunkOverlapSeconds);
            var languageOut = translate ? "en" : (completed[0].Result.Language ?? language);
            _logger.LogInformation(
                "Whisper chunked transcription complete chunks={Count} elapsedMs={Ms} durationSec={Duration:F0}",
                chunkPaths.Count,
                (int)sw.ElapsedMilliseconds,
                duration.Value);

            return (true, joined, languageOut, null, null, (int)sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Whisper chunked transcription failed. Falling back to full file.");
            return null;
        }
        finally
        {
            WhisperAudioChunker.CleanupFiles(chunkPaths);
        }
    }

    private async Task<(bool Success, string? Transcript, string? Language, string? RequestJson, string? ResponseJson, int LatencyMs, string? Error)>
        TranslateOrTranscribeFullFileAsync(string audioFilePath, bool translate, string? language, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var fileStream = File.OpenRead(audioFilePath);
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", Path.GetFileName(audioFilePath));
            content.Add(new StringContent(_openAiOptions.WhisperModel), "model");
            content.Add(new StringContent("verbose_json"), "response_format");
            if (!translate && !string.IsNullOrWhiteSpace(language))
                content.Add(new StringContent(language), "language");

            var client = _httpClientFactory.CreateClient("OpenAI");
            var endpoint = translate ? "audio/translations" : "audio/transcriptions";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content,
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, null, null, responseBody, (int)sw.ElapsedMilliseconds, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var transcript = doc.RootElement.TryGetProperty("text", out var textEl)
                ? textEl.GetString()
                : responseBody;
            var detectedLanguage = translate
                ? "en"
                : (doc.RootElement.TryGetProperty("language", out var langEl) ? langEl.GetString() : language);

            return (true, transcript, detectedLanguage, null, responseBody, (int)sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, translate ? "Whisper English translation failed." : "Whisper transcription failed.");
            return (false, null, null, null, null, (int)sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<(bool Success, AudioCaseExtractionModel? Result, string? RequestJson, string? ResponseJson, int PromptTokens, int CompletionTokens, int LatencyMs, string? Error)>
        ExtractCaseDataAsync(string transcript, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            return (false, null, null, null, 0, 0, 0, "Transcript is empty.");
        }

        transcript = TrimTranscriptForExtraction(transcript);

        if (!IsConfigured)
        {
            if (!_audioOptions.UseMockWhenNoApiKey)
            {
                return (false, null, null, null, 0, 0, 0, "OpenAI API key is not configured.");
            }

            return (true, BuildMockExtraction(transcript), null, null, 0, 0, 0, null);
        }

        var systemPrompt = """
            You are a homeopathic case-taking assistant. The repertory database uses ENGLISH rubric names only.
            ALL output must be in English. If the transcript is in another language, translate symptom phrases and summary to English.

            The full transcript is provided separately — do NOT repeat or echo the full transcript in your response.

            Return strict JSON only with this schema:
            {
              "conversation":[{"role":"doctor|patient","text":"English text","timestamp":"HH:MM:SS or empty"}],
              "symptoms":[
                {
                  "phrase":"short English symptom phrase using patient's clinical wording",
                  "searchTerms":["english","keywords","for","repertory","lookup"],
                  "category":"particular|general|mental",
                  "intensityHint":1-4,
                  "isSensationBearing":false
                }
              ],
              "summary":{
                "chiefComplaint":"English only — primary reason for visit",
                "historyOfPresentIllness":"English only — chronological clinical narrative",
                "mentals":[],
                "generals":[],
                "modalities":[],
                "particulars":[],
                "redFlags":[]
              },
              "detectedLanguage":"ISO code e.g. en, mr, hi"
            }

            Accuracy rules (critical):
            - Never invent symptoms, modalities, or history not supported by the transcript.
            - Extract EVERY distinct symptom, concomitant, modality, mental, and general mentioned.
            - Preserve clinical specificity (location, side, timing, before/after, aggravation/amelioration).
            - Ignore filler/repetition ("Yes. Yes. Yes.") and role-attribution noise from translation.
            - Doctor questions are NOT patient symptoms. Patient saying "No" negates that symptom.
            - Do NOT invent diagnoses. Keep the patient's word "fit" as "fit" unless the transcript
              explicitly says epilepsy / convulsion / seizure. Never auto-add those as searchTerms.
            - Do NOT invent organs or locations (e.g. feet fear must not become chest/heart/convulsion).
            - ALWAYS extract when present: talking in sleep; desire for salt; desire for meat/mutton;
              thirst / drinks large quantities of water; increased sexual desire;
              fear of heights / high places; dropping things / awkwardness; fear before fit;
              aura/vibration before fit; face red with anger; anger before fit.
            - If Whisper likely said "feet" but clinical context is clearly epileptic "fit", you may note
              both in searchTerms as "fit" and "feet" — still do NOT add epilepsy/convulsion unless spoken.
            - symptom.phrase: use concise English reflecting what the patient actually said.
            - searchTerms: 2-6 short English keywords drawn from the patient's language for repertory lookup
              (examples: fear, fit, vibration, hands, thirst, salt, sleep talking, sexual desire, awkward, drops).
            - isSensationBearing: true when the symptom is a sensation, emotion, or idiomatic bodily feeling
              (burning, tingling, crawling ants, fear, grief, anxiety, "as if" sensations). False for plain
              factual history without sensory/emotional quality.
            - summary.chiefComplaint: single clearest presenting complaint.
            - summary.particulars: list each local/particular symptom separately.
            - summary.modalities: all aggravations and ameliorations.
            - summary.mentals: fears, anxieties, irritability, delusions, etc.
            - conversation: include every exchange that contains clinical information; omit greetings/small talk only.
            """;

        var requestBody = new
        {
            model = _openAiOptions.ChatModel,
            temperature = 0.1,
            max_tokens = 8192,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Transcript:\n\n{transcript}" },
            },
        };

        var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
        var sw = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return (false, null, requestJson, responseBody, 0, 0, (int)sw.ElapsedMilliseconds, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var promptTokens = doc.RootElement.TryGetProperty("usage", out var usage)
                && usage.TryGetProperty("prompt_tokens", out var pt)
                ? pt.GetInt32()
                : 0;
            var completionTokens = usage.ValueKind != JsonValueKind.Undefined
                && usage.TryGetProperty("completion_tokens", out var ct)
                ? ct.GetInt32()
                : 0;

            var extraction = JsonSerializer.Deserialize<AudioCaseExtractionModel>(content ?? "{}", JsonOptions)
                ?? BuildMockExtraction(transcript);

            // Whisper transcript is the source of truth — never overwrite from GPT output.
            extraction.EnglishTranscript = transcript.Trim();

            return (true, extraction, requestJson, responseBody, promptTokens, completionTokens, (int)sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GPT extraction failed.");
            return (false, null, requestJson, null, 0, 0, (int)sw.ElapsedMilliseconds, ex.Message);
        }
    }

    public async Task<(bool Success, List<AudioCaseSuggestedRubricModel> Rubrics, string? RequestJson, string? ResponseJson, int PromptTokens, int CompletionTokens, int LatencyMs, string? Error)>
        SuggestAiRubricsAsync(
            IReadOnlyList<AudioCaseSymptomModel> symptoms,
            AudioCaseSummaryModel? summary,
            IReadOnlyList<string> existingDbRubricNames,
            int maxCount,
            CancellationToken cancellationToken)
    {
        if (maxCount <= 0)
        {
            return (true, new List<AudioCaseSuggestedRubricModel>(), null, null, 0, 0, 0, null);
        }

        if (!IsConfigured)
        {
            if (!_audioOptions.UseMockWhenNoApiKey)
            {
                return (false, new List<AudioCaseSuggestedRubricModel>(), null, null, 0, 0, 0, "OpenAI API key is not configured.");
            }

            return (true, BuildMockAiRubrics(symptoms, maxCount), null, null, 0, 0, 0, null);
        }

        var symptomLines = string.Join("\n", symptoms
            .Where(s => !string.IsNullOrWhiteSpace(s.Phrase))
            .Select(s => $"- {s.Phrase} ({s.Category}, intensity {s.IntensityHint})"));

        var existingLines = existingDbRubricNames.Count == 0
            ? "None"
            : string.Join("\n", existingDbRubricNames.Select(n => $"- {n}"));

        var summaryText = summary == null
            ? "N/A"
            : $"""
               Chief complaint: {summary.ChiefComplaint}
               HPI: {summary.HistoryOfPresentIllness}
               Particulars: {string.Join("; ", summary.Particulars ?? [])}
               Modalities: {string.Join("; ", summary.Modalities ?? [])}
               """;

        var systemPrompt = """
            You are a homeopathic repertory assistant. Suggest English rubric names in standard repertory style
            (e.g. "GENITALIA - ERUPTIONS, fungal", "SKIN - ERUPTIONS, itching") for symptoms NOT already covered
            by the database rubrics list. These are AI suggestions only — they may not exist in the user's database.

            Return strict JSON only:
            {
              "rubrics":[
                {
                  "rubricName":"SECTION - SYMPTOM, modality",
                  "sectionHint":"GENITALIA|SKIN|MIND|etc",
                  "matchedFrom":"symptom phrase from case",
                  "suggestedIntensityNo":1-4,
                  "reason":"brief clinical reason"
                }
              ]
            }

            Rules:
            - English only, repertory-style naming.
            - Do not duplicate rubrics already in the database list.
            - Suggest only clinically supported rubrics from the case.
            - Max rubrics as requested in the user message.
            """;

        var userPrompt = $"""
            Extracted symptoms:
            {symptomLines}

            Case summary:
            {summaryText}

            Database rubrics already matched (do NOT repeat these):
            {existingLines}

            Suggest up to {maxCount} additional AI rubrics for symptoms poorly covered by the database list.
            """;

        var requestBody = new
        {
            model = _openAiOptions.ChatModel,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
        };

        var requestJson = JsonSerializer.Serialize(requestBody, JsonOptions);
        var sw = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient("OpenAI");
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiOptions.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return (false, new List<AudioCaseSuggestedRubricModel>(), requestJson, responseBody, 0, 0, (int)sw.ElapsedMilliseconds, responseBody);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            var promptTokens = doc.RootElement.TryGetProperty("usage", out var usage)
                && usage.TryGetProperty("prompt_tokens", out var pt)
                ? pt.GetInt32()
                : 0;
            var completionTokens = usage.ValueKind != JsonValueKind.Undefined
                && usage.TryGetProperty("completion_tokens", out var ct)
                ? ct.GetInt32()
                : 0;

            var parsed = JsonSerializer.Deserialize<AudioCaseAiSuggestedRubricsModel>(content ?? "{}", JsonOptions)
                ?? new AudioCaseAiSuggestedRubricsModel();

            var aiRubrics = MapAiRubrics(parsed.Rubrics, maxCount);
            return (true, aiRubrics, requestJson, responseBody, promptTokens, completionTokens, (int)sw.ElapsedMilliseconds, null);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "GPT AI rubric suggestion failed.");
            return (false, new List<AudioCaseSuggestedRubricModel>(), requestJson, null, 0, 0, (int)sw.ElapsedMilliseconds, ex.Message);
        }
    }

    private static List<AudioCaseSuggestedRubricModel> MapAiRubrics(
        List<AudioCaseAiSuggestedRubricItemModel>? items,
        int maxCount)
    {
        var result = new List<AudioCaseSuggestedRubricModel>();
        if (items == null) return result;

        var aiId = -1;
        foreach (var item in items.Where(i => !string.IsNullOrWhiteSpace(i.RubricName)).Take(maxCount))
        {
            result.Add(new AudioCaseSuggestedRubricModel
            {
                SubSectionId = aiId,
                SubSectionName = item.RubricName.Trim(),
                MatchScore = 0,
                SuggestedIntensityNo = Math.Clamp(item.SuggestedIntensityNo, 1, 4),
                MatchedFrom = item.MatchedFrom ?? item.Reason,
                RemedyCountForSort = 0,
                IsAiSuggested = true,
                MatchSource = "AiGenerated",
            });
            aiId--;
        }

        return result;
    }

    private static List<AudioCaseSuggestedRubricModel> BuildMockAiRubrics(
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        int maxCount)
    {
        var result = new List<AudioCaseSuggestedRubricModel>();
        var aiId = -1;
        foreach (var symptom in symptoms.Where(s => !string.IsNullOrWhiteSpace(s.Phrase)).Take(maxCount))
        {
            result.Add(new AudioCaseSuggestedRubricModel
            {
                SubSectionId = aiId,
                SubSectionName = $"AI - {symptom.Phrase.ToUpperInvariant()}",
                MatchScore = 0,
                SuggestedIntensityNo = symptom.IntensityHint,
                MatchedFrom = symptom.Phrase,
                IsAiSuggested = true,
                MatchSource = "AiGenerated",
            });
            aiId--;
        }

        return result;
    }

    public static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static string ComputeSha256File(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var bytes = SHA256.HashData(stream);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static decimal ComputeTextSimilarity(string left, string right)
    {
        var a = Tokenize(left);
        var b = Tokenize(right);
        if (a.Count == 0 || b.Count == 0) return 0m;
        var intersection = a.Intersect(b).Count();
        var union = a.Union(b).Count();
        return union == 0 ? 0m : Math.Round((decimal)intersection / union, 4);
    }

    private static HashSet<string> Tokenize(string value) =>
        new(
            (value ?? string.Empty).ToUpperInvariant()
                .Split(new[] { ' ', '-', ',', '.', '/', '\\' }, StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);

    private string TrimTranscriptForExtraction(string transcript)
    {
        var max = Math.Clamp(_audioOptions.ExtractionMaxTranscriptChars, 4000, 80_000);
        if (transcript.Length <= max)
            return transcript;

        _logger.LogInformation(
            "GPT extraction transcript trimmed from {OriginalChars} to {MaxChars} characters (content not logged).",
            transcript.Length,
            max);
        return transcript[..max] + "\n[transcript truncated for extraction]";
    }

    private static AudioCaseExtractionModel BuildMockExtraction(string transcript)
    {
        return new AudioCaseExtractionModel
        {
            EnglishTranscript = transcript.Trim(),
            DetectedLanguage = "en",
            Conversation =
            [
                new AudioCaseMessageModel { Role = "doctor", Text = "Since when do you have this headache?", Timestamp = "00:00:08" },
                new AudioCaseMessageModel { Role = "patient", Text = "About three days. It is worse in the morning.", Timestamp = "00:00:22" },
                new AudioCaseMessageModel { Role = "doctor", Text = "Any nausea?", Timestamp = "00:00:35" },
                new AudioCaseMessageModel { Role = "patient", Text = "Yes, with the headache.", Timestamp = "00:00:48" },
            ],
            Symptoms =
            [
                new AudioCaseSymptomModel
                {
                    Phrase = "headache worse morning",
                    SearchTerms = ["headache", "morning", "pain"],
                    Category = "particular",
                    IntensityHint = 3,
                },
                new AudioCaseSymptomModel
                {
                    Phrase = "nausea",
                    SearchTerms = ["nausea", "stomach"],
                    Category = "particular",
                    IntensityHint = 2,
                },
            ],
            Summary = new AudioCaseSummaryModel
            {
                ChiefComplaint = "Headache for 3 days",
                HistoryOfPresentIllness = transcript.Length > 240 ? transcript[..240] + "..." : transcript,
                Modalities = ["Worse in the morning"],
                Particulars = ["Headache with nausea"],
            },
        };
    }
}
