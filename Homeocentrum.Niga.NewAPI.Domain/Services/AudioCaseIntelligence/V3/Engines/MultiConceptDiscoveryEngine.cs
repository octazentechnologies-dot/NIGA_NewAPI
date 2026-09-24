using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Learning;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

public class MultiConceptDiscoveryEngine : IMultiConceptDiscoveryEngine
{
    public const string ModelId = "v5-m5";
    public const string StageName = "MultiConceptDiscovery";

    private static readonly string[] SupportedCategories =
    [
        "Fear",
        "Forgetfulness",
        "Modality",
        "Food Desire",
        "Sleep",
        "Dream",
        "Sexual",
        "General",
        "Physical",
    ];

    private const string SystemPrompt = """
        You are Model M5 — Multi Concept Discovery Engine for classical homeopathy.
        Extract ALL supported concepts from each patient statement independently. Never stop at the strongest concept.
        Supported categories (parallel — extract every category present):
        Fear, Forgetfulness, Modality, Food Desire, Sleep, Dream, Sexual, General, Physical
        Output strict JSON:
        {
          "concepts":[
            {
              "meaningIndex":1,
              "category":"Fear|Forgetfulness|Modality|Food Desire|Sleep|Dream|Sexual|General|Physical",
              "clinicalConceptName":"Anticipatory Anxiety",
              "homeopathicConceptName":"Fear Before Convulsion",
              "importance":"High|Medium|Low",
              "symptomClass":"SRP|MentalGeneral|PhysicalGeneral|Modality|Concomitant|Particular|Food|Sleep|Dream|Sexual",
              "isSRP":false,
              "confidence":0.0-1.0,
              "evidenceSpan":"exact words from statement"
            }
          ]
        }
        Rules:
        - One patient statement may yield many concepts across different categories.
        - Same meaningIndex may appear multiple times for distinct categories/concepts.
        - Example: "fear before fit, forgets names, desires sweets, sleeps late, dreams of snakes, worse cold" →
          Fear, Forgetfulness, Food Desire, Sleep, Dream, Modality concepts (all extracted).
        - Never output rubric or repertory names.
        - evidenceSpan must be grounded in the patient text.
        """;

    private readonly IIntelligenceGptClient _gptClient;
    private readonly IDoctorLearningWeightProvider _learningWeights;
    private readonly RubricIntelligenceOptions _options;

    public MultiConceptDiscoveryEngine(
        IIntelligenceGptClient gptClient,
        IDoctorLearningWeightProvider learningWeights,
        IOptions<RubricIntelligenceOptions> options)
    {
        _gptClient = gptClient;
        _learningWeights = learningWeights;
        _options = options.Value;
    }

