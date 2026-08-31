namespace Niga_Domain.Master;

public partial class RubricRepertoryMap
{
    public long RubricRepertoryMapId { get; set; }

    public int SubSectionId { get; set; }

    public long RepertorySourceId { get; set; }

    public string? SourceRubricKey { get; set; }

    public string? SourceRubricPath { get; set; }

    public decimal MappingConfidence { get; set; }

    public bool IsPrimarySource { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EnteredDate { get; set; }

    public virtual RepertorySource? RepertorySource { get; set; }
}
