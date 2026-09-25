using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IIntelligenceGptClient
{
    Task<IntelligenceGptResult<T>> CompleteJsonAsync<T>(
        string systemPrompt,
        string userPrompt,
        string stageName,
        CancellationToken cancellationToken = default);
}

public class IntelligenceGptResult<T>
{
    public bool Success { get; set; }

    public T? Result { get; set; }

    public string? RequestJson { get; set; }

    public string? ResponseJson { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int LatencyMs { get; set; }

    public string? Error { get; set; }
}

public interface ICaseUnderstandingEngine
{
    Task<CaseUnderstandingEngineResult> AnalyzeAsync(
        string transcript,
        IReadOnlyList<AudioCaseSymptomModel> existingSymptoms,
        AudioCaseSummaryModel? summary,
        string? detectedLanguage,
        CancellationToken cancellationToken = default);
}

public class CaseUnderstandingEngineResult
{
    public bool Success { get; set; }

    public List<ClinicalConceptModel> Concepts { get; set; } = new();

    public List<AudioCaseSymptomModel> EnhancedSymptoms { get; set; } = new();

    public string? RequestJson { get; set; }

    public string? ResponseJson { get; set; }

    public int PromptTokens { get; set; }

    public int CompletionTokens { get; set; }

    public int LatencyMs { get; set; }

    public string? Error { get; set; }
}

public interface ISymptomExtractionEngine
{
    List<AudioCaseSymptomModel> Merge(
        IReadOnlyList<AudioCaseSymptomModel> v1Symptoms,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> gptEnhancedSymptoms);
}

public interface IModalityDetectionEngine
{
    List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts);
}

public interface IConcomitantDetectionEngine
{
    List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts);
}

public interface IClinicalReasoningEngine
{
    List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts);
}

public interface IHomeopathicReasoningEngine
{
    List<ClinicalConceptModel> Enrich(IReadOnlyList<ClinicalConceptModel> concepts);
}

public interface ICausationDetectionEngine
{
    CausationDetectionResult Detect(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string transcript);
}

public interface IHomeopathicWeightEngine
{
    Task<HomeopathicWeightResult> ApplyAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<CausationLinkModel> causationLinks,
        CancellationToken cancellationToken = default);

    Task<decimal> ApplyWeightToRubricAsync(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken = default);
}

public interface IAudioCaseIntelligenceRepository
{
    Task SaveConceptsAsync(
        Guid sessionId,
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken = default);

    Task SaveIntelligenceLogAsync(
        Guid sessionId,
        string? correlationId,
        string stageName,
        string status,
        string? message,
        string? detailsJson,
        int? latencyMs,
        CancellationToken cancellationToken = default,
        string engineVersion = "v2");

    Task<List<ClinicalConceptModel>> GetConceptsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveCausationLinksAsync(
        Guid sessionId,
        IReadOnlyList<CausationLinkModel> links,
        CancellationToken cancellationToken = default);

    Task<List<CausationLinkModel>> GetCausationLinksAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<List<HomeopathicWeightRule>> GetActiveWeightRulesAsync(
        CancellationToken cancellationToken = default);

    Task SaveInferenceLogsAsync(
        Guid sessionId,
        IReadOnlyList<ClinicalInferenceLogModel> logs,
        CancellationToken cancellationToken = default);
}

public interface IMetaphorInterpretationEngine
{
    Task<MetaphorInterpretationResult> EnrichAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        string? language,
        CancellationToken cancellationToken = default);
}

public interface IRubricAliasEngine
{
    Task<RubricAliasSearchResult> SearchRubricsAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        string? language,
        CancellationToken cancellationToken = default);
}

public interface IEmbeddingClient
{
    bool IsConfigured { get; }

    Task<EmbeddingClientResult> EmbedTextsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    Task<EmbeddingClientResult> EmbedTextsAsync(
        IReadOnlyList<string> texts,
        string modelName,
        CancellationToken cancellationToken = default);
}

public class EmbeddingClientResult
{
    public bool Success { get; set; }

    public List<float[]> Vectors { get; set; } = new();

    public string? Error { get; set; }

    public int LatencyMs { get; set; }
}

public interface IRubricEmbeddingRepository
{
    Task<List<RubricEmbeddingCacheEntry>> LoadAllAsync(
        string modelName,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, string>> GetExistingHashesAsync(
        IEnumerable<int> rubricIds,
        string modelName,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(
        int rubricId,
        string subSectionName,
        float[] vector,
        string modelName,
        string textHash,
        string sourceType,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(string modelName, CancellationToken cancellationToken = default);

    Task<DateTime?> GetLastUpdatedUtcAsync(string modelName, CancellationToken cancellationToken = default);
}

public interface IRubricEmbeddingMemoryCache
{
    IReadOnlyList<RubricEmbeddingCacheEntry> Entries { get; }

    DateTime? LastRefreshedUtc { get; }

    Task RefreshAsync(CancellationToken cancellationToken = default);
}

public interface IEmbeddingSearchEngine
{
    Task<EmbeddingSearchResult> SearchAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken = default);
}

public interface IHybridRetrievalEngine
{
    Task<HybridRetrievalResult> RetrieveAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        IReadOnlyList<AudioCaseSuggestedRubricModel> aliasRubrics,
        CancellationToken cancellationToken = default);
}

public interface IConfidenceScoringEngine
{
    decimal Calibrate(decimal hybridScore, decimal? historicalAcceptance = null);
}

public interface IClinicalInferenceEngine
{
    Task<ClinicalInferenceResult> InferAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<CausationLinkModel> causationLinks,
        IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics,
        CancellationToken cancellationToken = default);
}