    public async Task<MultiConceptDiscoveryResult> DiscoverAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors,
        IReadOnlyList<SymptomBlockNodeModel> symptomBlocks,
        string transcript,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default)
    {
        if (meanings.Count == 0)
            return new MultiConceptDiscoveryResult();

        var lines = BuildMeaningLines(meanings, metaphors);
        var blockHints = symptomBlocks.Count > 0
            ? string.Join("\n", symptomBlocks.Select(b => $"- [{b.CategoryHint}] {b.TranscriptSpan}"))
            : "none";

        var gpt = await _gptClient.CompleteJsonAsync<MultiConceptDiscoveryGptModel>(
            SystemPrompt,
            $"Chief complaint: {summary?.ChiefComplaint ?? "N/A"}\nTranscript excerpt:\n{transcript[..Math.Min(transcript.Length, 8000)]}\nSymptom blocks:\n{blockHints}\nPatient meanings:\n{lines}",
            StageName,
            cancellationToken);

        var items = gpt.Success && gpt.Result?.Concepts != null && gpt.Result.Concepts.Count > 0
            ? gpt.Result.Concepts
            : BuildFallbackItems(meanings, metaphors, symptomBlocks, transcript);

        var learnedWeights = await _learningWeights.LoadAsync(cancellationToken);
        return ConceptGraphAssemblyEngine.Assemble(items, meanings, summary?.ChiefComplaint, learnedWeights, _options);
    }

    private static string BuildMeaningLines(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors)
    {
        return string.Join("\n", meanings.Select((m, i) =>
        {
            var meta = metaphors.ElementAtOrDefault(i);
            var category = m.SymptomCategory ?? "General";
            var literal = m.NormalizedMeaning;
            if (meta is { IsMetaphor: true } && !string.IsNullOrWhiteSpace(meta.ClinicalMeaning)
                && !string.Equals(meta.ClinicalMeaning, literal, StringComparison.OrdinalIgnoreCase))
            {
                return $"{i + 1}. category={category} raw=\"{m.RawStatement}\" literal=\"{literal}\" metaphor=\"{meta.ClinicalMeaning}\" isMetaphor=true";
            }

            return $"{i + 1}. category={category} raw=\"{m.RawStatement}\" literal=\"{literal}\" isMetaphor=false";
        }));
    }

    private static List<MultiConceptDiscoveryItemGptModel> BuildFallbackItems(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors,
        IReadOnlyList<SymptomBlockNodeModel> symptomBlocks,
        string transcript)
    {
        var items = new List<MultiConceptDiscoveryItemGptModel>();

        for (var i = 0; i < meanings.Count; i++)
        {
            var meaning = meanings[i];
            var meta = metaphors.ElementAtOrDefault(i);

            // Literal path always
            AddFallbackForText(items, i + 1, meaning.NormalizedMeaning, meaning.RawStatement, meaning.Confidence, meaning.SymptomCategory, "Literal");

            // Metaphor path only when genuine metaphor detected
            if (meta is { IsMetaphor: true }
                && !string.IsNullOrWhiteSpace(meta.ClinicalMeaning)
                && !string.Equals(meta.ClinicalMeaning, meaning.NormalizedMeaning, StringComparison.OrdinalIgnoreCase))
            {
                AddFallbackForText(items, i + 1, meta.ClinicalMeaning, meta.Expression, meta.Confidence, meaning.SymptomCategory, "Metaphor");
            }
        }

        foreach (var block in symptomBlocks)
        {
            if (string.IsNullOrWhiteSpace(block.TranscriptSpan)) continue;
            foreach (var category in CategoryKeywordDetector.DetectCategories(block.TranscriptSpan, block.CategoryHint))
            {
                if (items.Any(x =>
                        string.Equals(x.EvidenceSpan, block.TranscriptSpan, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase)))
                    continue;

                items.Add(new MultiConceptDiscoveryItemGptModel
                {
                    MeaningIndex = 0,
                    Category = category,
                    ClinicalConceptName = block.TranscriptSpan.Trim(),
                    HomeopathicConceptName = ConceptGraphAssemblyEngine.ToHomeopathicLabel(block.TranscriptSpan, category),
                    Importance = "Medium",
                    SymptomClass = MapSymptomClass(category),
                    Confidence = block.Confidence,
                    EvidenceSpan = block.TranscriptSpan.Trim(),
                    InterpretationSource = "Literal",
                });
            }
        }

        if (items.Count == 0 && !string.IsNullOrWhiteSpace(transcript))
        {
            foreach (var category in CategoryKeywordDetector.DetectCategories(transcript, null))
            {
                items.Add(new MultiConceptDiscoveryItemGptModel
                {
                    MeaningIndex = meanings.Count > 0 ? 1 : 0,
                    Category = category,
                    ClinicalConceptName = transcript[..Math.Min(transcript.Length, 120)].Trim(),
                    HomeopathicConceptName = ConceptGraphAssemblyEngine.ToHomeopathicLabel(transcript, category),
                    Importance = "Medium",
                    SymptomClass = MapSymptomClass(category),
                    Confidence = 0.70m,
                    InterpretationSource = "Literal",
                });
            }
        }

        return items;
    }

    private static void AddFallbackForText(
        List<MultiConceptDiscoveryItemGptModel> items,
        int meaningIndex,
        string clinical,
        string evidence,
        decimal confidence,
        string? symptomCategory,
        string interpretationSource)
    {
        var text = $"{evidence} {clinical}";
        foreach (var category in CategoryKeywordDetector.DetectCategories(text, symptomCategory))
        {
            items.Add(new MultiConceptDiscoveryItemGptModel
            {
                MeaningIndex = meaningIndex,
                Category = category,
                ClinicalConceptName = clinical,
                HomeopathicConceptName = ConceptGraphAssemblyEngine.ToHomeopathicLabel(clinical, category),
                Importance = category is "Fear" or "General" ? "High" : "Medium",
                SymptomClass = MapSymptomClass(category),
                Confidence = confidence,
                EvidenceSpan = evidence,
                InterpretationSource = interpretationSource,
            });
        }
    }

    private static string MapSymptomClass(string category) =>
        category switch
        {
            "Fear" or "Forgetfulness" => "MentalGeneral",
            "Modality" => "Modality",
            "Food Desire" => "Food",
            "Sleep" => "Sleep",
            "Dream" => "Dream",
            "Sexual" => "Sexual",
            "General" => "PhysicalGeneral",
            _ => "Particular",
        };
}

