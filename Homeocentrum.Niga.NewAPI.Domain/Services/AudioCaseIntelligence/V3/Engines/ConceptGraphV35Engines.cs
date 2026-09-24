using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

public static class ConceptGraphTierHelper
{
    public static string ResolveTier(decimal confidence) =>
        confidence >= 0.90m ? "Tier1"
        : confidence >= 0.75m ? "Tier2"
        : confidence >= 0.60m ? "Tier3"
        : "BelowThreshold";

    public static string ResolveRubricTierLabel(decimal confidence) =>
        confidence >= 0.90m ? "Primary"
        : confidence >= 0.75m ? "Strong"
        : confidence >= 0.60m ? "Supporting"
        : "Confirmatory";

    public static decimal ClampConfidence(decimal value) =>
        Math.Clamp(value <= 0 ? 0.70m : value, 0m, 1m);

    public static List<PatientMeaningNodeModel> MergeMeanings(
        IReadOnlyList<PatientMeaningNodeModel> primary,
        IReadOnlyList<PatientMeaningNodeModel> expanded,
        IReadOnlyList<SymptomBlockNodeModel> blocks)
    {
        var merged = new List<PatientMeaningNodeModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(PatientMeaningNodeModel m)
        {
            var key = $"{m.RawStatement}|{m.NormalizedMeaning}".ToLowerInvariant();
            if (!seen.Add(key)) return;
            m.SequenceOrder = merged.Count + 1;
            merged.Add(m);
        }

        foreach (var m in primary) Add(m);
        foreach (var m in expanded) Add(m);

        foreach (var block in blocks)
        {
            if (string.IsNullOrWhiteSpace(block.TranscriptSpan)) continue;
            var key = block.TranscriptSpan.ToLowerInvariant();
            if (seen.Contains(key)) continue;
            Add(new PatientMeaningNodeModel
            {
                RawStatement = block.TranscriptSpan.Trim(),
                NormalizedMeaning = block.TranscriptSpan.Trim(),
                LanguageCode = "en",
                Confidence = block.Confidence,
                SymptomCategory = block.CategoryHint,
                ModelVersion = "v3.5-block",
            });
        }

        return merged;
    }

