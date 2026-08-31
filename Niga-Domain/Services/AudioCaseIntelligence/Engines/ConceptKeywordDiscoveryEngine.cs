using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public interface IConceptKeywordDiscoveryEngine
{
    Task<ConceptKeywordDiscoveryBatchResult> DiscoverAsync(
        Guid sessionId,
        string? correlationId,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics,
        CancellationToken cancellationToken = default);
}

public sealed class ConceptKeywordDiscoveryBatchResult
{
    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public List<ConceptDiscoveryTraceModel> Traces { get; set; } = new();
}

public sealed class ConceptDiscoveryTraceModel
{
    public Guid ConceptId { get; set; }

    public string ConceptText { get; set; } = string.Empty;

    public string? Category { get; set; }

    public bool IsSrp { get; set; }

    public bool SearchAttempted { get; set; }

    public List<string> SearchTermsUsed { get; set; } = new();

    public int RawCandidateCount { get; set; }

    public int KeptCandidateCount { get; set; }

    public string Outcome { get; set; } = string.Empty;

    public string? TopRubricName { get; set; }

    public decimal? TopScore { get; set; }

    public string? Error { get; set; }

    public int LatencyMs { get; set; }
}

/// <summary>
/// Per-concept SubSectionMaster keyword discovery with domain-aware scoring.
/// Ensures mental/particular/SRP concepts always get a search attempt (SRP affects weight, not eligibility).
/// Uses a DI scope per concept so EF DbContext is not shared across concurrent searches.
/// </summary>
public class ConceptKeywordDiscoveryEngine : IConceptKeywordDiscoveryEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IAudioCaseIntelligenceRepository _intelligenceRepository;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<ConceptKeywordDiscoveryEngine> _logger;

    public ConceptKeywordDiscoveryEngine(
        IServiceScopeFactory scopeFactory,
        IAudioCaseIntelligenceRepository intelligenceRepository,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<ConceptKeywordDiscoveryEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _intelligenceRepository = intelligenceRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ConceptKeywordDiscoveryBatchResult> DiscoverAsync(
        Guid sessionId,
        string? correlationId,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics,
        CancellationToken cancellationToken = default)
    {
        var result = new ConceptKeywordDiscoveryBatchResult();
        if (concepts.Count == 0)
            return result;

        var coveredIds = existingRubrics
            .Where(r => r.SubSectionId > 0)
            .Select(r => r.SubSectionId)
            .ToHashSet();

        var maxConcurrency = Math.Clamp(_options.ConceptDiscoveryMaxConcurrency, 1, 8);
        var perConceptTimeout = TimeSpan.FromSeconds(Math.Clamp(_options.ConceptDiscoveryTimeoutSeconds, 5, 60));
        using var gate = new SemaphoreSlim(maxConcurrency);

        var tasks = concepts.Select(async concept =>
        {
            // Bug 3: build terms first; empty → NoSearchTerms in <1ms (never burn timeout).
            var preTerms = ConceptSearchTermBuilder.Build(concept);
            if (preTerms.Count == 0)
            {
                return new ConceptDiscoveryUnit
                {
                    Trace = new ConceptDiscoveryTraceModel
                    {
                        ConceptId = concept.ConceptId,
                        ConceptText = concept.ClinicalMeaning ?? concept.RawStatement,
                        Category = concept.Category,
                        IsSrp = concept.IsSRP,
                        SearchAttempted = false,
                        SearchTermsUsed = preTerms,
                        Outcome = "NoSearchTerms",
                    },
                };
            }

            await gate.WaitAsync(cancellationToken);
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(perConceptTimeout);
                using var scope = _scopeFactory.CreateScope();
                var subSections = scope.ServiceProvider.GetRequiredService<ISubSectionRepository>();
                return await DiscoverOneAsync(subSections, concept, coveredIds, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new ConceptDiscoveryUnit
                {
                    Trace = new ConceptDiscoveryTraceModel
                    {
                        ConceptId = concept.ConceptId,
                        ConceptText = concept.ClinicalMeaning ?? concept.RawStatement,
                        Category = concept.Category,
                        IsSrp = concept.IsSRP,
                        SearchAttempted = true,
                        SearchTermsUsed = preTerms,
                        Outcome = "Timeout",
                        Error = $"Exceeded {perConceptTimeout.TotalSeconds:0}s per-concept timeout",
                    },
                    Rubrics = new List<AudioCaseSuggestedRubricModel>(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Per-concept keyword discovery failed for concept {ConceptId} ({Text})",
                    concept.ConceptId,
                    concept.ClinicalMeaning ?? concept.RawStatement);
                return new ConceptDiscoveryUnit
                {
                    Trace = new ConceptDiscoveryTraceModel
                    {
                        ConceptId = concept.ConceptId,
                        ConceptText = concept.ClinicalMeaning ?? concept.RawStatement,
                        Category = concept.Category,
                        IsSrp = concept.IsSRP,
                        SearchAttempted = true,
                        SearchTermsUsed = preTerms,
                        Outcome = "Exception",
                        Error = ex.Message,
                    },
                    Rubrics = new List<AudioCaseSuggestedRubricModel>(),
                };
            }
            finally
            {
                gate.Release();
            }
        });

        var units = await Task.WhenAll(tasks);
        foreach (var unit in units)
        {
            result.Traces.Add(unit.Trace);
            result.Rubrics.AddRange(unit.Rubrics);
        }

        await _intelligenceRepository.SaveIntelligenceLogAsync(
            sessionId,
            correlationId ?? sessionId.ToString("N")[..12],
            stageName: "PerConceptKeywordDiscovery",
            status: "Success",
            message: $"Concepts={concepts.Count}, withHits={result.Traces.Count(t => t.KeptCandidateCount > 0)}, rubrics={result.Rubrics.Count}",
            detailsJson: JsonSerializer.Serialize(result.Traces, JsonOptions),
            latencyMs: result.Traces.Sum(t => t.LatencyMs),
            cancellationToken);

        return result;
    }

    private async Task<ConceptDiscoveryUnit> DiscoverOneAsync(
        ISubSectionRepository subSectionRepository,
        ClinicalConceptModel concept,
        HashSet<int> coveredIds,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var text = (concept.ClinicalMeaning ?? concept.RawStatement ?? string.Empty).Trim();
        var terms = ConceptSearchTermBuilder.Build(concept);
        var domain = ConceptSearchTermBuilder.ResolveDomain(concept);

        var trace = new ConceptDiscoveryTraceModel
        {
            ConceptId = concept.ConceptId,
            ConceptText = text,
            Category = concept.Category,
            IsSrp = concept.IsSRP,
            SearchTermsUsed = terms,
            SearchAttempted = terms.Count > 0,
        };

        // Bug 3: empty terms must fail fast — never burn the 25s timeout with nothing to search.
        if (terms.Count == 0)
        {
            trace.Outcome = "NoSearchTerms";
            trace.LatencyMs = (int)sw.ElapsedMilliseconds;
            return new ConceptDiscoveryUnit { Trace = trace };
        }

        var raw = new Dictionary<int, (string Name, decimal Score, string Term)>();
        foreach (var term in terms.Take(3))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var search = await subSectionRepository.SearchSubSectionsByHotspotAsync(new SearchSubSectionByHotspotRequest
            {
                HotspotName = term,
                PageNumber = 1,
                PageSize = 8,
            });

            foreach (var item in search.Items)
            {
                if (string.IsNullOrWhiteSpace(item.SubSectionName))
                    continue;

                var score = ConceptSearchTermBuilder.ScoreCandidate(
                    concept,
                    domain,
                    term,
                    item.SubSectionName);

                // Bug A: reject substring collisions (drop→dropsy) even if hotspot SQL leaked them.
                if (!WordBoundaryMatcher.Matches(item.SubSectionName, term)
                    && WordBoundaryMatcher.IsSubstringCollision(item.SubSectionName, term))
                {
                    continue;
                }

                if (score < 0.40m)
                    continue;

                if (!raw.TryGetValue(item.SubSectionId, out var existing) || score > existing.Score)
                    raw[item.SubSectionId] = (item.SubSectionName, score, term);
            }
        }

        trace.RawCandidateCount = raw.Count;
        var kept = raw
            .Where(kv => !coveredIds.Contains(kv.Key))
            .OrderByDescending(kv => kv.Value.Score)
            .Take(3)
            .ToList();

        var rubrics = kept.Select(kv => new AudioCaseSuggestedRubricModel
        {
            SubSectionId = kv.Key,
            SubSectionName = kv.Value.Name,
            MatchScore = Math.Round(kv.Value.Score, 4),
            ConfidenceScore = Math.Round(kv.Value.Score, 4),
            SuggestedIntensityNo = concept.IsSRP ? 3 : 2,
            MatchedFrom = text,
            RemedyCountForSort = 0,
            IsAiSuggested = false,
            MatchSource = "ConceptKeyword",
            MatchLayer = "ConceptKeyword",
            EngineVersion = "v2-concept-kw",
            WhySuggested = $"Per-concept keyword hit via '{kv.Value.Term}' (domain={domain}, srp={concept.IsSRP}).",
            EvidenceChainComplete = true,
            Source = "Database",
            ResultKind = "RepertoryRubric",
            SourceConceptId = concept.ConceptId,
        }).ToList();

        trace.KeptCandidateCount = rubrics.Count;
        trace.TopRubricName = rubrics.FirstOrDefault()?.SubSectionName;
        trace.TopScore = rubrics.FirstOrDefault()?.MatchScore;
        trace.Outcome = rubrics.Count > 0
            ? "HitsKept"
            : raw.Count > 0
                ? "AllRankedOutOrAlreadyCovered"
                : "ZeroDbHits";
        trace.LatencyMs = (int)sw.ElapsedMilliseconds;

        return new ConceptDiscoveryUnit { Trace = trace, Rubrics = rubrics };
    }

    private sealed class ConceptDiscoveryUnit
    {
        public ConceptDiscoveryTraceModel Trace { get; set; } = new();
        public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();
    }
}