public static class CategoryKeywordDetector
{
    private static readonly (string Category, string[] Keywords)[] Rules =
    [
        ("Fear", ["fear", "afraid", "anxiety", "phobia", "panic", "terrified", "dread"]),
        ("Forgetfulness", ["forget", "memory", "absent minded", "absent-minded", "recall", "remember"]),
        ("Modality", ["worse", "better", "agg", "amel", "aggravat", "ameliorat", "cold", "heat", "motion", "rest"]),
        ("Food Desire", ["desire", "craving", "crave", "aversion", "appetite", "hungry", "sweet", "salt", "spicy"]),
        ("Sleep", ["sleep", "insomnia", "somnol", "drowsy", "wake", "snore", "restless night"]),
        ("Dream", ["dream", "nightmare", "night mare"]),
        ("Sexual", ["sexual", "libido", "desire sex", "erection", "masturbation", "orgasm"]),
        ("General", ["weakness", "fatigue", "fever", "chill", "perspir", "general"]),
        ("Physical", ["pain", "ache", "swelling", "burning", "numb", "tingl", "headache", "vomit", "nausea"]),
    ];

    public static IEnumerable<string> DetectCategories(string text, string? categoryHint)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lower = text.ToLowerInvariant();

        foreach (var (category, keywords) in Rules)
        {
            if (keywords.Any(k => lower.Contains(k, StringComparison.OrdinalIgnoreCase)))
                found.Add(category);
        }

        var hinted = NormalizeCategoryHint(categoryHint);
        if (!string.IsNullOrWhiteSpace(hinted))
            found.Add(hinted);

        if (found.Count == 0)
            found.Add("General");

        return found;
    }

    public static string? NormalizeCategoryHint(string? hint)
    {
        if (string.IsNullOrWhiteSpace(hint)) return null;
        var v = hint.Trim();
        if (v.Contains("Fear", StringComparison.OrdinalIgnoreCase)) return "Fear";
        if (v.Contains("Forget", StringComparison.OrdinalIgnoreCase)) return "Forgetfulness";
        if (v.Contains("Modality", StringComparison.OrdinalIgnoreCase)) return "Modality";
        if (v.Contains("Food", StringComparison.OrdinalIgnoreCase)) return "Food Desire";
        if (v.Contains("Sleep", StringComparison.OrdinalIgnoreCase)) return "Sleep";
        if (v.Contains("Dream", StringComparison.OrdinalIgnoreCase)) return "Dream";
        if (v.Contains("Sexual", StringComparison.OrdinalIgnoreCase)) return "Sexual";
        if (v.Contains("Mental", StringComparison.OrdinalIgnoreCase)) return "Fear";
        if (v.Contains("Physical", StringComparison.OrdinalIgnoreCase)) return "Physical";
        if (v.Contains("General", StringComparison.OrdinalIgnoreCase)) return "General";
        return v;
    }
}

