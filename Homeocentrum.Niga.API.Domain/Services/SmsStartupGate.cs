using Homeocentrum.Niga.API.Domain.Configuration;
using Microsoft.Extensions.Options;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Shared clock for SMS background workers so API/IIS start and deploy do not blast SMS.
/// Request-path OTP/notification sends do not use this gate.
/// </summary>
public sealed class SmsStartupGate
{
    private readonly IOptions<SmsOptions> _options;

    public SmsStartupGate(IOptions<SmsOptions> options)
    {
        _options = options;
        ProcessStartedUtc = DateTime.UtcNow;
    }

    public DateTime ProcessStartedUtc { get; }

    public TimeSpan GracePeriod
    {
        get
        {
            var minutes = Math.Clamp(_options.Value?.StartupGraceMinutes ?? 5, 0, 120);
            return TimeSpan.FromMinutes(minutes);
        }
    }

    public bool IsInGracePeriod => DateTime.UtcNow < ProcessStartedUtc + GracePeriod;

    public TimeSpan RemainingGrace
    {
        get
        {
            var end = ProcessStartedUtc + GracePeriod;
            var left = end - DateTime.UtcNow;
            return left > TimeSpan.Zero ? left : TimeSpan.Zero;
        }
    }
}
