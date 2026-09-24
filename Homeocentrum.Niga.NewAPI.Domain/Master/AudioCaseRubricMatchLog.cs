using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AudioCaseRubricMatchLog
{
    public long RubricMatchLogId { get; set; }

    public Guid AudioCaseSessionId { get; set; }

    public string SymptomPhrase { get; set; } = null!;

    public int SubSectionId { get; set; }

    public string? SubSectionName { get; set; }

    public decimal? KeywordScore { get; set; }

    public decimal? FullTextScore { get; set; }

    public decimal? SemanticScore { get; set; }

    public decimal FinalScore { get; set; }

    public int? RankPosition { get; set; }

    public bool IsSelectedForUi { get; set; }

    public int? SuggestedIntensityNo { get; set; }

    public string? MatchSource { get; set; }

    public string? ClinicalMeaning { get; set; }

    public string? HomeopathicMeaning { get; set; }

    public string? WhySuggested { get; set; }

    public decimal? ConfidenceScore { get; set; }

    public decimal? HomeopathicWeight { get; set; }

    public string? RubricTier { get; set; }

    public string? MatchLayer { get; set; }

    public string? CausationJson { get; set; }

    public string? ExplainabilityJson { get; set; }

    /// <summary>Task 6: Database | AiSuggested</summary>
    public string? UnifiedSource { get; set; }

    public decimal? FinalHybridScore { get; set; }

    public bool? EvidenceChainComplete { get; set; }

    public bool? GroundedInOntology { get; set; }

    public DateTime EnteredDate { get; set; }
}
