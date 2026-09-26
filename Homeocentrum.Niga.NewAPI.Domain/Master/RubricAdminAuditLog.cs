namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RubricAdminAuditLog
{
    public long AuditLogId { get; set; }

    public string EntityType { get; set; } = null!;

    public long EntityId { get; set; }

    public string ActionType { get; set; } = null!;

    public string? BeforeJson { get; set; }

    public string? AfterJson { get; set; }

    public int AdminUserId { get; set; }

    public string? IpAddress { get; set; }

    public DateTime EnteredDate { get; set; }
}
