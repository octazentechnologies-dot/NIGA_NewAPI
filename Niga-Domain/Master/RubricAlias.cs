namespace Niga_Domain.Master;

public partial class RubricAlias
{
    public long RubricAliasId { get; set; }

    public int SubSectionId { get; set; }

    public string AliasText { get; set; } = null!;

    public string NormalizedAlias { get; set; } = null!;

    public string Language { get; set; } = null!;

    public string AliasType { get; set; } = null!;

    public decimal Weight { get; set; }

    public string Source { get; set; } = null!;

    public int UsageCount { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public bool IsActive { get; set; } = true;

    public int VersionNo { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }
}
