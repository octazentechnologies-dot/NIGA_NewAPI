namespace Niga_Domain.Master;

public partial class GoldCaseLibrary
{
    public long GoldCaseId { get; set; }

    public string Category { get; set; } = null!;

    public string Transcript { get; set; } = null!;

    public string? SourceLanguage { get; set; }

    public string DoctorRubricsJson { get; set; } = null!;

    public string PrimaryRubricIdsJson { get; set; } = null!;

    public string? FinalRemedy { get; set; }

    public string? FollowUpOutcome { get; set; }

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EnteredDate { get; set; }
}
