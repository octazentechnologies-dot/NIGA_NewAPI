using System.Text.Json;
using Microsoft.Extensions.Logging;
using Niga_Domain.Configuration;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Calibration;

public interface ICalibrationRunner
{
    Task<List<EciCalibrationResult>> RunAsync(
        Func<RubricIntelligenceOptions, Task<EciBenchmarkReport>> runBenchmarkWithOptions,
        IEnumerable<EciV8RankingWeights> weightGrid,
        CancellationToken cancellationToken = default);
}

public sealed class EciCalibrationResult
{
    public string WeightConfigJson { get; set; } = "{}";
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1 { get; set; }
    public decimal Top10 { get; set; }
    public decimal AvgLatencyMs { get; set; }
}

/// <summary>
/// Offline calibration mode: evaluate same benchmark dataset over many weight configurations.
/// No runtime edits; produces sorted results.
/// </summary>
public sealed class CalibrationRunner : ICalibrationRunner
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly ILogger<CalibrationRunner> _logger;

    public CalibrationRunner(ILogger<CalibrationRunner> logger)
    {
        _logger = logger;
    }

    public async Task<List<EciCalibrationResult>> RunAsync(
        Func<RubricIntelligenceOptions, Task<EciBenchmarkReport>> runBenchmarkWithOptions,
        IEnumerable<EciV8RankingWeights> weightGrid,
        CancellationToken cancellationToken = default)
    {
        var results = new List<EciCalibrationResult>();

        foreach (var weights in weightGrid)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var options = new RubricIntelligenceOptions
            {
                EciV8RankingWeights = weights,
            };

            var report = await runBenchmarkWithOptions(options);
            results.Add(new EciCalibrationResult
            {
                WeightConfigJson = JsonSerializer.Serialize(weights, Json),
                Precision = report.Precision,
                Recall = report.Recall,
                F1 = report.F1,
                Top10 = report.Top10Accuracy,
                AvgLatencyMs = report.AvgLatencyMs,
            });
        }

        return results
            .OrderByDescending(r => r.F1)
            .ThenByDescending(r => r.Recall)
            .ThenByDescending(r => r.Top10)
            .ThenBy(r => r.AvgLatencyMs)
            .ToList();
    }
}

