namespace Homeocentrum.Niga.NewAPI.Domain.Services.Tele;

public sealed class TeleVideoIssueRequest
{
    public Guid AppointmentId { get; init; }
    public string RoomId { get; init; } = string.Empty;
    public string Role { get; init; } = "patient";
    public long UserAccountId { get; init; }
    public int TtlMinutes { get; init; } = 60;
}

public sealed class TeleVideoIssueResult
{
    public string Vendor { get; init; } = "Stub";
    public string RoomId { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAtUtc { get; init; }
    /// <summary>Client SDK bootstrap (AppId, channel, etc.). Null when Stub.</summary>
    public IReadOnlyDictionary<string, string>? ClientConfig { get; init; }
}

/// <summary>
/// Pluggable A/V vendor. Stub until Agora/Twilio/Daily keys are configured.
/// Device check / waiting room / rejoin / join-failure stay in NIGA orchestration APIs (no SignalR).
/// </summary>
public interface ITeleVideoVendor
{
    string VendorName { get; }
    Task<TeleVideoIssueResult> IssueTokenAsync(TeleVideoIssueRequest request, CancellationToken cancellationToken = default);
}
