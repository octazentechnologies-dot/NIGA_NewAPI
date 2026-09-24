namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class HomeopathicWeightRule
{
    public int WeightRuleId { get; set; }

    public string RuleCode { get; set; } = null!;

    public string Category { get; set; } = null!;

    public decimal WeightValue { get; set; }

    public decimal? MultiplierValue { get; set; }

    public string? Description { get; set; }

    public int? SetByUserId { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime EnteredDate { get; set; }
}
