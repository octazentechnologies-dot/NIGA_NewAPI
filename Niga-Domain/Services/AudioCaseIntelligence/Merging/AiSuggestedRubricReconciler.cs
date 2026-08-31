using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;

namespace Niga_Domain.Services.AudioCaseIntelligence.Merging;

/// <summary>
/// Task 6: promote AI-suggested rubrics to real SubSectionMaster rows when confidence is high enough.
/// </summary>
public class AiSuggestedRubricReconciler
{
    private readonly NIGACentrumContext _context;
    private readonly ILogger<AiSuggestedRubricReconciler> _logger;

    public AiSuggestedRubricReconciler(
        NIGACentrumContext context,
        ILogger<AiSuggestedRubricReconciler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<AudioCaseSuggestedRubricModel>> ReconcileAsync(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        decimal minConfidence,
        CancellationToken cancellationToken = default) =>
        await ReconcileAsync(rubrics, minConfidence, 0.85m, cancellationToken);

    public async Task<List<AudioCaseSuggestedRubricModel>> ReconcileAsync(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        decimal minConfidence,
        decimal fuzzyMinConfidence,
        CancellationToken cancellationToken = default)
    {
        var result = new List<AudioCaseSuggestedRubricModel>(rubrics.Count);

        foreach (var rubric in rubrics)
        {
            if (!NeedsReconciliation(rubric))
            {
                result.Add(rubric);
                continue;
            }

            var (match, score, isExact) = await FindBestSubSectionAsync(rubric, cancellationToken);
            var willPromote = match != null
                && AiReconciliationPromotion.ShouldPromote(isExact, score, minConfidence, fuzzyMinConfidence);

            _logger.LogInformation(
                "AI rubric reconciliation attempt '{Name}' bestScore={Score:F3} exactMin={ExactMin:F3} fuzzyMin={FuzzyMin:F3} exact={Exact} matched={Matched} promote={Promote}",
                rubric.SubSectionName,
                score,
                minConfidence,
                fuzzyMinConfidence,
                isExact,
                match?.SubSectionId,
                willPromote);

            if (willPromote && match != null)
            {
                AiReconciliationPromotion.Apply(
                    rubric,
                    match.SubSectionId,
                    match.SubSectionName,
                    match.SectionId,
                    match.RemedyCount,
                    score,
                    isExact);

                _logger.LogInformation(
                    "AI rubric reconciled {Kind} SubSectionId={Id} score={Score:F3} source={Source}",
                    isExact ? "exact" : "fuzzy",
                    match.SubSectionId,
                    score,
                    rubric.MatchSource);
            }
            else
            {
                rubric.Source = "AiSuggested";
                rubric.IsAiSuggested = true;
            }

            result.Add(rubric);
        }

        return result;
    }

    private static bool NeedsReconciliation(AudioCaseSuggestedRubricModel rubric) =>
        rubric.IsAiSuggested
        || rubric.SubSectionId <= 0
        || string.Equals(rubric.Source, "AiSuggested", StringComparison.OrdinalIgnoreCase)
        || string.Equals(rubric.ResultKind, "AiClinicalConcept", StringComparison.OrdinalIgnoreCase);

    private async Task<(ReconciledSubSection? Match, decimal Score, bool IsExact)> FindBestSubSectionAsync(
        AudioCaseSuggestedRubricModel rubric,
        CancellationToken cancellationToken)
    {
        var name = (rubric.SubSectionName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (null, 0m, false);

        var lowered = name.ToLower();
        var exact = await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null)
            .Where(s => s.SubSectionName!.Trim().ToLower() == lowered)
            .Select(s => new ReconciledSubSection
            {
                SubSectionId = s.SubSectionId,
                SubSectionName = s.SubSectionName,
                SectionId = s.SectionId,
                RemedyCount = s.RubricRemedyDetails.Count(r => r.DeletedStatus != true),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (exact != null)
            return (exact, 1.0m, true);

        var searchCorpus = BuildSearchCorpus(rubric);
        var tokens = searchCorpus.SelectMany(Tokenize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (tokens.Count == 0)
            return (null, 0m, false);

        var candidates = new List<ReconciledSubSection>();
        foreach (var lead in tokens.Take(4))
        {
            var batch = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null && s.SubSectionName.Contains(lead))
                .OrderBy(s => s.SubSectionName!.Length)
                .Take(30)
                .Select(s => new ReconciledSubSection
                {
                    SubSectionId = s.SubSectionId,
                    SubSectionName = s.SubSectionName,
                    SectionId = s.SectionId,
                    RemedyCount = s.RubricRemedyDetails.Count(r => r.DeletedStatus != true),
                })
                .ToListAsync(cancellationToken);
            candidates.AddRange(batch);
        }

        if (ContainsAuraHints(searchCorpus))
        {
            var auraBatch = await _context.SubSectionMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus && s.SubSectionName != null
                    && (EF.Functions.Like(s.SubSectionName, "%CONVULS%")
                        || EF.Functions.Like(s.SubSectionName, "%EPILEP%")
                        || EF.Functions.Like(s.SubSectionName, "%PREMONIT%")
                        || EF.Functions.Like(s.SubSectionName, "%AURA%")
                        || EF.Functions.Like(s.SubSectionName, "%VIBRAT%")))
                .OrderBy(s => s.SubSectionName)
                .Take(25)
                .Select(s => new ReconciledSubSection
                {
                    SubSectionId = s.SubSectionId,
                    SubSectionName = s.SubSectionName,
                    SectionId = s.SectionId,
                    RemedyCount = s.RubricRemedyDetails.Count(r => r.DeletedStatus != true),
                })
                .ToListAsync(cancellationToken);
            candidates.AddRange(auraBatch);
        }

        ReconciledSubSection? best = null;
        var bestScore = 0m;
        foreach (var candidate in candidates
                     .GroupBy(c => c.SubSectionId)
                     .Select(g => g.First()))
        {
            var score = searchCorpus
                .Select(text => ScoreName(text, candidate.SubSectionName ?? string.Empty, Tokenize(text)))
                .DefaultIfEmpty(0m)
                .Max();

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return (best, bestScore, best != null && AiReconciliationPromotion.IsExactNameMatch(name, best.SubSectionName));
    }

    private static IEnumerable<string> BuildSearchCorpus(AudioCaseSuggestedRubricModel rubric)
    {
        if (!string.IsNullOrWhiteSpace(rubric.SubSectionName))
            yield return rubric.SubSectionName;
        if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
            yield return rubric.MatchedFrom;
        if (!string.IsNullOrWhiteSpace(rubric.Explainability?.ClinicalMeaning))
            yield return rubric.Explainability!.ClinicalMeaning!;
        if (!string.IsNullOrWhiteSpace(rubric.Explainability?.HomeopathicMeaning))
            yield return rubric.Explainability!.HomeopathicMeaning!;
        if (!string.IsNullOrWhiteSpace(rubric.WhySuggested))
            yield return rubric.WhySuggested;
    }

    private static bool ContainsAuraHints(IEnumerable<string> corpus) =>
        corpus.Any(text =>
            text.Contains("vibrat", StringComparison.OrdinalIgnoreCase)
            || text.Contains("aura", StringComparison.OrdinalIgnoreCase)
            || text.Contains("premonit", StringComparison.OrdinalIgnoreCase)
            || text.Contains("prodrom", StringComparison.OrdinalIgnoreCase)
            || text.Contains("convuls", StringComparison.OrdinalIgnoreCase)
            || text.Contains("epilep", StringComparison.OrdinalIgnoreCase)
            || text.Contains("shock", StringComparison.OrdinalIgnoreCase));

    private static List<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split(new[] { ' ', '-', ',', ';', '/', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length >= 3)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static decimal ScoreName(string proposed, string actual, IReadOnlyList<string> proposedTokens)
    {
        if (string.Equals(proposed, actual, StringComparison.OrdinalIgnoreCase))
            return 1m;

        if (actual.Contains(proposed, StringComparison.OrdinalIgnoreCase)
            || proposed.Contains(actual, StringComparison.OrdinalIgnoreCase))
            return 0.92m;

        var actualTokens = Tokenize(actual);
        if (actualTokens.Count == 0 || proposedTokens.Count == 0)
            return 0m;

        var overlap = proposedTokens.Intersect(actualTokens, StringComparer.OrdinalIgnoreCase).Count();
        var union = proposedTokens.Union(actualTokens, StringComparer.OrdinalIgnoreCase).Count();
        return union == 0 ? 0m : (decimal)overlap / union;
    }

    private sealed class ReconciledSubSection
    {
        public int SubSectionId { get; set; }
        public string? SubSectionName { get; set; }
        public int? SectionId { get; set; }
        public int RemedyCount { get; set; }
    }
}
