namespace Niga_Domain.Master;

public partial class AudioCaseClinicalInferenceLog
{
    public long InferenceLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public Guid? SourceConceptId { get; set; }

    public string InferredRubricName { get; set; } = null!;

    public int? SubSectionId { get; set; }

    public string Reason { get; set; } = null!;

    public string? SourceSymptom { get; set; }

    public decimal Confidence { get; set; }

    public bool? DoctorAccepted { get; set; }

    public DateTime EnteredDate { get; set; }
}
