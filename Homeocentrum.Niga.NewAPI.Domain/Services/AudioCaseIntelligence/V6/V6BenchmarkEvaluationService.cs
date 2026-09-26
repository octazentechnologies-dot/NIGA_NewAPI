using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V6;

public interface IV6BenchmarkEvaluationService
{
    Task<V6BenchmarkReport> EvaluateAsync(
        IReadOnlyList<V6BenchmarkCase> cases,
        Func<V6BenchmarkCase, Task<IReadOnlyList<AudioCaseSuggestedRubricModel>>> runCaseAsync,
        CancellationToken cancellationToken = default);

    IReadOnlyList<V6BenchmarkCase> GetCuratedBenchmarkCases();
}

/// <summary>V6 self-validation against expert-reviewed consultation benchmarks.</summary>
public class V6BenchmarkEvaluationService : IV6BenchmarkEvaluationService
{
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<V6BenchmarkEvaluationService> _logger;

    public V6BenchmarkEvaluationService(
        IOptions<RubricIntelligenceOptions> options,
        ILogger<V6BenchmarkEvaluationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IReadOnlyList<V6BenchmarkCase> GetCuratedBenchmarkCases() =>
        new List<V6BenchmarkCase>
        {
            new()
            {
                CaseId = "epilepsy-aura-convulsion",
                Description = "Epilepsy case: aura before convulsion, fear, salt craving",
                ExpectedRubricPatterns = new List<string>
                {
                    "convulsion", "aura", "fear", "salt", "mind", "before",
                },
                ForbiddenGenericPatterns = new List<string> { "pain", "weakness" },
            },
            new()
            {
                CaseId = "anxiety-restlessness",
                Description = "Anxiety with restlessness and palpitation",
                ExpectedRubricPatterns = new List<string>
                {
                    "anxiety", "restless", "palpitat", "mind",
                },
                ForbiddenGenericPatterns = new List<string> { "headache" },
            },
            new()
            {
                CaseId = "fever-chill-thirst",
                Description = "Acute fever with chill and thirst",
                ExpectedRubricPatterns = new List<string>
                {
                    "fever", "chill", "thirst", "generals",
                },
                ForbiddenGenericPatterns = new List<string>(),
            },
            new()
            {
                CaseId = "grief-suppression",
                Description = "Grief with emotional suppression",
                ExpectedRubricPatterns = new List<string>
                {
                    "grief", "sad", "mind", "weep",
                },
                ForbiddenGenericPatterns = new List<string>(),
            },
            new()
            {
                CaseId = "digestive-nausea-food",
                Description = "Nausea worse after eating, food aversion",
                ExpectedRubricPatterns = new List<string>
                {
                    "nausea", "eating", "aversion", "stomach",
                },
                ForbiddenGenericPatterns = new List<string>(),
            },
        };

    public async Task<V6BenchmarkReport> EvaluateAsync(
        IReadOnlyList<V6BenchmarkCase> cases,
        Func<V6BenchmarkCase, Task<IReadOnlyList<AudioCaseSuggestedRubricModel>>> runCaseAsync,
        CancellationToken cancellationToken = default)
    {
        var report = new V6BenchmarkReport
        {
            GeneratedAtUtc = DateTime.UtcNow,
            CasesEvaluated = cases.Count,
        };

        var totalExpected = 0;
        var totalMatched = 0;
        var totalProduced = 0;
        var totalFalsePositives = 0;
        var duplicateCount = 0;
        var aiConceptUseful = 0;
        var aiConceptTotal = 0;

        foreach (var benchmarkCase in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rubrics = await runCaseAsync(benchmarkCase);
            var repertoryRubrics = rubrics
                .Where(r => !string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var aiConcepts = rubrics
                .Where(r => string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var caseResult = EvaluateCase(benchmarkCase, repertoryRubrics);
            report.CaseResults.Add(caseResult);

            totalExpected += caseResult.ExpectedCount;
            totalMatched += caseResult.MatchedCount;
            totalProduced += caseResult.ProducedCount;
            totalFalsePositives += caseResult.UnexpectedRubrics.Count;

            duplicateCount += repertoryRubrics
                .GroupBy(r => r.SubSectionId)
                .Count(g => g.Count() > 1);

            aiConceptTotal += aiConcepts.Count;
            aiConceptUseful += aiConcepts.Count(c =>
                !string.IsNullOrWhiteSpace(c.WhySuggested)
                && c.WhySuggested.Length > 10);
        }

        report.SqlRubricRecall = totalExpected > 0
            ? Math.Round((decimal)totalMatched / totalExpected * 100m, 2)
            : 0m;
        report.SqlRubricPrecision = totalProduced > 0
            ? Math.Round((decimal)(totalProduced - totalFalsePositives) / totalProduced * 100m, 2)
            : 0m;
        report.ExpertAgreementRate = report.SqlRubricRecall;
        report.DuplicateRate = totalProduced > 0
            ? Math.Round((decimal)duplicateCount / totalProduced * 100m, 2)
            : 0m;
        report.FalsePositiveRate = totalProduced > 0
            ? Math.Round((decimal)totalFalsePositives / totalProduced * 100m, 2)
            : 0m;
        report.AiConceptUsefulness = aiConceptTotal > 0
            ? Math.Round((decimal)aiConceptUseful / aiConceptTotal * 100m, 2)
            : 0m;

        report.IdentifiedGaps = BuildGaps(report);
        report.ProposedImprovements = BuildImprovements(report);

        _logger.LogInformation(
            "V6 benchmark: cases={Cases} recall={Recall}% precision={Precision}% duplicates={Dup}% falsePos={Fp}%",
            report.CasesEvaluated,
            report.SqlRubricRecall,
            report.SqlRubricPrecision,
            report.DuplicateRate,
            report.FalsePositiveRate);

        return report;
    }

    private static V6BenchmarkCaseResult EvaluateCase(
        V6BenchmarkCase benchmarkCase,
        IReadOnlyList<AudioCaseSuggestedRubricModel> repertoryRubrics)
    {
        var rubricNames = repertoryRubrics
            .Select(r => r.SubSectionName ?? string.Empty)
            .ToList();

        var matched = new List<string>();
        var missed = new List<string>();
        foreach (var pattern in benchmarkCase.ExpectedRubricPatterns)
        {
            if (rubricNames.Any(n => n.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                matched.Add(pattern);
            }
            else
            {
                missed.Add(pattern);
            }
        }

        var unexpected = rubricNames
            .Where(n => benchmarkCase.ForbiddenGenericPatterns.Any(f =>
                n.Contains(f, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var expectedCount = benchmarkCase.ExpectedRubricPatterns.Count;
        var matchedCount = matched.Count;
        var producedCount = repertoryRubrics.Count;

        return new V6BenchmarkCaseResult
        {
            CaseId = benchmarkCase.CaseId,
            ExpectedCount = expectedCount,
            MatchedCount = matchedCount,
            ProducedCount = producedCount,
            Recall = expectedCount > 0 ? Math.Round((decimal)matchedCount / expectedCount * 100m, 2) : 0m,
            Precision = producedCount > 0
                ? Math.Round((decimal)(producedCount - unexpected.Count) / producedCount * 100m, 2)
                : 0m,
            MatchedPatterns = matched,
            MissedPatterns = missed,
            UnexpectedRubrics = unexpected,
        };
    }

    private static List<string> BuildGaps(V6BenchmarkReport report)
    {
        var gaps = new List<string>();

        if (report.SqlRubricRecall < 70m)
        {
            gaps.Add($"SQL rubric recall below target ({report.SqlRubricRecall}% < 70%)");
        }

        if (report.FalsePositiveRate > 15m)
        {
            gaps.Add($"False-positive rate elevated ({report.FalsePositiveRate}%)");
        }

        if (report.DuplicateRate > 5m)
        {
            gaps.Add($"Duplicate rubric rate above threshold ({report.DuplicateRate}%)");
        }

        var missedPatterns = report.CaseResults
            .SelectMany(c => c.MissedPatterns)
            .GroupBy(p => p)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => g.Key);

        foreach (var pattern in missedPatterns)
        {
            gaps.Add($"Frequently missed expected pattern: '{pattern}'");
        }

        return gaps;
    }

    private static List<string> BuildImprovements(V6BenchmarkReport report)
    {
        var improvements = new List<string>();

        if (report.SqlRubricRecall < 80m)
        {
            improvements.Add("Expand bootstrap mappings for frequently missed symptom patterns");
            improvements.Add("Increase synonym alias coverage in RubricAliases table");
        }

        if (report.FalsePositiveRate > 10m)
        {
            improvements.Add("Tighten generic rubric filter and clinical validation thresholds");
        }

        if (report.AiConceptUsefulness < 50m && report.CasesEvaluated > 0)
        {
            improvements.Add("Improve AI clinical concept explanations for unmapped symptoms");
        }

        improvements.Add("Run periodic benchmark after embedding indexer updates");

        return improvements;
    }
}
