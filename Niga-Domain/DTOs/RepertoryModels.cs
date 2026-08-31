namespace Niga_Domain.DTOs;

public class RepertorySourceModel
{
    public long RepertorySourceId { get; set; }

    public string SourceCode { get; set; } = string.Empty;

    public string SourceName { get; set; } = string.Empty;

    public int PriorityOrder { get; set; }

    public bool IsActive { get; set; }
}

public class RepertoryMapModel
{
    public int SubSectionId { get; set; }

    public string SourceCode { get; set; } = string.Empty;

    public string SourceName { get; set; } = string.Empty;

    public string? SourceRubricKey { get; set; }

    public string? SourceRubricPath { get; set; }

    public decimal MappingConfidence { get; set; }

    public bool IsPrimarySource { get; set; }

    public int PriorityOrder { get; set; }
}

public class RepertoryMappingStatusModel
{
    public int ActiveSourceCount { get; set; }

    public int MappedRubricCount { get; set; }

    public int KentMappedCount { get; set; }

    public int CompleteMappedCount { get; set; }

    public List<RepertorySourceModel> Sources { get; set; } = new();

    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RepertoryTierEnrichmentResult
{
    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public int MappedRubricCount { get; set; }
}
