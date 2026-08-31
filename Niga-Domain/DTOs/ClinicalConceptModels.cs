namespace Niga_Domain.DTOs;

public class ClinicalConceptModel
{
    public Guid ConceptId { get; set; }

    public string RawStatement { get; set; } = string.Empty;

    public string? ClinicalMeaning { get; set; }

    public string? HomeopathicMeaning { get; set; }

    public string? Category { get; set; }

    public bool IsSRP { get; set; }

    /// <summary>True when transcript support for this concept contains a clear contradiction.</summary>
    public bool IsAmbiguous { get; set; }

    public List<string> Modalities { get; set; } = new();

    public List<string> Concomitants { get; set; } = new();

    public List<string> SearchTerms { get; set; } = new();

    public decimal Confidence { get; set; }

    public string? SourceLanguage { get; set; }

    public decimal HomeopathicWeight { get; set; } = 1m;

    public int SequenceOrder { get; set; }

    public string? ConceptTier { get; set; }
}

public class CausationLinkModel
{
    public Guid LinkId { get; set; }

    public Guid? CauseConceptId { get; set; }

    public Guid? EffectConceptId { get; set; }

    public string CauseText { get; set; } = string.Empty;

    public string EffectText { get; set; } = string.Empty;

    public string LinkType { get; set; } = "CauseEffect";

    public decimal Confidence { get; set; }

    public int SequenceOrder { get; set; }
}

public class CausationDetectionResult
{
    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public List<CausationLinkModel> Links { get; set; } = new();
}

public class HomeopathicWeightResult
{
    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public Dictionary<string, decimal> RuleWeights { get; set; } = new();
}

public class AudioCaseConceptsModel
{
    public Guid SessionId { get; set; }

    public string EngineVersion { get; set; } = "v2";

    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public List<CausationLinkModel> CausationLinks { get; set; } = new();

    public List<string> StagesCompleted { get; set; } = new();

    public List<TieredConceptNodeModel> PrimaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SecondaryConcepts { get; set; } = new();

    public List<TieredConceptNodeModel> SupportingConcepts { get; set; } = new();

    public List<ConceptGraphEdgeModel> ConceptGraphEdges { get; set; } = new();
}

public class CaseUnderstandingGptModel
{
    public List<CaseUnderstandingConceptGptModel> Concepts { get; set; } = new();

    public List<CaseUnderstandingSymptomGptModel> EnhancedSymptoms { get; set; } = new();
}

public class CaseUnderstandingConceptGptModel
{
    public string RawStatement { get; set; } = string.Empty;

    public string? ClinicalMeaning { get; set; }

    public string? HomeopathicMeaning { get; set; }

    public string? Category { get; set; }

    public bool IsSRP { get; set; }

    public List<string> Modalities { get; set; } = new();

    public List<string> Concomitants { get; set; } = new();

    public List<string> SearchTerms { get; set; } = new();

    public decimal Confidence { get; set; }
}

public class CaseUnderstandingSymptomGptModel
{
    public string Phrase { get; set; } = string.Empty;

    public List<string> SearchTerms { get; set; } = new();

    public string? Category { get; set; }

    public int IntensityHint { get; set; } = 2;
}
