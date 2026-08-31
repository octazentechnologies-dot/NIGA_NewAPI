namespace Niga_Domain.DTOs;

public static class KgNodeTypes
{
    public const string PatientExpression = "PatientExpression";

    public const string ClinicalMeaning = "ClinicalMeaning";

    public const string MedicalConcept = "MedicalConcept";

    public const string HomeopathicConcept = "HomeopathicConcept";

    public const string RepertoryRubric = "RepertoryRubric";

    public const string Remedy = "Remedy";
}

public static class KgEdgeTypes
{
    public const string Expresses = "Expresses";

    public const string ResolvesTo = "ResolvesTo";

    public const string SameMeaning = "SameMeaning";

    public const string ImpliesClinical = "ImpliesClinical";

    public const string MapsHomeopathic = "MapsHomeopathic";

    public const string SuggestsRubric = "SuggestsRubric";

    public const string HasRemedy = "HasRemedy";

    public const string Contradicts = "Contradicts";
}

public static class KgExpressionKinds
{
    public const string Literal = "Literal";

    public const string Colloquial = "Colloquial";

    public const string Idiom = "Idiom";

    public const string Metaphor = "Metaphor";

    public const string MixedLanguage = "MixedLanguage";
}

public class KgNodeModel
{
    public long NodeId { get; set; }

    public string NodeType { get; set; } = string.Empty;

    public string CanonicalKey { get; set; } = string.Empty;

    public string DisplayText { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string? ExpressionKind { get; set; }

    public int? SubSectionId { get; set; }

    public int? RemedyId { get; set; }

    public decimal Confidence { get; set; }

    public string Status { get; set; } = "Active";
}

public class KgEdgeModel
{
    public long EdgeId { get; set; }

    public long FromNodeId { get; set; }

    public long ToNodeId { get; set; }

    public string EdgeType { get; set; } = string.Empty;

    public decimal Weight { get; set; }

    public decimal Confidence { get; set; }

    public bool IsProvisional { get; set; }

    public string Source { get; set; } = string.Empty;
}

public class KgPathStepModel
{
    public string Layer { get; set; } = string.Empty;

    public long NodeId { get; set; }

    public string DisplayText { get; set; } = string.Empty;

    public string EdgeType { get; set; } = string.Empty;

    public decimal Confidence { get; set; }
}

public class KgRubricPathModel
{
    public int SubSectionId { get; set; }

    public string SubSectionName { get; set; } = string.Empty;

    public decimal PathConfidence { get; set; }

    public decimal CompositeScore { get; set; }

    public bool HasKnowledgeGraphPath { get; set; }

    public bool IsProvisionalPath { get; set; }

    public decimal? EmbeddingScore { get; set; }

    public List<KgPathStepModel> Steps { get; set; } = new();

    public string DiscoveryMethod { get; set; } = "KnowledgeGraph";
}

public class KgExpressionResolveRequest
{
    public string Text { get; set; } = string.Empty;

    public string? LanguageCode { get; set; }

    public string? ExpressionKind { get; set; }

    public string? ClinicalMeaning { get; set; }

    public string? LiteralMeaning { get; set; }

    public Guid? SessionId { get; set; }
}

public class KgExpressionResolveResult
{
    public KgNodeModel? ExpressionNode { get; set; }

    public KgNodeModel? ResolvedClinicalNode { get; set; }

    public List<KgRubricPathModel> RubricPaths { get; set; } = new();

    public bool ResolvedFromFigurativeStore { get; set; }

    public bool ResolvedFromCrossLanguage { get; set; }
}

public class KgDiscoveryResult
{
    public bool Success { get; set; }

    public string? Error { get; set; }

    public List<RubricDiscoveryNodeModel> Discoveries { get; set; } = new();

    public List<KgRubricPathModel> Paths { get; set; } = new();

    public int ExpressionsResolved { get; set; }

    public int GraphPathsFound { get; set; }
}

public class KgRemedyProjectionModel
{
    public int SubSectionId { get; set; }

    public int RemedyId { get; set; }

    public string? RemedyName { get; set; }

    public int? GradeValue { get; set; }

    public DateTime LastSyncedUtc { get; set; }
}

public class KgGraphStatsModel
{
    public string EngineVersion { get; set; } = "v11";

    public int NodeCount { get; set; }

    public int EdgeCount { get; set; }

    public int ExpressionCount { get; set; }

    public int RubricLinkCount { get; set; }

    public int RemedyProjectionCount { get; set; }

    public int FigurativeResolutionCount { get; set; }

    public int FeedbackMutationCount { get; set; }

    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
}

public class KgBootstrapImportResult
{
    public int NodesCreated { get; set; }

    public int EdgesCreated { get; set; }

    public int LearningRecordsImported { get; set; }

    public bool Success { get; set; }

    public string? Error { get; set; }
}

public class KgRemedySyncResult
{
    public int ProjectionsSynced { get; set; }

    public int RubricsCovered { get; set; }

    public DateTime SyncedAtUtc { get; set; } = DateTime.UtcNow;
}
