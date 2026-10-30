namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>Bounded exponential backoff for FCM outbox retries.</summary>
public static class PushOutboxBackoff
{
    public const int MaxAttempts = 8;

    /// <summary>Delay before the next attempt after <paramref name="attemptCount"/> failed tries (0-based after first failure).</summary>
    public static TimeSpan DelayAfterFailure(int attemptCount)
    {
        // 15s, 30s, 60s, 2m, 5m, 10m, 20m, 30m (capped)
        var seconds = attemptCount switch
        {
            <= 0 => 15,
            1 => 30,
            2 => 60,
            3 => 120,
            4 => 300,
            5 => 600,
            6 => 1200,
            _ => 1800
        };
        return TimeSpan.FromSeconds(seconds);
    }

    public static bool IsDead(int attemptCount) => attemptCount >= MaxAttempts;
}
