namespace Niga_Domain.Master;

public partial class RubricMetaphorDictionary
{
    public long MetaphorId { get; set; }

    public string PatientExpression { get; set; } = null!;

    public string NormalizedExpression { get; set; } = null!;

    public string ClinicalMeaning { get; set; } = null!;

    public string RubricMeaning { get; set; } = null!;

    public int? SubSectionId { get; set; }

    public string Language { get; set; } = null!;

    public decimal ConfidenceWeight { get; set; }

    public string ApprovalStatus { get; set; } = "Pending";

    public int UsageCount { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public int VersionNo { get; set; }

    public bool IsActive { get; set; } = true;

    public int? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public int? ApprovedBy { get; set; }

    public DateTime? ApprovedDate { get; set; }
}