public static class ConceptSearchTermBuilder
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "patient", "experiences", "experience", "has", "have", "a", "an", "of", "for", "and", "or",
        "but", "does", "not", "act", "on", "it", "their", "from", "with", "before", "after", "during",
        "very", "like", "feel", "feeling", "having", "objects", "object", "preference", "eating", "over",
        "other", "meats", "related", "occurrence", "epileptic", "amount", "same", "thing", "things",
        "desire", "desires", "more", "less", "low", "high",
    };

    public static List<string> Build(ClinicalConceptModel concept)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hay = $"{concept.RawStatement} {concept.ClinicalMeaning} {concept.HomeopathicMeaning} {string.Join(' ', concept.SearchTerms)}";

        // Prefer short GPT search terms; drop stopwords and full sentences.
        foreach (var t in concept.SearchTerms.Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var trimmed = t.Trim();
            if (trimmed.Length >= 3 && trimmed.Length <= 40 && !StopWords.Contains(trimmed) && !trimmed.Contains(' '))
                terms.Add(trimmed);
            else if (trimmed.Length <= 40 && trimmed.Contains(' ') && !trimmed.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
                terms.Add(trimmed);
        }

        // Domain seeds — short, searchable anchors only (no full clinical sentences).
        if (ContainsAny(hay, "salt"))
        {
            terms.Add("salt desire");
            terms.Add("salt");
        }

        if (ContainsAny(hay, "sexual", "sex desire", "masturbat", "libido", "having sex"))
        {
            terms.Add("SEXUAL DESIRE increased");
            terms.Add("sexual desire");
        }

        if (ContainsAny(hay, "drop", "awkward", "falls from hand", "thing falls", "objects from"))
        {
            terms.Add("drops things");
            terms.Add("AWKWARD");
        }

        if (ContainsAny(hay, "talk") && ContainsAny(hay, "sleep", "dream", "night"))
        {
            terms.Add("TALKING sleep");
            terms.Add("talking sleep");
        }

        if (ContainsAny(hay, "thirst", "drink", "water", "glasses"))
        {
            if (ContainsAny(hay, "large", "lot of water", "much thirst", "more", "continuous", "8-10", "quantit"))
                terms.Add("THIRST large quantities");
            if (ContainsAny(hay, "no thirst", "thirstless", "small amount", "little water", "2-3", "two or three", "low thirst"))
                terms.Add("thirstless");
            terms.Add("thirst");
        }

        if (ContainsAny(hay, "fear") && ContainsAny(hay, "fit", "convulsion", "epilep", "seizure"))
        {
            terms.Add("FEAR convulsions");
            terms.Add("fear fit");
        }

        if (ContainsAny(hay, "high place", "height", "looking down", "3rd floor", "third floor"))
        {
            terms.Add("FEAR high places");
            terms.Add("high places");
        }

        if (ContainsAny(hay, "vibrat", "aura", "shock", "prodrom", "quiver", "trembl"))
        {
            terms.Add("aura");
            terms.Add("vibration");
            terms.Add("before convulsion");
        }

        if (ContainsAny(hay, "anger") && ContainsAny(hay, "fit", "convulsion", "red face"))
        {
            terms.Add("CONVULSIONS anger");
            terms.Add("anger after");
        }

        if (ContainsAny(hay, "mutton", "meat"))
        {
            terms.Add("mutton");
            terms.Add("meat desire");
        }

        // Strip stopwords / polluting tokens.
        terms.RemoveWhere(t => StopWords.Contains(t) || t.Length < 3 || t.Length > 48
            || t.StartsWith("The patient", StringComparison.OrdinalIgnoreCase));

        // Prefer longer / multi-word domain anchors first (short tokens like "sleep" alone cause hitchhikers + slow scans).
        return terms
            .OrderByDescending(t => t.Contains(' ') ? 2 : 0)
            .ThenByDescending(t => t.Length)
            .ThenBy(t => t, StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
    }

    public static string ResolveDomain(ClinicalConceptModel concept)
    {
        var hay = $"{concept.RawStatement} {concept.ClinicalMeaning} {concept.HomeopathicMeaning}".ToLowerInvariant();
        if (ContainsAny(hay, "thirst", "drink", "water")) return "thirst";
        if (ContainsAny(hay, "salt")) return "salt";
        if (ContainsAny(hay, "sexual", "sex ", "masturbat", "having sex")) return "sexual";
        if (ContainsAny(hay, "drop", "awkward")) return "awkward";
        if (ContainsAny(hay, "talk") && ContainsAny(hay, "sleep")) return "sleep-talk";
        if (ContainsAny(hay, "high place", "height")) return "height-fear";
        if (ContainsAny(hay, "fear") && ContainsAny(hay, "fit", "convulsion")) return "fear-fit";
        if (ContainsAny(hay, "vibrat", "aura", "quiver", "trembl")) return "aura";
        if (ContainsAny(hay, "anger") && ContainsAny(hay, "convulsion", "fit")) return "anger-convulsion";
        if (ContainsAny(hay, "mutton", "meat")) return "food";
        return concept.Category?.ToLowerInvariant() ?? "general";
    }

    public static decimal ScoreCandidate(
        ClinicalConceptModel concept,
        string domain,
        string searchTerm,
        string rubricName)
    {
        var name = rubricName ?? string.Empty;
        var upper = name.ToUpperInvariant();
        var score = 0.40m;

        // Bug A: bare substring of short term into unrelated compound → hard reject.
        if (!string.IsNullOrWhiteSpace(searchTerm)
            && WordBoundaryMatcher.IsSubstringCollision(name, searchTerm)
            && !WordBoundaryMatcher.Matches(name, searchTerm))
        {
            return 0.05m;
        }

        // Bug 2 / Bug C: hitchhiker penalty — "accompanied by X" / concomitant "thirst, with".
        if (upper.Contains("ACCOMPANIED BY") || upper.Contains("ACCOMPANIED WITH"))
            score -= 0.55m;

        // Domain gates + section-anchor boosts.
        if (domain == "thirst")
        {
            if (upper.Contains("MIND") && upper.Contains("DESIRE") && !WordBoundaryMatcher.ContainsWord(upper, "THIRST"))
                return 0.05m;
            if ((upper.Contains("ACCOMPANIED BY") || upper.Contains(", WITH") || upper.Contains("-WITH"))
                && WordBoundaryMatcher.ContainsWord(upper, "THIRST")
                && !upper.StartsWith("STOMACH"))
                score -= 0.45m;
            if (upper.StartsWith("BLADDER") || upper.StartsWith("ABDOMEN") || upper.StartsWith("RECTUM")
                || upper.StartsWith("KIDNEY") || upper.StartsWith("URETHRA"))
            {
                if (WordBoundaryMatcher.ContainsWord(upper, "THIRST"))
                    score -= 0.50m;
            }

            if (upper.StartsWith("STOMACH") && WordBoundaryMatcher.ContainsWord(upper, "THIRST"))
                score += 0.50m;
            else if (WordBoundaryMatcher.ContainsWord(upper, "THIRST")
                && (WordBoundaryMatcher.ContainsWord(upper, "LARGE")
                    || WordBoundaryMatcher.ContainsWord(upper, "THIRSTLESS")
                    || WordBoundaryMatcher.ContainsWord(upper, "WANTING")))
                score += 0.40m;
            else if (WordBoundaryMatcher.ContainsWord(upper, "THIRST"))
                score += 0.20m;
        }

        if (domain == "salt")
        {
            if (WordBoundaryMatcher.ContainsWord(upper, "SALT")
                && (upper.Contains("FOOD AND DRINKS") || upper.StartsWith("GENERAL")))
                score += 0.50m;
            else if (upper.Contains("MIND") && upper.Contains("DESIRE") && !WordBoundaryMatcher.ContainsWord(upper, "SALT"))
                return 0.10m;
            else if (upper.Contains("ACCOMPANIED BY"))
                score -= 0.30m;
        }

        if (domain == "sexual")
        {
            if ((upper.Contains("GENITALIA") || upper.Contains("GENITAL")) && upper.Contains("SEXUAL DESIRE"))
                score += 0.55m;
            else if (upper.Contains("SEXUAL DESIRE") && !upper.Contains("ACCOMPANIED BY"))
                score += 0.40m;
            else if (upper.Contains("ACCOMPANIED BY") && upper.Contains("SEXUAL"))
                score -= 0.40m;
            else
                score -= 0.15m;
        }

        // Word-boundary: "DROPS" must not boost "DROPSY".
        if (domain == "awkward"
            && (WordBoundaryMatcher.ContainsWord(upper, "AWKWARD")
                || upper.Contains("DROPS THINGS", StringComparison.Ordinal)
                || (WordBoundaryMatcher.ContainsWord(upper, "DROPS")
                    && !WordBoundaryMatcher.ContainsWord(upper, "DROPSY"))))
            score += 0.50m;
        else if (domain == "awkward" && WordBoundaryMatcher.ContainsWord(upper, "DROPSY"))
            return 0.05m;

        if (domain == "sleep-talk")
        {
            if (WordBoundaryMatcher.ContainsWord(upper, "TALK")
                && WordBoundaryMatcher.ContainsWord(upper, "SLEEP")
                && upper.StartsWith("MIND")
                && !upper.Contains("ACCOMPANIED BY"))
            {
                score += 0.55m;
            }
            else if (WordBoundaryMatcher.ContainsWord(upper, "TALK")
                && WordBoundaryMatcher.ContainsWord(upper, "SLEEP")
                && !upper.Contains("ACCOMPANIED BY"))
            {
                score += 0.45m;
            }
            else if (upper.StartsWith("ABDOMEN") || upper.StartsWith("BLADDER") || upper.StartsWith("RECTUM")
                || upper.StartsWith("CHEST") || upper.StartsWith("STOMACH"))
            {
                // "sleep" as organ modality (e.g. ABDOMEN-...-sleep agg.) is a hitchhiker for talking-in-sleep.
                score -= 0.55m;
            }
            else if (WordBoundaryMatcher.ContainsWord(upper, "SLEEP")
                && !WordBoundaryMatcher.ContainsWord(upper, "TALK")
                && !WordBoundaryMatcher.ContainsWord(upper, "TALKING"))
            {
                score -= 0.40m;
            }
        }

        if (domain == "height-fear" && WordBoundaryMatcher.ContainsWord(upper, "FEAR")
            && (WordBoundaryMatcher.ContainsWord(upper, "HIGH") || WordBoundaryMatcher.ContainsWord(upper, "HEIGHT")
                || upper.Contains("HIGH PLACES", StringComparison.Ordinal)))
            score += 0.50m;

        if (domain == "fear-fit" && WordBoundaryMatcher.ContainsWord(upper, "FEAR")
            && (WordBoundaryMatcher.ContainsWord(upper, "CONVULSION")
                || WordBoundaryMatcher.ContainsWord(upper, "EPILEP")
                || WordBoundaryMatcher.ContainsWord(upper, "FIT")))
            score += 0.45m;

        if (domain == "aura" && (WordBoundaryMatcher.ContainsWord(upper, "AURA")
            || (WordBoundaryMatcher.ContainsWord(upper, "CONVULSION") && WordBoundaryMatcher.ContainsWord(upper, "BEFORE"))
            || WordBoundaryMatcher.ContainsWord(upper, "VIBRAT")))
            score += 0.45m;

        if (domain == "anger-convulsion" && WordBoundaryMatcher.ContainsWord(upper, "CONVULSION")
            && WordBoundaryMatcher.ContainsWord(upper, "ANGER"))
            score += 0.50m;

        // Category ↔ section anchor boost.
        var category = concept.Category?.ToLowerInvariant() ?? "";
        if (category is "mental" && upper.StartsWith("MIND"))
            score += 0.12m;
        if (category is "general" && (upper.StartsWith("GENERAL") || upper.StartsWith("STOMACH")))
            score += 0.10m;
        if (category is "particular" && !upper.StartsWith("MIND") && !upper.Contains("ACCOMPANIED BY"))
            score += 0.08m;

        var conceptTokens = Tokenize($"{concept.ClinicalMeaning} {concept.RawStatement} {searchTerm}");
        var rubricTokens = Tokenize(name);
        var overlap = conceptTokens.Intersect(rubricTokens, StringComparer.OrdinalIgnoreCase).Count();
        if (overlap > 0)
            score += Math.Min(0.20m, overlap * 0.06m);

        // Bug A: only boost when searchTerm is a whole-word/phrase match, not bare Contains.
        if (!string.IsNullOrWhiteSpace(searchTerm) && WordBoundaryMatcher.Matches(name, searchTerm))
            score += 0.15m;

        return Math.Clamp(score, 0m, 0.99m);
    }

    public static void SeedSearchTermsIfEmpty(ClinicalConceptModel concept)
    {
        if (concept.SearchTerms.Count > 0)
            return;

        concept.SearchTerms = Build(concept);
    }

    public static bool DetectContradiction(string? transcriptOrMeaning)
    {
        if (string.IsNullOrWhiteSpace(transcriptOrMeaning))
            return false;

        var text = transcriptOrMeaning.ToLowerInvariant();
        var hasMore = text.Contains("more") || text.Contains("lot of water") || text.Contains("much thirst") || text.Contains("continuous");
        var hasNone = text.Contains("no thirst") || text.Contains("thirstless") || text.Contains("i have no thirst");
        var hasSmall = text.Contains("2-3") || text.Contains("two or three") || text.Contains("small amount");
        return (hasMore && hasNone) || (hasMore && hasSmall && hasNone);
    }

    private static bool ContainsAny(string hay, params string[] needles) =>
        needles.Any(n => hay.Contains(n, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split(new[] { ' ', '-', ',', ';', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3 && !StopWords.Contains(t))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
