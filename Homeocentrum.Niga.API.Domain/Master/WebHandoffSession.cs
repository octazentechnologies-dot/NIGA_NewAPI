namespace Homeocentrum.Niga.API.Domain.Master;

/// <summary>One-time code and the short web session it opens. The raw code is never stored.</summary>
public class WebHandoffSession
{
    public long WebHandoffSessionId { get; set; }

    public Guid Sid { get; set; }

    public string CodeHash { get; set; } = "";

    public long DoctorUserId { get; set; }

    public int DoctorId { get; set; }

    public string Purpose { get; set; } = "";

    public int? PatientId { get; set; }

    public int? CaseId { get; set; }

    public int? PatientAppId { get; set; }

    public string? AppointmentDate { get; set; }

    public string? VisitType { get; set; }

    public string? ConsultMode { get; set; }

    public bool IsTele { get; set; }

    public string ReturnUrl { get; set; } = "";

    public DateTime CodeExpiresAtUtc { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public DateTime? AbsoluteExpiresAtUtc { get; set; }

    public DateTime? LastActivityAtUtc { get; set; }

    public string? CurrentJti { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokeReason { get; set; }

    public string? CreatedFromIp { get; set; }

    public DateTime EnteredDate { get; set; }
}

/// <summary>Every web token issued for a handoff, so a replay of an old jti can be told apart from a double renew.</summary>
public class WebHandoffJti
{
    public string Jti { get; set; } = "";

    public Guid Sid { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? ReplacedAtUtc { get; set; }
}