public interface IExplainabilityEngine
{
    List<AudioCaseSuggestedRubricModel> Enrich(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<CausationLinkModel> causationLinks);
}

public interface IPrimarySymptomEngine
{
    PrimarySymptomModel Resolve(
        AudioCaseSummaryModel? summary,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms);

    bool IsLinkedToPrimary(
        AudioCaseSuggestedRubricModel rubric,
        ClinicalConceptModel? linkedConcept,
        PrimarySymptomModel? primarySymptom);
}

public interface IEvidenceChainBuilder
{
    RubricEvidenceChainModel Build(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        string transcript);

    ClinicalConceptModel? FindLinkedConcept(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<ClinicalConceptModel> concepts);
}

public interface IRubricQualityScoringEngine
{
    RubricValidationResultModel Score(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyList<RubricValidationIssueModel> issues,
        bool isPrimaryLinked,
        RubricIntelligenceOptions options);
}

public interface IClinicalValidationEngine
{
    ClinicalValidationResult ValidateAndFilter(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        ClinicalValidationContext context);
}

public interface IEnterpriseClinicalValidationPipeline
{
    RubricEnterpriseValidationReport ValidateRubric(RubricValidationStepContext context);

    void ApplyBatchDuplicateDetection(
        IList<AudioCaseSuggestedRubricModel> accepted,
        IList<AudioCaseSuggestedRubricModel> rejected);
}

public interface IRubricValidationStep
{
    string StepName { get; }

    RubricValidationIssueModel? Validate(RubricValidationStepContext context);
}

public interface IRubricEnterpriseEvidenceChainBuilder
{
    RubricEnterpriseEvidenceChainModel Build(
        AudioCaseSuggestedRubricModel rubric,
        RubricEvidenceChainEnrichmentContext context);
}

public interface IRubricEnterpriseEvidenceChainEnricher
{
    Task EnrichRubricsAsync(
        IList<AudioCaseSuggestedRubricModel> rubrics,
        RubricEvidenceChainEnrichmentContext context,
        CancellationToken cancellationToken = default);

    RubricEvidenceChainSessionResult BuildSessionResult(
        Guid sessionId,
        IEnumerable<AudioCaseSuggestedRubricModel> rubrics);
}

public interface IRubricEvidenceChainFeedbackLoader
{
    Task<IReadOnlyDictionary<int, AudioCaseRubricFeedbackSummaryModel>> LoadLatestBySubSectionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}

public interface IRubricEmbeddingIndexerService
{
    Task<EmbeddingReindexResult> ReindexAsync(
        int? maxRubrics = null,
        CancellationToken cancellationToken = default);
}

public interface IRubricIntelligenceAdminService
{
    Task<RubricIntelligenceAdminListModel<HomeopathicWeightRuleModel>> GetWeightsAsync(
        int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<HomeopathicWeightRuleModel?> GetWeightByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, HomeopathicWeightRuleModel? Result)> UpdateWeightAsync(
        int adminUserId, int id, HomeopathicWeightRuleUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default);

    Task<RubricIntelligenceAdminListModel<RubricMetaphorModel>> GetMetaphorsAsync(
        string? search, string? language, string? approvalStatus, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<RubricMetaphorModel?> GetMetaphorByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, RubricMetaphorModel? Result)> CreateMetaphorAsync(
        int adminUserId, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, RubricMetaphorModel? Result)> UpdateMetaphorAsync(
        int adminUserId, long id, RubricMetaphorUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteMetaphorAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int DeletedCount)> DeleteAllMetaphorsAsync(
        int adminUserId, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> ApproveMetaphorAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> RejectMetaphorAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default);

    Task<RubricIntelligenceAdminListModel<RubricAliasModel>> GetAliasesAsync(
        string? search, string? language, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<RubricAliasModel?> GetAliasByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, RubricAliasModel? Result)> CreateAliasAsync(
        int adminUserId, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, RubricAliasModel? Result)> UpdateAliasAsync(
        int adminUserId, long id, RubricAliasUpsertModel model, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> DeleteAliasAsync(
        int adminUserId, long id, string? ipAddress, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message, int DeletedCount)> DeleteAllAliasesAsync(
        int adminUserId, string? ipAddress, CancellationToken cancellationToken = default);

    Task<List<RubricMetaphorDictionary>> SearchApprovedMetaphorsAsync(
        IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default);

    Task<List<(RubricAlias Alias, string SubSectionName)>> SearchActiveAliasesAsync(
        IEnumerable<string> normalizedTerms, string? language, CancellationToken cancellationToken = default);
}
