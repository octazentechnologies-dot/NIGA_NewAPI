namespace Niga_Domain.Master;

public partial class AudioCaseRubricBenchmark
{
    public long BenchmarkId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string EngineVersion { get; set; } = null!;

    public int AiSuggestedCount { get; set; }

    public int DoctorAcceptedCount { get; set; }

    public int DoctorRejectedCount { get; set; }

    public int DoctorCorrectedCount { get; set; }

    public bool? PrimaryInTop5 { get; set; }

    public decimal? PrecisionScore { get; set; }

    public decimal? RecallScore { get; set; }

    public decimal? F1Score { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public decimal? FalsePositiveRate { get; set; }

    public decimal? ConfidenceCalibration { get; set; }

    public DateTime CalculatedDate { get; set; }
}
