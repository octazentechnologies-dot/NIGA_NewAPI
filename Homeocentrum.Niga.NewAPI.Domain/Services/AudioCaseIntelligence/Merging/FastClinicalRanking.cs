using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Learning;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Stage F: canonical 0–1 score + MMR diversity selection for the fast clinical pipeline.
/// </summary>
public static class FastClinicalRanking
{
    public sealed class ScoreWeights
    {
        public decimal Evidence { get; set; } = 0.30m;
        public decimal ClinicalMatch { get; set; } = 0.25m;
        public decimal Semantic { get; set; } = 0.20m;
        public decimal ExactAlias { get; set; } = 0.15m;
        public decimal Keyword { get; set; } = 0.10m;
    }

    /// <summary>
    /// Assigns CanonicalScore (0–1) and Display MatchScore/ConfidenceScore (0–100 / 0–1).
    /// Does not invent evidence — weak lexical overlap lowers Evidence component.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> ApplyCanonicalScores(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts,
        ScoreWeights? weights = null)
    {
        weights ??= new ScoreWeights();
        var conceptHay = string.Join(' ',
            concepts.SelectMany(c => new[] { c.RawStatement, c.ClinicalMeaning ?? "", c.HomeopathicMeaning ?? "" }
                .Concat(c.SearchTerms)));

        foreach (var rubric in rubrics)
        {
            if (rubric.SubSectionId <= 0)
            {
                rubric.ConfidenceScore = 0;
                rubric.MatchScore = 0;
                continue;
            }

            var name = rubric.SubSectionName ?? string.Empty;
            var source = (rubric.MatchSource ?? string.Empty).ToLowerInvariant();

            var evidence = ComputeEvidenceOverlap(name, conceptHay, rubric.MatchedFrom);
            var clinical = Clamp01((rubric.ConfidenceScore ?? rubric.MatchScore) > 1
                ? (rubric.ConfidenceScore ?? rubric.MatchScore) / 100m
                : (rubric.ConfidenceScore ?? rubric.MatchScore));

            var semantic = source.Contains("embed") ? clinical : clinical * 0.5m;
            var exactAlias = source.Contains("alias") || source.Contains("exact") || source.Contains("database")
                ? Math.Max(clinical, 0.7m)
                : 0.35m;
            var keyword = source.Contains("keyword") || source.Contains("fts") || source.Contains("concept")
                ? Math.Max(clinical, 0.65m)
                : 0.30m;

            // Hard gate: no concept evidence → strong penalty (cannot be rescued by semantic alone).
            if (evidence < 0.15m)
            {
                evidence = 0;
                clinical *= 0.4m;
                semantic *= 0.3m;
            }

            var canonical =
                evidence * weights.Evidence
                + clinical * weights.ClinicalMatch
                + semantic * weights.Semantic
                + exactAlias * weights.ExactAlias
                + keyword * weights.Keyword;

            canonical = Clamp01(canonical);
            rubric.ConfidenceScore = canonical;
            rubric.MatchScore = Math.Round(canonical * 100m, 1);
            if (string.IsNullOrWhiteSpace(rubric.WhySuggested))
            {
                rubric.WhySuggested =
                    $"CanonicalScore={canonical:F2} (evidence={evidence:F2}, source={rubric.MatchSource})";
            }
        }

        return rubrics
            .OrderByDescending(r => r.ConfidenceScore ?? 0)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Maximal Marginal Relevance selection. Prefer relevance but penalize near-duplicates.
    /// Stops early when score cliff is large (never fabricate to fill targetCount).
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> SelectWithMmr(
        IReadOnlyList<AudioCaseSuggestedRubricModel> ranked,
        int targetCount,
        decimal lambda = 0.80m,
        decimal minCanonicalScore = 0.45m,
        decimal scoreCliffRatio = 0.70m)
    {
        targetCount = Math.Clamp(targetCount, 1, 20);
        var pool = ranked
            .Where(r => r.SubSectionId > 0)
            .Where(r => (r.ConfidenceScore ?? 0) >= minCanonicalScore)
            .GroupBy(r => r.SubSectionId)
            .Select(g => g.OrderByDescending(x => x.ConfidenceScore ?? 0).First())
            .OrderByDescending(r => r.ConfidenceScore ?? 0)
            .ToList();

        if (pool.Count == 0)
            return new List<AudioCaseSuggestedRubricModel>();

        var selected = new List<AudioCaseSuggestedRubricModel>();
        var topScore = pool[0].ConfidenceScore ?? 0m;

        while (selected.Count < targetCount && pool.Count > 0)
        {
            AudioCaseSuggestedRubricModel? best = null;
            decimal bestMmr = decimal.MinValue;

            foreach (var candidate in pool)
            {
                var relevance = candidate.ConfidenceScore ?? 0m;
                if (selected.Count == 0)
                {
                    best = candidate;
                    bestMmr = relevance;
                    break;
                }

                var maxSim = selected.Max(s => NameSimilarity(s.SubSectionName, candidate.SubSectionName));
                var mmr = lambda * relevance - (1 - lambda) * maxSim;
                if (mmr > bestMmr)
                {
                    bestMmr = mmr;
                    best = candidate;
                }
            }

            if (best == null)
                break;

            var score = best.ConfidenceScore ?? 0m;
            // Evidence-based stop: large cliff after at least 5 strong rubrics.
            if (selected.Count >= 5 && topScore > 0 && score < topScore * scoreCliffRatio)
                break;

            selected.Add(best);
            pool.RemoveAll(r => r.SubSectionId == best.SubSectionId);
        }

        return selected;
    }

    /// <summary>
    /// Soft doctor-learning boost AFTER evidence/canonical scoring.
    /// Never rescues zero-evidence candidates; never overrides gender/hallucination rejects.
    /// </summary>
    public static List<AudioCaseSuggestedRubricModel> ApplyDoctorLearningBoost(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts,
        DoctorLearningWeightsSnapshot? learned,
        RubricIntelligenceOptions options)
    {
        if (!options.EnableDoctorLearningEngine
            || !options.FastPipelineEnableDoctorLearning
            || learned == null
            || (learned.RubricAcceptanceRates.Count == 0
                && learned.ConceptRubricMapping.Count == 0
                && learned.ConceptRanking.Count == 0))
        {
            return rubrics.ToList();
        }

        foreach (var rubric in rubrics)
        {
            if (rubric.SubSectionId <= 0)
                continue;

            // Hard rule: learning cannot invent evidence.
            if ((rubric.EvidenceScore ?? 0) < options.FastPipelineMinEvidenceScore
                && (rubric.ConfidenceScore ?? 0) < options.FastPipelineMinCanonicalScore)
            {
                continue;
            }

            var score = rubric.ConfidenceScore ?? 0m;
            var before = score;

            if (learned.RubricAcceptanceRates.TryGetValue(rubric.SubSectionId, out var rate))
            {
                // Blend small acceptance prior (≤5% of weight budget conceptually).
                score = Math.Clamp((score * 0.95m) + (rate * 0.05m), 0m, 1m);
            }

            var conceptName = rubric.MatchedFrom
                ?? rubric.PatientEvidence
                ?? rubric.Explainability?.HomeopathicMeaning
                ?? concepts.FirstOrDefault(c => c.ConceptId == rubric.SourceConceptId)?.HomeopathicMeaning;

            if (!string.IsNullOrWhiteSpace(conceptName)
                && learned.ConceptRubricMapping.TryGetValue((conceptName, rubric.SubSectionId), out var mapW))
            {
                var capped = DoctorLearningScoring.CapWeight(mapW, options.DoctorLearningMaxAccumulatedWeight);
                score = Math.Clamp(score + (capped * 0.05m), 0m, 1m);
            }

            if (!string.IsNullOrWhiteSpace(conceptName))
            {
                score = DoctorLearningScoring.ApplyConceptRankingBoost(
                    score, conceptName, learned.ConceptRanking, options);
            }

            if (score != before)
            {
                rubric.ConfidenceScore = score;
                rubric.CanonicalScore = score;
                rubric.MatchScore = Math.Round(score * 100m, 1);
                rubric.ValidationFlags.Add("DoctorLearningBoost");
            }
        }

        return rubrics
            .OrderByDescending(r => r.ConfidenceScore ?? 0)
            .ThenBy(r => r.SubSectionName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static decimal ComputeEvidenceOverlap(string rubricName, string conceptHay, string? matchedFrom)
    {
        if (string.IsNullOrWhiteSpace(conceptHay) && string.IsNullOrWhiteSpace(matchedFrom))
            return 0;

        var tokens = Tokenize(rubricName);
        if (tokens.Count == 0)
            return string.IsNullOrWhiteSpace(matchedFrom) ? 0 : 0.4m;

        var hay = (conceptHay + " " + (matchedFrom ?? "")).ToLowerInvariant();
        var hits = tokens.Count(t => hay.Contains(t, StringComparison.Ordinal));
        return Clamp01(hits / (decimal)tokens.Count);
    }

    private static decimal NameSimilarity(string? a, string? b)
    {
        var ta = Tokenize(a);
        var tb = Tokenize(b);
        if (ta.Count == 0 || tb.Count == 0)
            return 0;
        var inter = ta.Intersect(tb, StringComparer.OrdinalIgnoreCase).Count();
        var union = ta.Union(tb, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0 : inter / (decimal)union;
    }

    private static HashSet<string> Tokenize(string? text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(text))
            return set;
        foreach (var part in text.Split(new[] { ' ', '-', ',', ';', '/', '(', ')', '.' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var t = part.Trim().ToLowerInvariant();
            if (t.Length >= 3 && t is not ("the" or "and" or "with" or "from" or "for"))
                set.Add(t);
        }
        return set;
    }

    private static decimal Clamp01(decimal v) =>
        v < 0 ? 0 : v > 1 ? 1 : v;
}
