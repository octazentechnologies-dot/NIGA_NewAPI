namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AiRolloutGate
{
    public long RolloutGateId { get; set; }

    public string FlagName { get; set; } = null!;

    public DateTime BenchmarkRunUtc { get; set; }

    public decimal? Top5Accuracy { get; set; }

    public decimal? DoctorAcceptanceRate { get; set; }

    public int ApprovedByUserId { get; set; }

    public string? Notes { get; set; }

    public DateTime EnteredDate { get; set; }
}
