using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.AiEmbeddingInfrastructure;

public class AiIncrementalEmbeddingQueueProcessorTests
{
    [Fact]
    public void ParsePayload_reads_change_metadata()
    {
        const string json =
            """{"changeType":"EnrichmentChanged","reasons":["DoctorApprovedConcept","HashComparison"],"detectedAtUtc":"2026-06-23T12:00:00Z"}""";

        var payload = AiIncrementalEmbeddingQueueProcessor.ParsePayload(json);

        Assert.Equal(AiEmbeddingQueueChangeTypes.EnrichmentChanged, payload.ChangeType);
        Assert.Contains(AiEmbeddingChangeReasons.DoctorApprovedConcept, payload.Reasons);
        Assert.Contains(AiEmbeddingChangeReasons.HashComparison, payload.Reasons);
    }

    [Fact]
    public void ParsePayload_returns_default_for_invalid_json()
    {
        var payload = AiIncrementalEmbeddingQueueProcessor.ParsePayload("{not-json");
        Assert.NotNull(payload);
        Assert.Equal(AiEmbeddingQueueChangeTypes.Updated, payload.ChangeType);
    }

    [Fact]
    public void ParsePayload_reads_deleted_change_type()
    {
        const string json =
            """{"changeType":"Deleted","reasons":["DeletedDate"],"detectedAtUtc":"2026-06-23T12:00:00Z"}""";

        var parsed = AiIncrementalEmbeddingQueueProcessor.ParsePayload(json);
        Assert.Equal(AiEmbeddingQueueChangeTypes.Deleted, parsed.ChangeType);
    }
}
