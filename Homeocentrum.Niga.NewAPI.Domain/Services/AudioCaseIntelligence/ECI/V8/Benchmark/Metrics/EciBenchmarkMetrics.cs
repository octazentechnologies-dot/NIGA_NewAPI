namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;

public sealed class EciBenchmarkCaseMetrics
{
    public string CaseId { get; set; } = string.Empty;
    public int ExpectedCount { get; set; }
    public int ProducedCount { get; set; }
    public int TruePositives { get; set; }
    public int FalsePositives { get; set; }
    public int FalseNegatives { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1 { get; set; }
    public decimal Top1 { get; set; }
    public decimal Top3 { get; set; }
    public decimal Top5 { get; set; }
    public decimal Top10 { get; set; }
    public decimal Mrr { get; set; }
    public decimal Ndcg { get; set; }
    public decimal AvgRank { get; set; }
    public int CandidateCount { get; set; }
    public int LatencyMs { get; set; }
    public int SearchMs { get; set; }
    public int RankingMs { get; set; }
    public int ExtractionMs { get; set; }
    public int TotalMs { get; set; }
}

public sealed class EciBenchmarkReport
{
    public string EngineVersion { get; set; } = "v8.0";
    public string BenchmarkVersion { get; set; } = "v1";
    public string WeightConfigJson { get; set; } = "{}";

    public int CasesEvaluated { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1 { get; set; }
    public decimal Top1Accuracy { get; set; }
    public decimal Top3Accuracy { get; set; }
    public decimal Top5Accuracy { get; set; }
    public decimal Top10Accuracy { get; set; }
    public decimal AvgRank { get; set; }
    public decimal Mrr { get; set; }
    public decimal Ndcg { get; set; }

    public decimal AvgLatencyMs { get; set; }
    public decimal AvgSearchMs { get; set; }
    public decimal AvgRankingMs { get; set; }
    public decimal AvgExtractionMs { get; set; }
    public decimal AvgTotalMs { get; set; }

    public List<EciBenchmarkCaseMetrics> CaseMetrics { get; set; } = new();
    public List<EciBenchmarkRubricMiss> TopMissedRubrics { get; set; } = new();
    public List<EciBenchmarkRubricFalsePositive> TopFalsePositives { get; set; } = new();
}

public sealed class EciBenchmarkRubricMiss
{
    public string RubricText { get; set; } = string.Empty;
    public int MissCount { get; set; }
    public List<string> ExampleCaseIds { get; set; } = new();
}

public sealed class EciBenchmarkRubricFalsePositive
{
    public string RubricText { get; set; } = string.Empty;
    public int FalsePositiveCount { get; set; }
    public List<string> ExampleCaseIds { get; set; } = new();
}