public static class ConceptGraphAssemblyEngine
{
    public static MultiConceptDiscoveryResult Assemble(
        IReadOnlyList<MultiConceptDiscoveryItemGptModel> items,
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        string? chiefComplaint,
        DoctorLearningWeightsSnapshot? learnedWeights = null,
        RubricIntelligenceOptions? options = null)
    {
        var result = new MultiConceptDiscoveryResult();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.ClinicalConceptName)
                || string.IsNullOrWhiteSpace(item.HomeopathicConceptName))
                continue;

            var meaningIndex = item.MeaningIndex > 0 && item.MeaningIndex <= meanings.Count
                ? item.MeaningIndex - 1
                : 0;
            var meaning = meanings.ElementAtOrDefault(meaningIndex);
            var category = NormalizeCategory(item.Category ?? meaning?.SymptomCategory);
            var clinicalName = item.ClinicalConceptName.Trim();
            var homeoName = item.HomeopathicConceptName.Trim();
            var dedupeKey = $"{meaningIndex}|{category}|{clinicalName}|{homeoName}".ToLowerInvariant();
            if (!seen.Add(dedupeKey)) continue;

            var clinicalIndex = result.ClinicalConcepts.Count;
            var clinical = new ClinicalConceptNodeModel
            {
                PatientMeaningId = meaning?.PatientMeaningId,
                MeaningIndex = meaningIndex,
                ConceptName = clinicalName,
                Domain = MapDomain(category),
                Confidence = ConceptGraphTierHelper.ClampConfidence(item.Confidence),
                SymptomCategory = category,
                ModelVersion = MultiConceptDiscoveryEngine.ModelId,
            };
            result.ClinicalConcepts.Add(clinical);

            var weight = ResolveWeight(item.Importance, item.IsSRP, item.SymptomClass);
            var homeoIndex = result.HomeopathicConcepts.Count;
            var homeo = new HomeopathicConceptNodeModel
            {
                ClinicalConceptIndex = clinicalIndex,
                ConceptName = homeoName,
                Importance = item.Importance ?? "Medium",
                SymptomClass = item.SymptomClass ?? MapSymptomClass(category),
                Category = category,
                IsSRP = item.IsSRP,
                Weight = weight,
                Confidence = ConceptGraphTierHelper.ClampConfidence(item.Confidence),
                EvidenceSpan = item.EvidenceSpan,
                ModelVersion = MultiConceptDiscoveryEngine.ModelId,
            };
            result.HomeopathicConcepts.Add(homeo);

            if (meaning != null)
            {
                var interpretationSource = string.Equals(
                    item.InterpretationSource, "Metaphor", StringComparison.OrdinalIgnoreCase)
                    ? "Metaphor"
                    : "Literal";
                clinical.InterpretationSource = interpretationSource;

                result.Edges.Add(new ConceptGraphEdgeModel
                {
                    FromNodeType = "Meaning",
                    FromNodeKey = $"meaning:{meaningIndex}",
                    ToNodeType = "Clinical",
                    ToNodeKey = $"clinical:{clinicalIndex}",
                    EdgeType = interpretationSource == "Metaphor"
                        ? ConceptInterpretationEdgeTypes.Interprets
                        : ConceptInterpretationEdgeTypes.DerivesClinical,
                    Weight = clinical.Confidence,
                    Confidence = clinical.Confidence,
                });
            }

            result.Edges.Add(new ConceptGraphEdgeModel
            {
                FromNodeType = "Clinical",
                FromNodeKey = $"clinical:{clinicalIndex}",
                ToNodeType = "Homeopathic",
                ToNodeKey = $"homeopathic:{homeoIndex}",
                EdgeType = ConceptInterpretationEdgeTypes.MapsTo,
                Weight = weight,
                Confidence = homeo.Confidence,
            });

            result.Edges.Add(new ConceptGraphEdgeModel
            {
                FromNodeType = "Homeopathic",
                FromNodeKey = $"homeopathic:{homeoIndex}",
                ToNodeType = "Category",
                ToNodeKey = $"category:{category}",
                EdgeType = $"Category:{category}",
                Weight = 1m,
                Confidence = homeo.Confidence,
            });
        }

        var tiers = ConceptTierAssignmentEngine.Assign(result, chiefComplaint, learnedWeights, options);
        result.PrimaryConcepts = tiers.Primary;
        result.SecondaryConcepts = tiers.Secondary;
        result.SupportingConcepts = tiers.Supporting;

        ApplyTiersToNodes(result);
        return result;
    }

    public static string ToHomeopathicLabel(string clinicalText, string category)
    {
        var trimmed = clinicalText.Trim();
        if (trimmed.Length <= 80) return trimmed;
        return category switch
        {
            "Fear" => "Anticipatory Fear",
            "Forgetfulness" => "Forgetfulness",
            "Food Desire" => "Food Desire",
            "Sleep" => "Sleep Disturbance",
            "Dream" => "Dream Symptom",
            "Sexual" => "Sexual Symptom",
            "Modality" => "Modality Symptom",
            _ => trimmed[..80].Trim(),
        };
    }

    private static void ApplyTiersToNodes(MultiConceptDiscoveryResult result)
    {
        foreach (var tiered in result.PrimaryConcepts
            .Concat(result.SecondaryConcepts)
            .Concat(result.SupportingConcepts))
        {
            if (tiered.ClinicalConceptIndex >= 0 && tiered.ClinicalConceptIndex < result.ClinicalConcepts.Count)
                result.ClinicalConcepts[tiered.ClinicalConceptIndex].ConceptTier = tiered.ConceptTier;

            if (tiered.HomeopathicConceptIndex >= 0 && tiered.HomeopathicConceptIndex < result.HomeopathicConcepts.Count)
                result.HomeopathicConcepts[tiered.HomeopathicConceptIndex].ConceptTier = tiered.ConceptTier;
        }
    }

    private static string NormalizeCategory(string? category)
    {
        var normalized = CategoryKeywordDetector.NormalizeCategoryHint(category);
        return string.IsNullOrWhiteSpace(normalized) ? "General" : normalized;
    }

    private static string MapDomain(string category) =>
        category switch
        {
            "Fear" or "Forgetfulness" => "Mental",
            "Food Desire" => "GI",
            "Sleep" or "Dream" => "General",
            "Sexual" => "General",
            "Modality" => "General",
            "Physical" => "General",
            _ => "General",
        };

    private static string MapSymptomClass(string category) =>
        category switch
        {
            "Fear" or "Forgetfulness" => "MentalGeneral",
            "Modality" => "Modality",
            "Food Desire" => "Food",
            "Sleep" => "Sleep",
            "Dream" => "Dream",
            "Sexual" => "Sexual",
            "General" => "PhysicalGeneral",
            _ => "Particular",
        };

    private static decimal ResolveWeight(string? importance, bool isSrp, string? symptomClass)
    {
        if (isSrp || string.Equals(symptomClass, "SRP", StringComparison.OrdinalIgnoreCase)) return 3.0m;
        if (string.Equals(importance, "High", StringComparison.OrdinalIgnoreCase)) return 2.5m;
        if (string.Equals(symptomClass, "MentalGeneral", StringComparison.OrdinalIgnoreCase)) return 2.0m;
        if (string.Equals(importance, "Low", StringComparison.OrdinalIgnoreCase)) return 0.8m;
        return 1.5m;
    }
}

