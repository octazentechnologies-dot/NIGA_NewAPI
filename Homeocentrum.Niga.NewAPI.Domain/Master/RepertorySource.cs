namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RepertorySource
{
    public long RepertorySourceId { get; set; }

    public string SourceCode { get; set; } = null!;

    public string SourceName { get; set; } = null!;

    public int? AuthorId { get; set; }

    public int PriorityOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EnteredDate { get; set; }
}
