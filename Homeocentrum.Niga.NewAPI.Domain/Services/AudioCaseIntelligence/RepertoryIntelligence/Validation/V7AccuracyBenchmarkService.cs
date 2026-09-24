using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Validation;

/// <summary>V7: accuracy benchmarking with precision, recall, F1, top-5/10 agreement.</summary>
public interface IV7AccuracyBenchmarkService
{
    V7BenchmarkAccuracyReport Evaluate(
        IReadOnlyList<V7BenchmarkCaseAccuracyInput> cases);

    IReadOnlyList<V7BenchmarkCaseAccuracyInput> GetEpilepsyReferenceCases();
}

public class V7BenchmarkCaseAccuracyInput
{
    public string CaseId { get; set; } = string.Empty;

    public List<string> ExpectedPatterns { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> ProducedRubrics { get; set; } = new();
}

public class V7AccuracyBenchmarkService : IV7AccuracyBenchmarkService
{
    public IReadOnlyList<V7BenchmarkCaseAccuracyInput> GetEpilepsyReferenceCases() =>
        new List<V7BenchmarkCaseAccuracyInput>
        {
            new()
            {
                CaseId = "epilepsy-comprehensive",
                ExpectedPatterns = new List<string>
                {
                    "fear", "convulsion", "aura", "awkward", "drops", "salt", "thirst",
                    "sleep", "height", "meat", "forget",
                },
            },
        };

    public V7BenchmarkAccuracyReport Evaluate(
        IReadOnlyList<V7BenchmarkCaseAccuracyInput> cases)
    {
        var report = new V7BenchmarkAccuracyReport { CasesEvaluated = cases.Count };
        var totalExpected = 0;
        var totalMatched = 0;
        var totalProduced = 0;
        var totalFalsePositives = 0;
        var top5Hits = 0;
        var top10Hits = 0;

        foreach (var benchmarkCase in cases)
        {
            var repertory = benchmarkCase.ProducedRubrics
                .Where(r => r.SubSectionId > 0)
                .ToList();

            var names = repertory.Select(r => r.SubSectionName ?? string.Empty).ToList();
            var top5 = names.Take(5).ToList();
            var top10 = names.Take(10).ToList();

            var matched = new List<string>();
            var missed = new List<string>();
            foreach (var pattern in benchmarkCase.ExpectedPatterns)
            {
                if (names.Any(n => n.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    matched.Add(pattern);
                }
                else
                {
                    missed.Add(pattern);
                }
            }

            var caseTop5 = benchmarkCase.ExpectedPatterns.Count(p =>
                top5.Any(n => n.Contains(p, StringComparison.OrdinalIgnoreCase)));
            var caseTop10 = benchmarkCase.ExpectedPatterns.Count(p =>
                top10.Any(n => n.Contains(p, StringComparison.OrdinalIgnoreCase)));

            top5Hits += caseTop5;
            top10Hits += caseTop10;
            totalExpected += benchmarkCase.ExpectedPatterns.Count;
            totalMatched += matched.Count;
            totalProduced += repertory.Count;

            var unexpected = repertory.Count - matched.Count;
            totalFalsePositives += Math.Max(0, unexpected);

            var recall = benchmarkCase.ExpectedPatterns.Count > 0
                ? (decimal)matched.Count / benchmarkCase.ExpectedPatterns.Count
                : 0m;
            var precision = repertory.Count > 0
                ? (decimal)matched.Count / repertory.Count
                : 0m;
            var f1 = recall + precision > 0
                ? 2m * recall * precision / (recall + precision)
                : 0m;

            report.CaseResults.Add(new V7BenchmarkCaseAccuracy
            {
                CaseId = benchmarkCase.CaseId,
                Recall = Math.Round(recall * 100m, 2),
                Precision = Math.Round(precision * 100m, 2),
                F1Score = Math.Round(f1 * 100m, 2),
                Top5Agreement = benchmarkCase.ExpectedPatterns.Count > 0
                    ? Math.Round((decimal)caseTop5 / benchmarkCase.ExpectedPatterns.Count * 100m, 2)
                    : 0m,
                Top10Agreement = benchmarkCase.ExpectedPatterns.Count > 0
                    ? Math.Round((decimal)caseTop10 / benchmarkCase.ExpectedPatterns.Count * 100m, 2)
                    : 0m,
                MatchedPatterns = matched,
                MissedPatterns = missed,
                DiagnosticNotes = missed.Select(m => $"Missed pattern '{m}' — check synonym/ontology/bootstrap coverage").ToList(),
            });
        }

        report.Recall = totalExpected > 0
            ? Math.Round((decimal)totalMatched / totalExpected * 100m, 2)
            : 0m;
        report.Precision = totalProduced > 0
            ? Math.Round((decimal)totalMatched / totalProduced * 100m, 2)
            : 0m;
        report.F1Score = report.Recall + report.Precision > 0
            ? Math.Round(2m * report.Recall * report.Precision / (report.Recall + report.Precision), 2)
            : 0m;
        report.Top5Agreement = totalExpected > 0
            ? Math.Round((decimal)top5Hits / totalExpected * 100m, 2)
            : 0m;
        report.Top10Agreement = totalExpected > 0
            ? Math.Round((decimal)top10Hits / totalExpected * 100m, 2)
            : 0m;

        if (report.Recall < 80m)
        {
            report.IdentifiedGaps.Add($"Recall {report.Recall}% below 80% target");
        }

        foreach (var missed in report.CaseResults.SelectMany(c => c.MissedPatterns).Distinct())
        {
            report.IdentifiedGaps.Add($"Frequently missed: '{missed}'");
        }

        return report;
    }
}
