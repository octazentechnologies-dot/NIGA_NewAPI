namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AiKgNode
{
    public long NodeId { get; set; }

    public string NodeType { get; set; } = null!;

    public string CanonicalKey { get; set; } = null!;

    public string DisplayText { get; set; } = null!;

    public string? LanguageCode { get; set; }

    public string? ExpressionKind { get; set; }

    public int? SubSectionId { get; set; }

    public int? RemedyId { get; set; }

    public int? GradeId { get; set; }

    public string? Domain { get; set; }

    public string? SymptomCategory { get; set; }

    public decimal Confidence { get; set; }

    public string Status { get; set; } = "Active";

    public string? MetadataJson { get; set; }

    public DateTime EnteredDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public partial class AiKgEdge
{
    public long EdgeId { get; set; }

    public long FromNodeId { get; set; }

    public long ToNodeId { get; set; }

    public string EdgeType { get; set; } = null!;

    public decimal Weight { get; set; } = 1m;

    public decimal Confidence { get; set; }

    public bool IsProvisional { get; set; }

    public string Source { get; set; } = "Bootstrap";

    public Guid? SourceSessionId { get; set; }

    public DateTime EnteredDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}

public partial class AiKgEdgeEvidence
{
    public long EdgeEvidenceId { get; set; }

    public long EdgeId { get; set; }

    public Guid? AudioCaseSessionId { get; set; }

    public string? TranscriptSpan { get; set; }

    public long? FeedbackId { get; set; }

    public decimal? EmbeddingScore { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiKgFeedbackMutation
{
    public long FeedbackMutationId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long? FeedbackId { get; set; }

    public string FeedbackType { get; set; } = null!;

    public string MutationType { get; set; } = null!;

    public long? EdgeId { get; set; }

    public long? NodeId { get; set; }

    public int? SubSectionId { get; set; }

    public int? CorrectedSubSectionId { get; set; }

    public decimal? WeightDelta { get; set; }

    public string? DetailsJson { get; set; }

    public int DoctorUserId { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiKgSessionPath
{
    public long SessionPathId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public int SubSectionId { get; set; }

    public string PathJson { get; set; } = null!;

    public decimal PathConfidence { get; set; }

    public string DiscoveryMethod { get; set; } = "KnowledgeGraph";

    public DateTime EnteredDate { get; set; }
}

public partial class AiKgRemedyProjection
{
    public long RemedyProjectionId { get; set; }

    public int SubSectionId { get; set; }

    public int RemedyId { get; set; }

    public int? GradeId { get; set; }

    public string? RemedyName { get; set; }

    public int? GradeValue { get; set; }

    public DateTime LastSyncedUtc { get; set; }
}

public partial class AiKgFigurativeResolution
{
    public long FigurativeResolutionId { get; set; }

    public long ExpressionNodeId { get; set; }

    public long ClinicalMeaningNodeId { get; set; }

    public string ExpressionKind { get; set; } = null!;

    public string? LiteralMeaning { get; set; }

    public string? ResolutionExplanation { get; set; }

    public decimal Confidence { get; set; }

    public string Source { get; set; } = null!;

    public DateTime EnteredDate { get; set; }
}
