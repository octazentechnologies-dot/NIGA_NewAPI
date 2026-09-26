using System.Text.Json;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Storage;

public interface IEciBenchmarkStore
{
    Task<EciBenchmarkVersion> GetOrCreateVersionAsync(string versionCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EciBenchmarkCase>> GetCasesAsync(Guid benchmarkVersionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EciBenchmarkExpectedRubric>> GetExpectedRubricsAsync(Guid benchmarkCaseId, CancellationToken cancellationToken = default);
    Task<Guid> CreateRunAsync(EciBenchmarkRun run, CancellationToken cancellationToken = default);
    Task AppendRunRubricsAsync(IReadOnlyList<EciBenchmarkRunRubric> rubrics, CancellationToken cancellationToken = default);
    Task AppendPerRubricAnalysesAsync(IReadOnlyList<EciBenchmarkPerRubricAnalysis> analyses, CancellationToken cancellationToken = default);
    Task SaveRunMetricsAsync(EciBenchmarkRunMetrics metrics, CancellationToken cancellationToken = default);
}

/// <summary>
/// Offline-only file store (JSON lines). Each entity type is isolated into its own file.
/// This avoids any production DB/schema impact and requires no extra packages.
/// </summary>
public sealed class EciBenchmarkFileStore : IEciBenchmarkStore
{
    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _root;

    public EciBenchmarkFileStore(string rootDirectory)
    {
        _root = rootDirectory;
        Directory.CreateDirectory(_root);
    }

    public async Task<EciBenchmarkVersion> GetOrCreateVersionAsync(string versionCode, CancellationToken cancellationToken = default)
    {
        var versions = await ReadAllAsync<EciBenchmarkVersion>(Path.Combine(_root, "benchmark_versions.jsonl"), cancellationToken);
        var v = versions.FirstOrDefault(x => x.VersionCode == versionCode);
        if (v != null) return v;

        v = new EciBenchmarkVersion { VersionCode = versionCode, Description = "Auto-created" };
        await AppendAsync(Path.Combine(_root, "benchmark_versions.jsonl"), v, cancellationToken);
        return v;
    }

    public async Task<IReadOnlyList<EciBenchmarkCase>> GetCasesAsync(Guid benchmarkVersionId, CancellationToken cancellationToken = default)
    {
        var cases = await ReadAllAsync<EciBenchmarkCase>(Path.Combine(_root, "benchmark_cases.jsonl"), cancellationToken);
        return cases.Where(c => c.BenchmarkVersionId == benchmarkVersionId).ToList();
    }

    public async Task<IReadOnlyList<EciBenchmarkExpectedRubric>> GetExpectedRubricsAsync(Guid benchmarkCaseId, CancellationToken cancellationToken = default)
    {
        var rubrics = await ReadAllAsync<EciBenchmarkExpectedRubric>(Path.Combine(_root, "expected_rubrics.jsonl"), cancellationToken);
        return rubrics.Where(r => r.BenchmarkCaseId == benchmarkCaseId).ToList();
    }

    public async Task<Guid> CreateRunAsync(EciBenchmarkRun run, CancellationToken cancellationToken = default)
    {
        await AppendAsync(Path.Combine(_root, "benchmark_runs.jsonl"), run, cancellationToken);
        return run.BenchmarkRunId;
    }

    public async Task AppendRunRubricsAsync(IReadOnlyList<EciBenchmarkRunRubric> rubrics, CancellationToken cancellationToken = default)
    {
        foreach (var r in rubrics)
        {
            await AppendAsync(Path.Combine(_root, "run_rubrics.jsonl"), r, cancellationToken);
        }
    }

    public async Task AppendPerRubricAnalysesAsync(IReadOnlyList<EciBenchmarkPerRubricAnalysis> analyses, CancellationToken cancellationToken = default)
    {
        foreach (var a in analyses)
        {
            await AppendAsync(Path.Combine(_root, "per_rubric_analysis.jsonl"), a, cancellationToken);
        }
    }

    public async Task SaveRunMetricsAsync(EciBenchmarkRunMetrics metrics, CancellationToken cancellationToken = default)
    {
        await AppendAsync(Path.Combine(_root, "run_metrics.jsonl"), metrics, cancellationToken);
    }

    private static async Task AppendAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        var line = JsonSerializer.Serialize(value, Json);
        await File.AppendAllTextAsync(path, line + Environment.NewLine, cancellationToken);
    }

    private static async Task<List<T>> ReadAllAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path)) return new List<T>();
        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var list = new List<T>(lines.Length);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var item = JsonSerializer.Deserialize<T>(line, Json);
            if (item != null) list.Add(item);
        }
        return list;
    }
}