    /// <summary>Maps GPT/clinical concept labels to bootstrap homeopathic pattern names.</summary>
    public static bool ConceptNamesMatch(string conceptName, string patternName)
    {
        if (string.IsNullOrWhiteSpace(conceptName) || string.IsNullOrWhiteSpace(patternName))
            return false;

        var a = conceptName.Trim();
        var b = patternName.Trim();
        if (a.Equals(b, StringComparison.OrdinalIgnoreCase)) return true;
        if (a.Contains(b, StringComparison.OrdinalIgnoreCase)) return true;
        if (b.Contains(a, StringComparison.OrdinalIgnoreCase)) return true;

        foreach (var group in ConceptAliasGroups)
        {
            if (!group.Contains(a) && !group.Contains(b)) continue;
            if (group.Contains(a) && group.Contains(b)) return true;
            if (group.Contains(a) && group.Any(alias => b.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                return true;
            if (group.Contains(b) && group.Any(alias => a.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    private static readonly string[][] ConceptAliasGroups =
    {
        new[] { "Prodromal Sensation", "Shock Sensation", "Vibration Sensation", "Convulsion Aura", "Epileptic Aura" },
        new[] { "Shaking Sensation", "Shock Sensation", "Vibration Sensation", "Trembling" },
        new[] { "Anticipatory Anxiety", "Anticipatory Fear", "Anticipatory Fear of Seizure", "Fear Before Convulsion", "Epileptic Anticipation", "Fear Of Convulsions" },
        new[] { "Fear of Heights", "Fear Of Heights", "Fear Of High Places", "Vertigo Heights" },
        new[] { "Fear from Emotional States", "Fear When Angry", "Ailments From Anger", "Anger", "Fear From Irritation" },
        new[] { "Desire for Sweets", "Desire For Sweets", "Craving Sweets", "Desire Sweets" },
        new[] { "Talking in Sleep", "Sleep Talking", "Somnambulism", "Sleep Walking" },
        new[] { "Forgetfulness", "Memory Weakness", "Absent Minded" },
        new[] { "Sexual Desire", "Sexual Desire Increased", "Increased Sexual Desire" },
    };
}

public class CaseDecompositionEngine : ICaseDecompositionEngine
{
    public const string ModelId = "v3.5-m0";
    public const string StageName = "CaseDecomposition";

    private const string SystemPrompt = """
        You are Model M0 — Case Decomposition Engine for homeopathic case taking.
        Split the transcript into independent symptom blocks. Never merge unrelated domains.
        Output strict JSON:
        {
          "blocks":[
            {"blockOrder":1,"transcriptSpan":"exact quote","categoryHint":"MentalGeneral|PhysicalGeneral|Particular|Modality|Concomitant|Dream|Food|Sleep|Sexual|Etiology|Convulsion|General","blockType":"optional label","confidence":0.0-1.0}
          ]
        }
        Rules:
        - Extract ALL symptom blocks: mental, physical, food desires/aversions, sleep, dreams, sexual, modalities, causation, convulsion/epilepsy.
        - transcriptSpan must be faithful to transcript — do not invent.
        - One block per distinct symptom expression.
        - categoryHint must be one of the listed values.
        """;

    private readonly IIntelligenceGptClient _gptClient;
    private readonly ILogger<CaseDecompositionEngine> _logger;

    public CaseDecompositionEngine(IIntelligenceGptClient gptClient, ILogger<CaseDecompositionEngine> logger)
    {
        _gptClient = gptClient;
        _logger = logger;
    }

    public async Task<List<SymptomBlockNodeModel>> DecomposeAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return new List<SymptomBlockNodeModel>();

        var gpt = await _gptClient.CompleteJsonAsync<CaseDecompositionGptModel>(
            SystemPrompt, $"Transcript:\n{transcript}", StageName, cancellationToken);

        if (!gpt.Success || gpt.Result?.Blocks == null || gpt.Result.Blocks.Count == 0)
        {
            _logger.LogWarning("Case decomposition fallback to sentence split: {Error}", gpt.Error);
            return FallbackSplit(transcript);
        }

        return gpt.Result.Blocks
            .Where(b => !string.IsNullOrWhiteSpace(b.TranscriptSpan))
            .OrderBy(b => b.BlockOrder)
            .Select(b => new SymptomBlockNodeModel
            {
                BlockOrder = b.BlockOrder > 0 ? b.BlockOrder : 1,
                TranscriptSpan = b.TranscriptSpan!.Trim(),
                CategoryHint = NormalizeCategory(b.CategoryHint),
                BlockType = b.BlockType,
                Confidence = ConceptGraphTierHelper.ClampConfidence(b.Confidence),
                ModelVersion = ModelId,
            }).ToList();
    }

    private static List<SymptomBlockNodeModel> FallbackSplit(string transcript) =>
        transcript.Split(new[] { '.', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select((s, i) => s.Trim())
            .Where(s => s.Length >= 8)
            .Select((s, i) => new SymptomBlockNodeModel
            {
                BlockOrder = i + 1,
                TranscriptSpan = s,
                CategoryHint = "General",
                Confidence = 0.75m,
                ModelVersion = ModelId,
            }).ToList();

    private static string NormalizeCategory(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint)) return "General";
        var v = hint.Trim();
        return v switch
        {
            var x when x.Contains("Mental", StringComparison.OrdinalIgnoreCase) => "MentalGeneral",
            var x when x.Contains("Food", StringComparison.OrdinalIgnoreCase) => "Food",
            var x when x.Contains("Sleep", StringComparison.OrdinalIgnoreCase) => "Sleep",
            var x when x.Contains("Sexual", StringComparison.OrdinalIgnoreCase) => "Sexual",
            var x when x.Contains("Dream", StringComparison.OrdinalIgnoreCase) => "Dream",
            var x when x.Contains("Convuls", StringComparison.OrdinalIgnoreCase) => "Convulsion",
            var x when x.Contains("Modality", StringComparison.OrdinalIgnoreCase) => "Modality",
            var x when x.Contains("Concomitant", StringComparison.OrdinalIgnoreCase) => "Concomitant",
            var x when x.Contains("Etiology", StringComparison.OrdinalIgnoreCase) => "Etiology",
            _ => v,
        };
    }
}

public class MultiSymptomDiscoveryEngine : IMultiSymptomDiscoveryEngine
{
    public const string ModelId = "v3.5-m1b";
    public const string StageName = "MultiSymptomDiscovery";

    private const string SystemPrompt = """
        You are Model M1b — Multi-Symptom Discovery Engine.
        For each patient meaning, extract ALL atomic clinical symptoms (2-5 per compound statement).
        Output strict JSON:
        {
          "expansions":[
            {"parentMeaningIndex":1,"rawStatement":"quote","normalizedMeaning":"English clinical meaning","symptomCategory":"MentalGeneral|PhysicalGeneral|Particular|Modality|Concomitant|Dream|Food|Sleep|Sexual|Etiology|Convulsion|General","confidence":0.0-1.0}
          ]
        }
        Example: "vibration before fit" → Aura, Shock Sensation, Fear Before Fit (3 expansions).
        Never stop at first match. Do not invent symptoms not in the parent meaning.
        """;

    private readonly IIntelligenceGptClient _gptClient;

    public MultiSymptomDiscoveryEngine(IIntelligenceGptClient gptClient) => _gptClient = gptClient;

    public async Task<List<PatientMeaningNodeModel>> ExpandAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default)
    {
        if (meanings.Count == 0) return new List<PatientMeaningNodeModel>();

        var lines = string.Join("\n", meanings.Select((m, i) =>
            $"{i + 1}. raw=\"{m.RawStatement}\" normalized=\"{m.NormalizedMeaning}\""));

        var gpt = await _gptClient.CompleteJsonAsync<MultiSymptomExpansionGptModel>(
            SystemPrompt, $"Meanings:\n{lines}", StageName, cancellationToken);

        if (!gpt.Success || gpt.Result?.Expansions == null || gpt.Result.Expansions.Count == 0)
            return new List<PatientMeaningNodeModel>();

        return gpt.Result.Expansions
            .Where(x => x.ParentMeaningIndex > 0 && x.ParentMeaningIndex <= meanings.Count
                && !string.IsNullOrWhiteSpace(x.NormalizedMeaning))
            .Select(x =>
            {
                var parent = meanings[x.ParentMeaningIndex - 1];
                return new PatientMeaningNodeModel
                {
                    RawStatement = (x.RawStatement ?? x.NormalizedMeaning ?? parent.RawStatement).Trim(),
                    NormalizedMeaning = x.NormalizedMeaning!.Trim(),
                    LanguageCode = parent.LanguageCode,
                    Confidence = ConceptGraphTierHelper.ClampConfidence(x.Confidence),
                    SymptomCategory = x.SymptomCategory ?? parent.SymptomCategory,
                    ParentMeaningId = parent.PatientMeaningId,
                    ModelVersion = ModelId,
                };
            }).ToList();
    }
}

public class CategoryDiscoveryEngine : ICategoryDiscoveryEngine
{
    public const string ModelId = "v3.5-m3a";
    public const string StageName = "CategoryDiscovery";

    private const string SystemPrompt = """
        Classify each patient meaning into exactly one homeopathic category.
        Output strict JSON: {"categories":[{"meaningIndex":1,"symptomCategory":"MentalGeneral|PhysicalGeneral|Particular|Modality|Concomitant|Dream|Food|Sleep|Sexual|Etiology|Convulsion|General","confidence":0.0-1.0}]}
        Categories are parallel — do not suppress any.
        """;

    private readonly IIntelligenceGptClient _gptClient;

    public CategoryDiscoveryEngine(IIntelligenceGptClient gptClient) => _gptClient = gptClient;

    public async Task ApplyCategoriesAsync(
        IList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default)
    {
        if (meanings.Count == 0) return;

        var needsCategory = meanings.Where(m => string.IsNullOrWhiteSpace(m.SymptomCategory)).ToList();
        if (needsCategory.Count == 0) return;

        var lines = string.Join("\n", meanings.Select((m, i) => $"{i + 1}. {m.NormalizedMeaning}"));
        var gpt = await _gptClient.CompleteJsonAsync<CategoryDiscoveryGptModel>(
            SystemPrompt, $"Meanings:\n{lines}", StageName, cancellationToken);

        if (!gpt.Success || gpt.Result?.Categories == null) return;

        foreach (var cat in gpt.Result.Categories)
        {
            if (cat.MeaningIndex <= 0 || cat.MeaningIndex > meanings.Count) continue;
            if (string.IsNullOrWhiteSpace(cat.SymptomCategory)) continue;
            meanings[cat.MeaningIndex - 1].SymptomCategory ??= cat.SymptomCategory.Trim();
        }
    }
}

public class RecallExpansionEngine : IRecallExpansionEngine
{
    public const string ModelId = "v3.5-m4b";
    public const string StageName = "RecallExpansion";

    private const string SystemPrompt = """
        You are Model M4b — Recall Expansion Engine. Think like a senior classical homeopath.
        For EACH clinical concept, generate ALL supported homeopathic concepts (1-5), not just the strongest.
        Output strict JSON:
        {
          "concepts":[
            {"clinicalConceptIndex":1,"conceptName":"Fear Before Convulsion","importance":"High","symptomClass":"MentalGeneral","isSRP":false,"confidence":0.0-1.0,"evidenceSpan":"exact transcript words"}
          ]
        }
        Example: "fear of vibration before fit" → Fear Before Convulsion, Anticipatory Anxiety, Fear Of Convulsions, Epileptic Anticipation.
        Never output rubric names. evidenceSpan must exist in transcript.
        """;

    private readonly IIntelligenceGptClient _gptClient;

    public RecallExpansionEngine(IIntelligenceGptClient gptClient) => _gptClient = gptClient;

    public async Task<List<HomeopathicConceptNodeModel>> ExpandAsync(
        IReadOnlyList<ClinicalConceptNodeModel> clinicalConcepts,
        IReadOnlyList<HomeopathicConceptNodeModel> baseConcepts,
        string transcript,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default)
    {
        if (clinicalConcepts.Count == 0) return baseConcepts.ToList();

        var lines = string.Join("\n", clinicalConcepts.Select((c, i) =>
            $"{i + 1}. {c.ConceptName} ({c.Domain}) category={c.SymptomCategory ?? "General"}"));

        var gpt = await _gptClient.CompleteJsonAsync<RecallExpansionGptModel>(
            SystemPrompt,
            $"Chief complaint: {summary?.ChiefComplaint ?? "N/A"}\nTranscript excerpt:\n{transcript[..Math.Min(transcript.Length, 8000)]}\nClinical concepts:\n{lines}",
            StageName,
            cancellationToken);

        var result = baseConcepts.ToList();
        if (!gpt.Success || gpt.Result?.Concepts == null || gpt.Result.Concepts.Count == 0)
            return result;

        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var concept in result)
        {
            existing.Add($"{concept.ClinicalConceptIndex}|{concept.ConceptName.Trim()}");
        }

        foreach (var item in gpt.Result.Concepts)
        {
            if (item.ClinicalConceptIndex <= 0 || item.ClinicalConceptIndex > clinicalConcepts.Count) continue;
            if (string.IsNullOrWhiteSpace(item.ConceptName)) continue;

            var clinicalIndex = item.ClinicalConceptIndex - 1;
            var dedupeKey = $"{clinicalIndex}|{item.ConceptName.Trim()}";
            if (!existing.Add(dedupeKey)) continue;

            var clinical = clinicalConcepts[clinicalIndex];
            if (!string.IsNullOrWhiteSpace(item.EvidenceSpan)
                && !transcript.Contains(item.EvidenceSpan, StringComparison.OrdinalIgnoreCase)
                && !clinical.ConceptName.Contains(item.EvidenceSpan, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(new HomeopathicConceptNodeModel
            {
                ClinicalConceptId = clinical.ClinicalConceptId,
                ClinicalConceptIndex = clinicalIndex,
                ConceptName = item.ConceptName.Trim(),
                Importance = item.Importance ?? "Medium",
                SymptomClass = item.SymptomClass ?? clinical.SymptomCategory ?? "Particular",
                IsSRP = item.IsSRP,
                Weight = ResolveWeight(item.Importance, item.IsSRP, item.SymptomClass),
                Confidence = ConceptGraphTierHelper.ClampConfidence(item.Confidence),
                EvidenceSpan = item.EvidenceSpan,
                ModelVersion = ModelId,
            });
        }

        return result;
    }

    public async Task<List<HomeopathicConceptNodeModel>> ExpandBlockAsync(
        SymptomBlockNodeModel block,
        string transcript,
        CancellationToken cancellationToken = default)
    {
        var clinical = new ClinicalConceptNodeModel
        {
            ConceptName = block.TranscriptSpan,
            Domain = block.CategoryHint,
            SymptomCategory = block.CategoryHint,
            Confidence = block.Confidence,
            ModelVersion = "v3.5-missing",
        };

        return await ExpandAsync(
            new[] { clinical },
            new List<HomeopathicConceptNodeModel>(),
            transcript,
            null,
            cancellationToken);
    }

    private static decimal ResolveWeight(string? importance, bool isSrp, string? symptomClass)
    {
        if (isSrp || string.Equals(symptomClass, "SRP", StringComparison.OrdinalIgnoreCase)) return 3.0m;
        if (string.Equals(importance, "High", StringComparison.OrdinalIgnoreCase)) return 2.5m;
        if (string.Equals(symptomClass, "MentalGeneral", StringComparison.OrdinalIgnoreCase)) return 2.0m;
        if (string.Equals(importance, "Low", StringComparison.OrdinalIgnoreCase)) return 0.8m;
        return 1.5m;
    }
}

public class ConceptClusterEngine : IConceptClusterEngine
{
    public List<ConceptClusterNodeModel> BuildClusters(IReadOnlyList<HomeopathicConceptNodeModel> concepts)
    {
        var clusters = new List<ConceptClusterNodeModel>();
        var grouped = concepts
            .GroupBy(c => c.ClinicalConceptId ?? 0)
            .Where(g => g.Key > 0 || g.Count() > 0);

        foreach (var group in grouped)
        {
            var label = group.First().ConceptName;
            clusters.Add(new ConceptClusterNodeModel
            {
                ClusterLabel = label,
                HomeopathicConceptIds = group.Select(c => c.HomeopathicConceptId ?? 0).Where(id => id > 0).ToList(),
            });
        }

        if (clusters.Count == 0 && concepts.Count > 0)
        {
            clusters.Add(new ConceptClusterNodeModel
            {
                ClusterLabel = "CaseCluster",
                HomeopathicConceptIds = concepts.Select(c => c.HomeopathicConceptId ?? 0).Where(id => id > 0).ToList(),
            });
        }

        return clusters;
    }
}

public class TranscriptCoverageEngine : ITranscriptCoverageEngine
{
    public CaseCoverageMetricsModel Measure(
        IReadOnlyList<SymptomBlockNodeModel> blocks,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        IReadOnlyList<PatientMeaningNodeModel> meanings)
    {
        if (blocks.Count == 0)
        {
            return new CaseCoverageMetricsModel
            {
                TranscriptCoverage = meanings.Count > 0 && acceptedRubrics.Count > 0 ? 1m : 0m,
                TotalBlocks = meanings.Count,
                CoveredBlocks = acceptedRubrics.Count > 0 ? meanings.Count : 0,
            };
        }

        var covered = 0;
        var uncovered = new List<string>();

        foreach (var block in blocks)
        {
            var isCovered = acceptedRubrics.Any(r =>
                BlockMatchesRubric(block, r, meanings));
            block.IsCovered = isCovered;
            if (isCovered) covered++;
            else uncovered.Add(block.TranscriptSpan);
        }

        return new CaseCoverageMetricsModel
        {
            TranscriptCoverage = blocks.Count == 0 ? 0m : Math.Round((decimal)covered / blocks.Count, 4),
            TotalBlocks = blocks.Count,
            CoveredBlocks = covered,
            UncoveredSpans = uncovered,
            MissingSymptomCount = uncovered.Count,
        };
    }

    private static bool BlockMatchesRubric(
        SymptomBlockNodeModel block,
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<PatientMeaningNodeModel> meanings)
    {
        var blockText = block.TranscriptSpan;
        if (string.IsNullOrWhiteSpace(blockText)) return false;

        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom)
            && blockText.Contains(rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase))
            return true;

        if (rubric.EvidenceChain?.TranscriptExcerpt != null
            && blockText.Contains(rubric.EvidenceChain.TranscriptExcerpt, StringComparison.OrdinalIgnoreCase))
            return true;

        var rubricTail = rubric.SubSectionName.Contains('-')
            ? rubric.SubSectionName[(rubric.SubSectionName.LastIndexOf('-') + 1)..]
            : rubric.SubSectionName;

        var similarity = AudioCaseAiProcessor.ComputeTextSimilarity(blockText, rubricTail);
        if (similarity >= 0.35m) return true;

        return meanings.Any(m =>
            blockText.Contains(m.RawStatement, StringComparison.OrdinalIgnoreCase)
            && (rubric.WhySuggested?.Contains(m.NormalizedMeaning, StringComparison.OrdinalIgnoreCase) == true
                || AudioCaseAiProcessor.ComputeTextSimilarity(m.NormalizedMeaning, rubricTail) >= 0.35m));
    }
}

public class MissingSymptomDetector : IMissingSymptomDetector
{
    private readonly IRecallExpansionEngine _recallEngine;
    private readonly IRubricDiscoveryEngineV3 _discoveryEngine;
    private readonly IConceptGraphEvidenceEngine _evidenceEngine;

    public MissingSymptomDetector(
        IRecallExpansionEngine recallEngine,
        IRubricDiscoveryEngineV3 discoveryEngine,
        IConceptGraphEvidenceEngine evidenceEngine)
    {
        _recallEngine = recallEngine;
        _discoveryEngine = discoveryEngine;
        _evidenceEngine = evidenceEngine;
    }

    public async Task<MissingSymptomPassResult> RunSecondPassAsync(
        ConceptGraphFullModel graph,
        string transcript,
        CaseCoverageMetricsModel coverage,
        CancellationToken cancellationToken = default)
    {
        var result = new MissingSymptomPassResult();
        if (coverage.UncoveredSpans.Count == 0) return result;

        var uncoveredBlocks = graph.SymptomBlocks
            .Where(b => !b.IsCovered)
            .ToList();

        foreach (var block in uncoveredBlocks)
        {
            var expanded = await _recallEngine.ExpandBlockAsync(block, transcript, cancellationToken);
            if (expanded.Count == 0) continue;

            graph.HomeopathicConcepts.AddRange(expanded);
            var discoveries = await _discoveryEngine.DiscoverAsync(expanded, cancellationToken);
            discoveries = _evidenceEngine.BuildEvidenceChains(discoveries, graph);
            result.AdditionalDiscoveries.AddRange(discoveries);
            result.ResolvedBlockCount++;
        }

        return result;
    }
}

public class CaseCompletenessScoringEngine : ICaseCompletenessScoringEngine
{
    private static readonly Dictionary<string, decimal> CategoryWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["MentalGeneral"] = 0.15m,
        ["PhysicalGeneral"] = 0.15m,
        ["Food"] = 0.10m,
        ["Sleep"] = 0.10m,
        ["Sexual"] = 0.10m,
        ["Convulsion"] = 0.15m,
        ["Particular"] = 0.15m,
        ["Modality"] = 0.05m,
        ["Concomitant"] = 0.05m,
    };

    public decimal Score(
        IReadOnlyList<SymptomBlockNodeModel> blocks,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        CaseCoverageMetricsModel coverage)
    {
        if (blocks.Count == 0)
            return coverage.TranscriptCoverage;

        var presentCategories = blocks
            .Select(b => b.CategoryHint)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (presentCategories.Count == 0)
            return coverage.TranscriptCoverage;

        decimal weightedSum = 0m;
        decimal totalWeight = 0m;

        foreach (var category in presentCategories)
        {
            var weight = CategoryWeights.GetValueOrDefault(category, 0.08m);
            totalWeight += weight;
            var categoryBlocks = blocks.Where(b =>
                string.Equals(b.CategoryHint, category, StringComparison.OrdinalIgnoreCase)).ToList();
            var categoryCovered = categoryBlocks.Count(b => b.IsCovered);
            var categoryRate = categoryBlocks.Count == 0 ? 0m : (decimal)categoryCovered / categoryBlocks.Count;
            weightedSum += weight * categoryRate;
        }

        var completeness = totalWeight == 0 ? coverage.TranscriptCoverage : weightedSum / totalWeight;
        return Math.Round(Math.Max(completeness, coverage.TranscriptCoverage), 4);
    }
}
