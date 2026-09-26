using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.AiEmbeddingInfrastructure;

public class AiEmbeddingRetryPolicyTests
{
    private static AiEmbeddingInfrastructureOptions CreateOptions() => new()
    {
        RetryBaseDelaySeconds = 30,
        RetryMaxDelaySeconds = 3600,
    };

    [Fact]
    public void CanRetry_returns_true_when_attempts_remain()
    {
        Assert.True(AiEmbeddingRetryPolicy.CanRetry(1, 3));
        Assert.False(AiEmbeddingRetryPolicy.CanRetry(3, 3));
    }

    [Fact]
    public void ResolveQueueStatusAfterFailure_returns_deadletter_when_max_attempts_reached()
    {
        var status = AiEmbeddingRetryPolicy.ResolveQueueStatusAfterFailure(3, 3);
        Assert.Equal(DTOs.AiEmbeddingStatuses.DeadLetter, status);
    }

    [Fact]
    public void ComputeNextRetryUtc_uses_exponential_backoff()
    {
        var options = CreateOptions();
        var first = AiEmbeddingRetryPolicy.ComputeNextRetryUtc(1, options);
        var second = AiEmbeddingRetryPolicy.ComputeNextRetryUtc(2, options);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(second > first);
    }

    [Fact]
    public void ComputeNextRetryUtc_caps_at_max_delay()
    {
        var options = CreateOptions();
        var retry = AiEmbeddingRetryPolicy.ComputeNextRetryUtc(20, options);
        Assert.NotNull(retry);
        Assert.True(retry.Value <= DateTime.UtcNow.AddSeconds(options.RetryMaxDelaySeconds + 1));
    }
}
