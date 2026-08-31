namespace Niga_Domain.Master;

public partial class AudioCaseCausationLink
{
    public Guid CausationLinkId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public Guid? CauseConceptId { get; set; }

    public Guid? EffectConceptId { get; set; }

    public string CauseText { get; set; } = null!;

    public string EffectText { get; set; } = null!;

    public string LinkType { get; set; } = "CauseEffect";

    public decimal Confidence { get; set; }

    public int SequenceOrder { get; set; }

    public DateTime EnteredDate { get; set; }
}
