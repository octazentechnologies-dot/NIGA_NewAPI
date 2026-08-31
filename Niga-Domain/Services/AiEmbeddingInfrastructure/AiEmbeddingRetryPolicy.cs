using Niga_Domain.Configuration;

namespace Niga_Domain.Services.AiEmbeddingInfrastructure;

public static class AiEmbeddingRetryPolicy
{
    public static DateTime? ComputeNextRetryUtc(int attemptCount, AiEmbeddingInfrastructureOptions options)
    {
        if (attemptCount <= 0) return null;

        var exponent = Math.Min(attemptCount - 1, 10);
        var delaySeconds = options.RetryBaseDelaySeconds * Math.Pow(2, exponent);
        delaySeconds = Math.Min(delaySeconds, options.RetryMaxDelaySeconds);
        return DateTime.UtcNow.AddSeconds(delaySeconds);
    }

    public static bool CanRetry(int attemptCount, int maxAttempts) =>
        attemptCount < maxAttempts;

    public static string ResolveQueueStatusAfterFailure(int attemptCount, int maxAttempts) =>
        CanRetry(attemptCount, maxAttempts)
            ? DTOs.AiEmbeddingStatuses.Pending
            : DTOs.AiEmbeddingStatuses.DeadLetter;
}
