namespace Niga_Domain.DTOs;

public class RubricMetaphorModel
{
    public long MetaphorId { get; set; }

    public string PatientExpression { get; set; } = string.Empty;

    public string ClinicalMeaning { get; set; } = string.Empty;

    public string RubricMeaning { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    public string? SubSectionName { get; set; }

    public string Language { get; set; } = "en";

    public decimal ConfidenceWeight { get; set; }

    public string ApprovalStatus { get; set; } = "Pending";

    public int UsageCount { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public int VersionNo { get; set; }

    public bool IsActive { get; set; } = true;
}

public class RubricAliasModel
{
    public long RubricAliasId { get; set; }

    public int SubSectionId { get; set; }

    public string? SubSectionName { get; set; }

    public string AliasText { get; set; } = string.Empty;

    public string Language { get; set; } = "en";

    public string AliasType { get; set; } = "patient_phrase";

    public decimal Weight { get; set; } = 1m;

    public string Source { get; set; } = "manual";

    public int UsageCount { get; set; }

    public decimal? AcceptanceRate { get; set; }

    public bool IsActive { get; set; } = true;

    public int VersionNo { get; set; }
}

public class RubricMetaphorUpsertModel
{
    public string PatientExpression { get; set; } = string.Empty;

    public string ClinicalMeaning { get; set; } = string.Empty;

    public string RubricMeaning { get; set; } = string.Empty;

    public int? SubSectionId { get; set; }

    public string Language { get; set; } = "en";

    public decimal ConfidenceWeight { get; set; } = 0.85m;
}

public class RubricAliasUpsertModel
{
    public int SubSectionId { get; set; }

    public string AliasText { get; set; } = string.Empty;

    public string Language { get; set; } = "en";

    public string AliasType { get; set; } = "patient_phrase";

    public decimal Weight { get; set; } = 1m;
}

public class HomeopathicWeightRuleModel
{
    public int WeightRuleId { get; set; }

    public string RuleCode { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal WeightValue { get; set; }

    public decimal? MultiplierValue { get; set; }

    public string? Description { get; set; }

    public string? Notes { get; set; }

    public int? SetByUserId { get; set; }

    public bool IsActive { get; set; }
}

public class HomeopathicWeightRuleUpsertModel
{
    public decimal WeightValue { get; set; }

    public decimal? MultiplierValue { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }
}

public class RubricIntelligenceAdminListModel<T>
{
    public List<T> Items { get; set; } = new();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }
}

public class MetaphorInterpretationResult
{
    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public int MetaphorsMatched { get; set; }
}

public class RubricAliasSearchResult
{
    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public int AliasesMatched { get; set; }
}
