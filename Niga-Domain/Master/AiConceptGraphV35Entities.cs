namespace Niga_Domain.Master;

public partial class AiSymptomBlock
{
    public long SymptomBlockId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public int BlockOrder { get; set; }

    public string TranscriptSpan { get; set; } = null!;

    public string CategoryHint { get; set; } = null!;

    public string? BlockType { get; set; }

    public decimal Confidence { get; set; }

    public bool IsCovered { get; set; }

    public string ModelVersion { get; set; } = "v3.5-m0";

    public DateTime EnteredDate { get; set; }
}

public partial class AiConceptCluster
{
    public long ConceptClusterId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string ClusterLabel { get; set; } = null!;

    public long? SymptomBlockId { get; set; }

    public int MemberCount { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiConceptClusterMember
{
    public long ConceptClusterMemberId { get; set; }

    public long ConceptClusterId { get; set; }

    public long HomeopathicConceptId { get; set; }

    public int RankOrder { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiCaseCoverageMetrics
{
    public long CaseCoverageMetricsId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public decimal TranscriptCoverage { get; set; }

    public decimal CaseCompleteness { get; set; }

    public int TotalBlocks { get; set; }

    public int CoveredBlocks { get; set; }

    public int Tier1Count { get; set; }

    public int Tier2Count { get; set; }

    public int Tier3Count { get; set; }

    public int MissingSymptomCount { get; set; }

    public string? MetricsJson { get; set; }

    public DateTime EnteredDate { get; set; }
}

public partial class AiMissingSymptomCandidate
{
    public long MissingSymptomCandidateId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public long? SymptomBlockId { get; set; }

    public string TranscriptSpan { get; set; } = null!;

    public string CategoryHint { get; set; } = null!;

    public string ResolutionStatus { get; set; } = "Pending";

    public int ResolvedRubricCount { get; set; }

    public DateTime EnteredDate { get; set; }
}
