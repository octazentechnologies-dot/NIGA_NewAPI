using Homeocentrum.Niga.API.Domain.Services;
using Xunit;

namespace Homeocentrum.Niga.API.Domain.Tests.Services;

public class PushOutboxBackoffTests
{
    [Theory]
    [InlineData(0, 15)]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(3, 120)]
    [InlineData(7, 1800)]
    public void DelayAfterFailure_is_bounded_exponential(int attempt, int expectedSeconds)
        => Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), PushOutboxBackoff.DelayAfterFailure(attempt));

    [Fact]
    public void IsDead_after_max_attempts()
    {
        Assert.False(PushOutboxBackoff.IsDead(PushOutboxBackoff.MaxAttempts - 1));
        Assert.True(PushOutboxBackoff.IsDead(PushOutboxBackoff.MaxAttempts));
    }
}
