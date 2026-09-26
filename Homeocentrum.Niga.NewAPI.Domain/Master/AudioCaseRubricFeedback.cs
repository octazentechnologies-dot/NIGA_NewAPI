namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AudioCaseRubricFeedback
{
    public long FeedbackId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public int? SubSectionId { get; set; }

    public string RubricName { get; set; } = null!;

    public string FeedbackType { get; set; } = null!;

    public string? OriginalMatchLayer { get; set; }

    public int? CorrectedSubSectionId { get; set; }

    public string? Reason { get; set; }

    public string? RejectReasonStage { get; set; }

    public string? RejectReasonNote { get; set; }

    public decimal? ConfidenceAtFeedback { get; set; }

    public string EngineVersion { get; set; } = null!;

    public long DoctorUserId { get; set; }

    public DateTime EnteredDate { get; set; }
}
