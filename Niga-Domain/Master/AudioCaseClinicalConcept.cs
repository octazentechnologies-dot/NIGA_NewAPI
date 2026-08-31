namespace Niga_Domain.Master;

public partial class AudioCaseClinicalConcept
{
    public Guid ConceptId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string RawStatement { get; set; } = null!;

    public string? ClinicalMeaning { get; set; }

    public string? HomeopathicMeaning { get; set; }

    public string? Category { get; set; }

    public bool IsSRP { get; set; }

    public string? ModalitiesJson { get; set; }

    public string? ConcomitantsJson { get; set; }

    public string? SequenceJson { get; set; }

    public decimal Confidence { get; set; }

    public string? SourceLanguage { get; set; }

    public DateTime EnteredDate { get; set; }
}
