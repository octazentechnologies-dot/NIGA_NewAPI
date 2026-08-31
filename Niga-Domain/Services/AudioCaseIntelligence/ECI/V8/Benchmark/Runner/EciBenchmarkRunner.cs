using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Entities;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Storage;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Runner;

public interface IBenchmarkRunner
{
    Task<EciBenchmarkReport> RunAsync(
        string benchmarkStoreDirectory,
        string engineVersion,
        string benchmarkVersionCode,
        RubricIntelligenceOptions optionsSnapshot,
        Func<EciBenchmarkCase, Task<(List<AudioCaseSuggestedRubricModel> Rubrics, EciV8Diagnostics? Diagnostics)>> runCaseAsync,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Offline benchmark runner. Stores results and metrics in a dedicated SQLite DB.
/// Does not depend on runtime APIs.
/// </summary>
public sealed class BenchmarkRunner : IBenchmarkRunner
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ILogger<BenchmarkRunner> _logger;

    public BenchmarkRunner(ILogger<BenchmarkRunner> logger)
    {
        _logger = logger;
    }

    public async Task<EciBenchmarkReport> RunAsync(
        string benchmarkStoreDirectory,
        string engineVersion,
        string benchmarkVersionCode,
        RubricIntelligenceOptions optionsSnapshot,
        Func<EciBenchmarkCase, Task<(List<AudioCaseSuggestedRubricModel> Rubrics, EciV8Diagnostics? Diagnostics)>> runCaseAsync,
        CancellationToken cancellationToken = default)
    {
        IEciBenchmarkStore store = new EciBenchmarkFileStore(benchmarkStoreDirectory);
        var benchmarkVersion = await store.GetOrCreateVersionAsync(benchmarkVersionCode, cancellationToken);
        var cases = await store.GetCasesAsync(benchmarkVersion.BenchmarkVersionId, cancellationToken);

        var run = new EciBenchmarkRun
        {
            BenchmarkVersionId = benchmarkVersion.BenchmarkVersionId,
            EngineVersion = engineVersion,
            WeightConfigJson = JsonSerializer.Serialize(optionsSnapshot.EciV8RankingWeights, Json),
            StartedAtUtc = DateTime.UtcNow,
        };
        await store.CreateRunAsync(run, cancellationToken);

        var report = new EciBenchmarkReport
        {
            EngineVersion = engineVersion,
            BenchmarkVersion = benchmarkVersionCode,
            WeightConfigJson = run.WeightConfigJson,
        };

        var allTp = 0;
        var allFp = 0;
        var allFn = 0;
        var top1 = 0m;
        var top3 = 0m;
        var top5 = 0m;
        var top10 = 0m;
        var sumAvgRank = 0m;
        var sumMrr = 0m;
        var sumNdcg = 0m;

        var lat = new List<int>();
        var search = new List<int>();
        var ranking = new List<int>();
        var extraction = new List<int>();
        var total = new List<int>();

        foreach (var c in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var expected = await store.GetExpectedRubricsAsync(c.BenchmarkCaseId, cancellationToken);

            var expectedIds = expected.Where(e => e.SubSectionId.HasValue).Select(e => e.SubSectionId!.Value).Distinct().ToList();
            var expectedText = expected.Where(e => !string.IsNullOrWhiteSpace(e.RubricText)).Select(e => e.RubricText).ToList();

            var sw = Stopwatch.StartNew();
            var (rubrics, diag) = await runCaseAsync(c);
            sw.Stop();

            var produced = rubrics
                .Where(r => r.SubSectionId > 0 && !string.IsNullOrWhiteSpace(r.SubSectionName))
                .ToList();

            var producedRankById = produced
                .Select((r, idx) => (r.SubSectionId, Rank: idx + 1))
                .ToDictionary(x => x.SubSectionId, x => x.Rank);

            // Matching strategy: prefer SubSectionId; fallback to text contains for cases without IDs.
            var tp = 0;
            foreach (var id in expectedIds)
            {
                if (producedRankById.ContainsKey(id)) tp++;
            }

            if (expectedIds.Count == 0 && expectedText.Count > 0)
            {
                tp = expectedText.Count(t => produced.Any(p => (p.SubSectionName ?? string.Empty).Contains(t, StringComparison.OrdinalIgnoreCase)));
            }

            var fp = Math.Max(0, produced.Count - tp);
            var fn = Math.Max(0, (expectedIds.Count > 0 ? expectedIds.Count : expectedText.Count) - tp);

            var (pScore, rScore, f1Score) = EciMetricsCalculator.ComputePrecisionRecallF1(tp, fp, fn);

            var hitRanks = expectedIds
                .Select(id => producedRankById.TryGetValue(id, out var rankVal) ? rankVal : 0)
                .Where(rk => rk > 0)
                .ToList();

            var mrr = EciMetricsCalculator.ComputeMrr(hitRanks);
            var ndcg = expectedIds.Count > 0 ? EciMetricsCalculator.ComputeNdcg(expectedIds, producedRankById) : 0m;
            var avgRank = hitRanks.Count > 0 ? Math.Round((decimal)hitRanks.Average(), 4) : 0m;

            var top1Hit = expectedIds.Count > 0 ? EciMetricsCalculator.ComputeTopKAccuracy(1, producedRankById, expectedIds) : 0m;
            var top3Hit = expectedIds.Count > 0 ? EciMetricsCalculator.ComputeTopKAccuracy(3, producedRankById, expectedIds) : 0m;
            var top5Hit = expectedIds.Count > 0 ? EciMetricsCalculator.ComputeTopKAccuracy(5, producedRankById, expectedIds) : 0m;
            var top10Hit = expectedIds.Count > 0 ? EciMetricsCalculator.ComputeTopKAccuracy(10, producedRankById, expectedIds) : 0m;

            allTp += tp;
            allFp += fp;
            allFn += fn;
            top1 += top1Hit;
            top3 += top3Hit;
            top5 += top5Hit;
            top10 += top10Hit;
            sumAvgRank += avgRank;
            sumMrr += mrr;
            sumNdcg += ndcg;

            // Latency breakdown (if provided by diagnostics).
            var diagLookup = diag?.Stages.ToDictionary(s => s.Stage, s => s.DurationMs) ?? new Dictionary<string, int>();
            extraction.Add(diagLookup.GetValueOrDefault(EciV8StageNames.StructuredSymptomExtraction, 0));
            search.Add(diagLookup.GetValueOrDefault(EciV8StageNames.DatabaseIntelligence, 0));
            ranking.Add(diagLookup.GetValueOrDefault(EciV8StageNames.RubricRanking, 0));
            lat.Add((int)sw.ElapsedMilliseconds);
            total.Add((int)sw.ElapsedMilliseconds);

            report.CaseMetrics.Add(new EciBenchmarkCaseMetrics
            {
                CaseId = c.CaseId,
                ExpectedCount = expectedIds.Count > 0 ? expectedIds.Count : expectedText.Count,
                ProducedCount = produced.Count,
                TruePositives = tp,
                FalsePositives = fp,
                FalseNegatives = fn,
                Precision = pScore,
                Recall = rScore,
                F1 = f1Score,
                Top1 = top1Hit,
                Top3 = top3Hit,
                Top5 = top5Hit,
                Top10 = top10Hit,
                Mrr = mrr,
                Ndcg = ndcg,
                AvgRank = avgRank,
                CandidateCount = 0,
                LatencyMs = (int)sw.ElapsedMilliseconds,
                SearchMs = diagLookup.GetValueOrDefault(EciV8StageNames.DatabaseIntelligence, 0),
                RankingMs = diagLookup.GetValueOrDefault(EciV8StageNames.RubricRanking, 0),
                ExtractionMs = diagLookup.GetValueOrDefault(EciV8StageNames.StructuredSymptomExtraction, 0),
                TotalMs = (int)sw.ElapsedMilliseconds,
            });

            // Store produced rubrics.
            var candidateCount = produced.Count;
            var runRubrics = new List<EciBenchmarkRunRubric>();
            for (var i = 0; i < produced.Count; i++)
            {
                var r = produced[i];
                runRubrics.Add(new EciBenchmarkRunRubric
                {
                    BenchmarkRunId = run.BenchmarkRunId,
                    BenchmarkCaseId = c.BenchmarkCaseId,
                    SubSectionId = r.SubSectionId,
                    RubricText = r.SubSectionName ?? string.Empty,
                    Rank = i + 1,
                    Score = r.QualityScore ?? (r.ConfidenceScore ?? r.MatchScore) * 100m,
                    Explainability = r.SelectionReason,
                    CandidateCount = candidateCount,
                });
            }
            await store.AppendRunRubricsAsync(runRubrics, cancellationToken);

            // Per-rubric miss analysis.
            var analyses = new List<EciBenchmarkPerRubricAnalysis>();
            foreach (var exp in expected)
            {
                var expId = exp.SubSectionId;
                var expTextRub = exp.RubricText;
                var producedMatch = expId.HasValue
                    ? produced.FirstOrDefault(p => p.SubSectionId == expId.Value)
                    : produced.FirstOrDefault(p => (p.SubSectionName ?? string.Empty).Contains(expTextRub, StringComparison.OrdinalIgnoreCase));

                if (producedMatch == null)
                {
                    analyses.Add(new EciBenchmarkPerRubricAnalysis
                    {
                        BenchmarkRunId = run.BenchmarkRunId,
                        BenchmarkCaseId = c.BenchmarkCaseId,
                        ExpectedRubric = expTextRub,
                        ExpectedRubricId = expId,
                        ProducedRubric = null,
                        ProducedRank = null,
                        FailureClass = "Missed",
                        FailureReason = ClassifyMissReason(exp),
                        Evidence = null,
                        Score = null,
                        CandidateCount = candidateCount,
                    });
                }
            }
            if (analyses.Count > 0)
            {
                await store.AppendPerRubricAnalysesAsync(analyses, cancellationToken);
            }
        }

        run.CompletedAtUtc = DateTime.UtcNow;

        report.CasesEvaluated = report.CaseMetrics.Count;
        var (prec, rec, f1) = EciMetricsCalculator.ComputePrecisionRecallF1(allTp, allFp, allFn);
        report.Precision = prec;
        report.Recall = rec;
        report.F1 = f1;
        report.Top1Accuracy = report.CasesEvaluated == 0 ? 0m : Math.Round(top1 / report.CasesEvaluated, 4);
        report.Top3Accuracy = report.CasesEvaluated == 0 ? 0m : Math.Round(top3 / report.CasesEvaluated, 4);
        report.Top5Accuracy = report.CasesEvaluated == 0 ? 0m : Math.Round(top5 / report.CasesEvaluated, 4);
        report.Top10Accuracy = report.CasesEvaluated == 0 ? 0m : Math.Round(top10 / report.CasesEvaluated, 4);
        report.AvgRank = report.CasesEvaluated == 0 ? 0m : Math.Round(sumAvgRank / report.CasesEvaluated, 4);
        report.Mrr = report.CasesEvaluated == 0 ? 0m : Math.Round(sumMrr / report.CasesEvaluated, 4);
        report.Ndcg = report.CasesEvaluated == 0 ? 0m : Math.Round(sumNdcg / report.CasesEvaluated, 4);

        report.AvgLatencyMs = lat.Count == 0 ? 0m : Math.Round((decimal)lat.Average(), 2);
        report.AvgSearchMs = search.Count == 0 ? 0m : Math.Round((decimal)search.Average(), 2);
        report.AvgRankingMs = ranking.Count == 0 ? 0m : Math.Round((decimal)ranking.Average(), 2);
        report.AvgExtractionMs = extraction.Count == 0 ? 0m : Math.Round((decimal)extraction.Average(), 2);
        report.AvgTotalMs = total.Count == 0 ? 0m : Math.Round((decimal)total.Average(), 2);

        var metricsEntity = new EciBenchmarkRunMetrics
        {
            BenchmarkRunId = run.BenchmarkRunId,
            CasesEvaluated = report.CasesEvaluated,
            Precision = report.Precision,
            Recall = report.Recall,
            F1Score = report.F1,
            Top1Accuracy = report.Top1Accuracy,
            Top3Accuracy = report.Top3Accuracy,
            Top5Accuracy = report.Top5Accuracy,
            Top10Accuracy = report.Top10Accuracy,
            Mrr = report.Mrr,
            Ndcg = report.Ndcg,
            AvgRank = report.AvgRank,
            AvgLatencyMs = report.AvgLatencyMs,
            AvgSearchMs = report.AvgSearchMs,
            AvgRankingMs = report.AvgRankingMs,
            AvgExtractionMs = report.AvgExtractionMs,
            AvgTotalMs = report.AvgTotalMs,
            ReportJson = JsonSerializer.Serialize(report, Json),
        };
        await store.SaveRunMetricsAsync(metricsEntity, cancellationToken);

        return report;
    }

    private static string ClassifyMissReason(EciBenchmarkExpectedRubric expected)
    {
        // Phase C.5 initial classifier (deterministic). This will be expanded once we store candidate provenance per run.
        if (expected.SubSectionId == null)
        {
            return "Gold standard rubric missing SubSectionId (text-only match).";
        }

        return "Expected rubric not produced in top-N. Possible: SQL miss / synonym miss / embedding miss / ranking mistake.";
    }
}

