using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Orchestration;

public sealed class FastClinicalRetrievalContext
{
    public PatientClinicalContext? Patient { get; set; }

    public IReadOnlyList<AudioCaseMessageModel>? Messages { get; set; }

    public string? Transcript { get; set; }
}

public sealed class FastClinicalRetrievalResult
{
    public bool Success { get; set; }

    public string EngineVersion { get; set; } = "fast-e";

    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public List<AudioCaseSuggestedRubricModel> Rubrics { get; set; } = new();

    public List<ConceptDiscoveryTraceModel> KeywordTraces { get; set; } = new();

    public int V1RubricCount { get; set; }

    public int KeywordRubricCount { get; set; }

    public int AliasRubricCount { get; set; }

    public int EmbeddingRubricCount { get; set; }

    public int CatalogRubricCount { get; set; }

    public int LatencyMs { get; set; }

    public string? Error { get; set; }

    public List<string> StagesCompleted { get; set; } = new();
}

public interface IFastClinicalRetrievalOrchestrator
{
    Task<FastClinicalRetrievalResult> DiscoverAsync(
        Guid sessionId,
        string? correlationId,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        string? detectedLanguage,
        Func<CancellationToken, Task<List<AudioCaseSuggestedRubricModel>>> v1Discovery,
        CancellationToken cancellationToken = default,
        FastClinicalRetrievalContext? context = null);
}