public static class ConceptTierAssignmentEngine
{
    public static (List<TieredConceptNodeModel> Primary, List<TieredConceptNodeModel> Secondary, List<TieredConceptNodeModel> Supporting)
        Assign(
            MultiConceptDiscoveryResult discovery,
            string? chiefComplaint,
            DoctorLearningWeightsSnapshot? learnedWeights = null,
            RubricIntelligenceOptions? options = null)
    {
        var scoringOptions = options ?? new RubricIntelligenceOptions();
        var scored = new List<(TieredConceptNodeModel Node, decimal Score)>();

        for (var i = 0; i < discovery.HomeopathicConcepts.Count; i++)
        {
            var homeo = discovery.HomeopathicConcepts[i];
            var clinical = discovery.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex);
            if (clinical == null) continue;

            var score = homeo.Weight * homeo.Confidence;
            if (homeo.IsSRP) score *= 1.15m;
            if (MatchesChiefComplaint(homeo, clinical, chiefComplaint)) score *= 1.10m;

            if (learnedWeights != null)
            {
                score = DoctorLearningScoring.ApplyConceptRankingBoost(
                    score,
                    homeo.ConceptName,
                    learnedWeights.ConceptRanking,
                    scoringOptions);
                score = DoctorLearningScoring.ApplyClinicalRelevanceBoost(
                    score,
                    clinical.ConceptName,
                    homeo.ConceptName,
                    learnedWeights.ClinicalRelevance,
                    scoringOptions);
                homeo.Confidence = DoctorLearningScoring.ApplyConfidenceCalibration(
                    homeo.Confidence,
                    homeo.ConceptName,
                    learnedWeights.ConceptRanking,
                    scoringOptions);
            }

            scored.Add((new TieredConceptNodeModel
            {
                Category = homeo.Category ?? clinical.SymptomCategory ?? "General",
                ClinicalConceptName = clinical.ConceptName,
                HomeopathicConceptName = homeo.ConceptName,
                Confidence = homeo.Confidence,
                Weight = homeo.Weight,
                IsSRP = homeo.IsSRP,
                ClinicalConceptIndex = homeo.ClinicalConceptIndex,
                HomeopathicConceptIndex = i,
                MeaningIndex = clinical.MeaningIndex >= 0 ? clinical.MeaningIndex : null,
                EvidenceSpan = homeo.EvidenceSpan,
                SymptomClass = homeo.SymptomClass,
            }, score));
        }

