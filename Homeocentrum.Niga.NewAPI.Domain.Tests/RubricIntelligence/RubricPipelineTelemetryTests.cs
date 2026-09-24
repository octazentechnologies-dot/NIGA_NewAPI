using Microsoft.Extensions.Logging.Abstractions;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Monitoring;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class RubricPipelineTelemetryTests
{
    private sealed class FakeIntelligenceRepo : IAudioCaseIntelligenceRepository
    {
        public List<(string Stage, string? EngineVersion, int? LatencyMs, string? Details, string? Message)> Logs { get; } = new();

        public Task SaveConceptsAsync(Guid sessionId, IReadOnlyList<ClinicalConceptModel> concepts, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task SaveIntelligenceLogAsync(
            Guid sessionId,
            string? correlationId,
            string stageName,
            string status,
            string? message,
            string? detailsJson,
            int? latencyMs,
            CancellationToken cancellationToken = default,
            string engineVersion = "v2")
        {
            Logs.Add((stageName, engineVersion, latencyMs, detailsJson, message));
            return Task.CompletedTask;
        }

        public Task<List<ClinicalConceptModel>> GetConceptsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<ClinicalConceptModel>());

        public Task SaveCausationLinksAsync(Guid sessionId, IReadOnlyList<CausationLinkModel> links, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<List<CausationLinkModel>> GetCausationLinksAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<CausationLinkModel>());

        public Task<List<HomeopathicWeightRule>> GetActiveWeightRulesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<HomeopathicWeightRule>());

        public Task SaveInferenceLogsAsync(Guid sessionId, IReadOnlyList<ClinicalInferenceLogModel> logs, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    [Fact]
    public async Task Records_stages_and_flushes_summary_without_transcript()
    {
        var repo = new FakeIntelligenceRepo();
        var telemetry = new RubricPipelineTelemetry(
            repo,
            NullLogger<RubricPipelineTelemetry>.Instance);

        var sessionId = Guid.NewGuid();
        telemetry.BeginSession(sessionId, "corr123");
        telemetry.SetEngineVersion("fast-f");
        telemetry.IncrementLlmCalls(2);
        telemetry.IncrementEmbeddingCalls(1);
        telemetry.IncrementSqlQueries(5);
        telemetry.SetCandidateCount(40);
        telemetry.SetFinalRubricCounts(11, 10, 1);

        var start = DateTime.UtcNow.AddSeconds(-2);
        await telemetry.RecordStageAsync("Whisper", 1500, start, DateTime.UtcNow, message: "detectedLanguage=en");
        await telemetry.RecordStageAsync("GptExtraction", 800, start, DateTime.UtcNow);

        await telemetry.FlushSummaryAsync();

        Assert.Contains(repo.Logs, x => x.Stage == "Whisper" && x.EngineVersion == "fast-f");
        Assert.Contains(repo.Logs, x => x.Stage == RubricPipelineTelemetryConstants.SummaryStageName);

        var summary = telemetry.BuildSummary();
        Assert.Equal(sessionId, summary.CaseSessionId);
        Assert.Equal(2, summary.LlmCallCount);
        Assert.Equal(1, summary.EmbeddingCallCount);
        Assert.Equal(5, summary.SqlQueryCount);
        Assert.Equal(11, summary.FinalRubricCount);
        Assert.Equal(10, summary.DbBackedRubricCount);
        Assert.All(repo.Logs, log =>
        {
            Assert.DoesNotContain("Doctor:", log.Details ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Patient:", log.Details ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("transcript", log.Message ?? "", StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Inactive_telemetry_is_noop()
    {
        var repo = new FakeIntelligenceRepo();
        var telemetry = new RubricPipelineTelemetry(
            repo,
            NullLogger<RubricPipelineTelemetry>.Instance);

        telemetry.IncrementLlmCalls(9);
        await telemetry.RecordStageAsync("Whisper", 100, DateTime.UtcNow, DateTime.UtcNow);
        await telemetry.FlushSummaryAsync();

        Assert.Empty(repo.Logs);
    }
}
