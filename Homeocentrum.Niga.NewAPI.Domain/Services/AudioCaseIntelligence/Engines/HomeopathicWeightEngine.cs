using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class HomeopathicWeightEngine : IHomeopathicWeightEngine
{
    private const decimal BaselineWeight = 5m;
    private const decimal DefaultCausationMultiplier = 1.3m;

    private static readonly Dictionary<string, decimal> DefaultRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["srp"] = 10m,
        ["mental"] = 8m,
        ["causation"] = 7m,
        ["general"] = 5m,
        ["particular"] = 4m,
        ["concomitant"] = 3.5m,
        ["confirmatory"] = 2m,
    };

    private readonly IAudioCaseIntelligenceRepository _repository;
    private WeightRuleSet _currentRules = WeightRuleSet.FromDefaults(DefaultRules);

    public HomeopathicWeightEngine(IAudioCaseIntelligenceRepository repository)
    {
        _repository = repository;
    }

    public async Task<HomeopathicWeightResult> ApplyAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<CausationLinkModel> causationLinks,
        CancellationToken cancellationToken = default)
    {
        var rules = await LoadRulesAsync(cancellationToken);
        var effectConceptIds = causationLinks
            .Where(l => l.EffectConceptId.HasValue)
            .Select(l => l.EffectConceptId!.Value)
            .ToHashSet();

        var weighted = concepts.Select(concept =>
        {
            var copy = Clone(concept);
            var categoryKey = ResolveCategoryKey(copy);
            var weight = rules.Weights.TryGetValue(categoryKey, out var ruleWeight)
                ? ruleWeight
                : BaselineWeight;

            if (copy.IsSRP)
            {
                weight = Math.Max(weight, rules.Weights.TryGetValue("srp", out var srpWeight) ? srpWeight : 10m);
            }

            if (effectConceptIds.Contains(copy.ConceptId))
            {
                weight *= GetMultiplier("causation");
            }

            copy.HomeopathicWeight = Math.Round(weight, 2);
            return copy;
        }).ToList();

        return new HomeopathicWeightResult
        {
            Concepts = weighted,
            RuleWeights = rules.Weights,
        };
    }

    public async Task<decimal> ApplyWeightToRubricAsync(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken = default)
    {
        await LoadRulesAsync(cancellationToken);
        var conceptWeight = ResolveConceptWeightForRubric(rubric, concepts);
        var sectionWeight = ResolveSectionWeight(rubric.SubSectionName);
        var weight = Math.Max(conceptWeight, sectionWeight);
        weight = Math.Max(weight, 1m);

        rubric.HomeopathicWeight = Math.Round(weight, 2);
        var multiplier = weight / BaselineWeight;
        if (IsCausationLinked(rubric))
            multiplier *= GetMultiplier("CausationLinked");

        rubric.MatchScore = Math.Round(Math.Min(0.99m, rubric.MatchScore * multiplier), 4);
        rubric.ConfidenceScore = rubric.MatchScore;
        return weight;
    }

    public decimal GetMultiplier(string categoryOrCode)
    {
        if (_currentRules.Multipliers.TryGetValue(categoryOrCode, out var multiplier))
            return multiplier;

        return string.Equals(categoryOrCode, "causation", StringComparison.OrdinalIgnoreCase)
            || string.Equals(categoryOrCode, "CausationLinked", StringComparison.OrdinalIgnoreCase)
            ? DefaultCausationMultiplier
            : 1m;
    }

    private async Task<WeightRuleSet> LoadRulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var dbRules = await _repository.GetActiveWeightRulesAsync(cancellationToken);
            if (dbRules.Count > 0)
            {
                _currentRules = WeightRuleSet.FromRows(dbRules, DefaultRules);
                return _currentRules;
            }
        }
        catch
        {
            // Fall back to defaults when Phase 3 SQL is not deployed yet.
        }

        _currentRules = WeightRuleSet.FromDefaults(DefaultRules);
        return _currentRules;
    }

    private static decimal ResolveConceptWeightForRubric(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts)
    {
        if (concepts.Count == 0) return BaselineWeight;

        var rubricText = $"{rubric.MatchedFrom} {rubric.SubSectionName}".ToLowerInvariant();
        var matched = concepts
            .Where(c => ConceptMatchesRubric(c, rubricText))
            .Select(c => c.HomeopathicWeight)
            .DefaultIfEmpty(BaselineWeight)
            .Max();

        return matched > 0 ? matched : BaselineWeight;
    }

    private static bool ConceptMatchesRubric(ClinicalConceptModel concept, string rubricText)
    {
        var terms = concept.SearchTerms
            .Concat(new[] { concept.RawStatement, concept.ClinicalMeaning ?? string.Empty })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.ToLowerInvariant());

        return terms.Any(term =>
            rubricText.Contains(term, StringComparison.Ordinal)
            || term.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(token => token.Length > 3 && rubricText.Contains(token, StringComparison.Ordinal)));
    }

    private static bool IsCausationLinked(AudioCaseSuggestedRubricModel rubric)
    {
        var explainability = $"{rubric.MatchedFrom} {rubric.MatchLayer} {rubric.MatchSource}";
        return explainability.Contains("causation", StringComparison.OrdinalIgnoreCase)
            || explainability.Contains("cause", StringComparison.OrdinalIgnoreCase)
            || explainability.Contains("effect", StringComparison.OrdinalIgnoreCase);
    }

    private static decimal ResolveSectionWeight(string? subSectionName)
    {
        if (string.IsNullOrWhiteSpace(subSectionName)) return BaselineWeight;

        var upper = subSectionName.ToUpperInvariant();

        // Domain-specific anchors must outrank generic MIND section bias
        // (e.g. STOMACH-THIRST vs MIND-DESIRES for thirst concepts).
        if (upper.Contains("THIRST"))
            return 9m;
        if (upper.Contains("SEXUAL DESIRE") || upper.Contains("MALE GENITALIA"))
            return 8.5m;
        if (upper.Contains("SALT") && upper.Contains("FOOD"))
            return 8.5m;
        if (upper.Contains("AWKWARD") || (upper.Contains("DROPS") && upper.Contains("THING")))
            return 8.5m;
        if (upper.Contains("TALKING") && upper.Contains("SLEEP"))
            return 8.5m;
        if (upper.Contains("CONVULSION") && upper.Contains("ANGER"))
            return 8.5m;
        if (upper.Contains("FEAR") && (upper.Contains("HIGH") || upper.Contains("HEIGHT") || upper.Contains("CONVULSION")))
            return 8.5m;

        // Bare MIND-DESIRES without a clinical anchor should not outrank organ/thirst rubrics.
        if (upper.StartsWith("MIND") && upper.Contains("DESIRE") && !upper.Contains("FEAR") && !upper.Contains("AWKWARD"))
            return 3m;

        var prefix = subSectionName.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault()?.ToUpperInvariant();

        return prefix switch
        {
            "MIND" => 8m,
            "GENERALITIES" or "GENERALS" => 5m,
            "HEAD" or "STOMACH" or "CHEST" or "EXTREMITIES" => 4m,
            _ => BaselineWeight,
        };
    }

    private static string ResolveCategoryKey(ClinicalConceptModel concept)
    {
        if (concept.IsSRP) return "srp";

        var category = concept.Category?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(category) && DefaultRules.ContainsKey(category))
        {
            return category;
        }

        return "general";
    }

    private static ClinicalConceptModel Clone(ClinicalConceptModel source) => new()
    {
        ConceptId = source.ConceptId,
        RawStatement = source.RawStatement,
        ClinicalMeaning = source.ClinicalMeaning,
        HomeopathicMeaning = source.HomeopathicMeaning,
        Category = source.Category,
        IsSRP = source.IsSRP,
        IsAmbiguous = source.IsAmbiguous,
        Modalities = source.Modalities.ToList(),
        Concomitants = source.Concomitants.ToList(),
        SearchTerms = source.SearchTerms.ToList(),
        Confidence = source.Confidence,
        SourceLanguage = source.SourceLanguage,
        HomeopathicWeight = source.HomeopathicWeight,
        SequenceOrder = source.SequenceOrder,
        ConceptTier = source.ConceptTier,
    };

    private sealed class WeightRuleSet
    {
        public Dictionary<string, decimal> Weights { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, decimal> Multipliers { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        public static WeightRuleSet FromDefaults(Dictionary<string, decimal> defaults) => new()
        {
            Weights = new Dictionary<string, decimal>(defaults, StringComparer.OrdinalIgnoreCase),
            Multipliers = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["causation"] = DefaultCausationMultiplier,
                ["CausationLinked"] = DefaultCausationMultiplier,
            },
        };

        public static WeightRuleSet FromRows(
            IEnumerable<Homeocentrum.Niga.NewAPI.Domain.Master.HomeopathicWeightRule> rows,
            Dictionary<string, decimal> defaults)
        {
            var set = FromDefaults(defaults);
            foreach (var row in rows)
            {
                if (!string.IsNullOrWhiteSpace(row.Category))
                    set.Weights[row.Category.Trim()] = row.WeightValue;
                if (!string.IsNullOrWhiteSpace(row.RuleCode))
                    set.Weights[row.RuleCode.Trim()] = row.WeightValue;

                if (row.MultiplierValue.HasValue)
                {
                    if (!string.IsNullOrWhiteSpace(row.Category))
                        set.Multipliers[row.Category.Trim()] = row.MultiplierValue.Value;
                    if (!string.IsNullOrWhiteSpace(row.RuleCode))
                        set.Multipliers[row.RuleCode.Trim()] = row.MultiplierValue.Value;
                }
            }

            return set;
        }
    }
}