        if (scored.Count == 0)
            return (new List<TieredConceptNodeModel>(), new List<TieredConceptNodeModel>(), new List<TieredConceptNodeModel>());

        var maxScore = scored.Max(x => x.Score);
        var primaryThreshold = maxScore * 0.85m;
        var secondaryThreshold = maxScore * 0.60m;

        var primary = new List<TieredConceptNodeModel>();
        var secondary = new List<TieredConceptNodeModel>();
        var supporting = new List<TieredConceptNodeModel>();

        foreach (var (node, score) in scored.OrderByDescending(x => x.Score))
        {
            if (node.IsSRP && node.Confidence >= 0.75m || score >= primaryThreshold)
            {
                node.ConceptTier = ConceptTierLabels.Primary;
                primary.Add(node);
            }
            else if (score >= secondaryThreshold)
            {
                node.ConceptTier = ConceptTierLabels.Secondary;
                secondary.Add(node);
            }
            else if (node.Confidence >= 0.50m)
            {
                node.ConceptTier = ConceptTierLabels.Supporting;
                supporting.Add(node);
            }
        }

        return (primary, secondary, supporting);
    }

    private static bool MatchesChiefComplaint(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel clinical,
        string? chiefComplaint)
    {
        if (string.IsNullOrWhiteSpace(chiefComplaint)) return false;
        return chiefComplaint.Contains(homeo.ConceptName, StringComparison.OrdinalIgnoreCase)
            || chiefComplaint.Contains(clinical.ConceptName, StringComparison.OrdinalIgnoreCase)
            || homeo.ConceptName.Contains(chiefComplaint, StringComparison.OrdinalIgnoreCase)
            || clinical.ConceptName.Contains(chiefComplaint, StringComparison.OrdinalIgnoreCase);
    }
}
