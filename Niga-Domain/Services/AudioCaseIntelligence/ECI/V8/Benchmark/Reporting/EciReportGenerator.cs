using System.Text;
using System.Text.Json;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Metrics;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Benchmark.Reporting;

public interface IBenchmarkReportGenerator
{
    Task WriteAllAsync(
        EciBenchmarkReport report,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}

public sealed class BenchmarkReportGenerator : IBenchmarkReportGenerator
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task WriteAllAsync(
        EciBenchmarkReport report,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "report.json"),
            JsonSerializer.Serialize(report, Json),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "report.csv"),
            ToCsv(report),
            cancellationToken);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "report.html"),
            ToHtml(report),
            cancellationToken);
    }

    private static string ToCsv(EciBenchmarkReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("CaseId,Expected,Produced,TP,FP,FN,Precision,Recall,F1,Top1,Top3,Top5,Top10,MRR,NDCG,AvgRank,LatencyMs");
        foreach (var c in report.CaseMetrics)
        {
            sb.AppendLine(string.Join(",",
                Escape(c.CaseId),
                c.ExpectedCount,
                c.ProducedCount,
                c.TruePositives,
                c.FalsePositives,
                c.FalseNegatives,
                c.Precision,
                c.Recall,
                c.F1,
                c.Top1,
                c.Top3,
                c.Top5,
                c.Top10,
                c.Mrr,
                c.Ndcg,
                c.AvgRank,
                c.LatencyMs));
        }

        return sb.ToString();
    }

    private static string Escape(string value) =>
        value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static string ToHtml(EciBenchmarkReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html><html><head><meta charset=\"utf-8\"/>");
        sb.AppendLine("<title>ECI Benchmark Report</title>");
        sb.AppendLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px} table{border-collapse:collapse;width:100%} th,td{border:1px solid #ddd;padding:6px} th{background:#f5f5f5;text-align:left}</style>");
        sb.AppendLine("</head><body>");
        sb.AppendLine($"<h2>ECI Benchmark Report</h2>");
        sb.AppendLine($"<div><b>Engine</b>: {report.EngineVersion} &nbsp; <b>Benchmark</b>: {report.BenchmarkVersion}</div>");
        sb.AppendLine($"<div><b>Cases</b>: {report.CasesEvaluated}</div>");
        sb.AppendLine("<h3>Overall metrics</h3>");
        sb.AppendLine("<ul>");
        sb.AppendLine($"<li><b>Precision</b>: {report.Precision:P2}</li>");
        sb.AppendLine($"<li><b>Recall</b>: {report.Recall:P2}</li>");
        sb.AppendLine($"<li><b>F1</b>: {report.F1:P2}</li>");
        sb.AppendLine($"<li><b>Top5</b>: {report.Top5Accuracy:P2}</li>");
        sb.AppendLine($"<li><b>Top10</b>: {report.Top10Accuracy:P2}</li>");
        sb.AppendLine($"<li><b>MRR</b>: {report.Mrr:0.####}</li>");
        sb.AppendLine($"<li><b>NDCG</b>: {report.Ndcg:0.####}</li>");
        sb.AppendLine($"<li><b>Avg latency</b>: {report.AvgLatencyMs:0.##} ms</li>");
        sb.AppendLine("</ul>");

        sb.AppendLine("<h3>Per-case metrics</h3>");
        sb.AppendLine("<table><thead><tr>");
        sb.AppendLine("<th>CaseId</th><th>Precision</th><th>Recall</th><th>F1</th><th>Top5</th><th>Top10</th><th>MRR</th><th>NDCG</th><th>Latency(ms)</th>");
        sb.AppendLine("</tr></thead><tbody>");
        foreach (var c in report.CaseMetrics)
        {
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(c.CaseId)}</td>");
            sb.AppendLine($"<td>{c.Precision:P2}</td>");
            sb.AppendLine($"<td>{c.Recall:P2}</td>");
            sb.AppendLine($"<td>{c.F1:P2}</td>");
            sb.AppendLine($"<td>{c.Top5:P0}</td>");
            sb.AppendLine($"<td>{c.Top10:P0}</td>");
            sb.AppendLine($"<td>{c.Mrr:0.####}</td>");
            sb.AppendLine($"<td>{c.Ndcg:0.####}</td>");
            sb.AppendLine($"<td>{c.LatencyMs}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }
}

