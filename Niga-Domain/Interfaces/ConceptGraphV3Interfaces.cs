using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IConceptGraphRepository
{
    Task ClearSessionV3DataAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SavePatientMeaningsAsync(
        Guid sessionId,
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default);

    Task<List<PatientMeaningNodeModel>> GetPatientMeaningsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveFullGraphAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        CancellationToken cancellationToken = default);

    Task<ConceptGraphFullModel> GetFullGraphAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task SaveReasoningAuditAsync(
        Guid sessionId,
        AiReasoningAuditModel audit,
        CancellationToken cancellationToken = default);

    Task<List<AiConceptMappingBootstrapModel>> GetActiveConceptMappingsAsync(
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, decimal>> GetLearnedConceptWeightsAsync(
        CancellationToken cancellationToken = default);

    Task SaveCoverageMetricsAsync(
        Guid sessionId,
        CaseCoverageMetricsModel metrics,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists rubrics shown in SuggestedRubricsJson to AIRubricDiscovery synchronously so diagnostics
    /// and the doctor UI read the same authoritative rows (not only deferred background graph writes).
    /// </summary>
    Task SaveDisplayedRubricsAsync(
        Guid sessionId,
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        ConceptGraphFullModel? graph = null,
        CancellationToken cancellationToken = default);
}

public class AiConceptMappingBootstrapModel
{
    public string HomeopathicConceptPattern { get; set; } = string.Empty;

    public string SubSectionNamePattern { get; set; } = string.Empty;

    public string? Domain { get; set; }

    public int PriorityOrder { get; set; }
}

public interface IPatientMeaningGraphEngine
{
    Task<PatientMeaningGraphResult> BuildAsync(
        string transcript,
        string? detectedLanguage,
        CancellationToken cancellationToken = default);

    Task<PatientMeaningGraphResult> BuildAsync(
        string transcript,
        string? detectedLanguage,
        DualLanguageMeaningContext? dualLanguage,
        CancellationToken cancellationToken = default);
}

public interface IMetaphorUnderstandingEngine
{
    Task<List<MetaphorResolutionNodeModel>> ResolveAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default);
}

public interface IClinicalConceptEngineV3
{
    Task<List<ClinicalConceptNodeModel>> DeriveAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors,
        CancellationToken cancellationToken = default);
}

public interface IHomeopathicConceptEngineV3
{
    Task<List<HomeopathicConceptNodeModel>> MapAsync(
        IReadOnlyList<ClinicalConceptNodeModel> clinicalConcepts,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default);
}

public interface IRubricDiscoveryEngineV3
{
    Task<List<RubricDiscoveryNodeModel>> DiscoverAsync(
        IReadOnlyList<HomeopathicConceptNodeModel> homeopathicConcepts,
        CancellationToken cancellationToken = default);
}

public interface IConceptGraphEvidenceEngine
{
    List<RubricDiscoveryNodeModel> BuildEvidenceChains(
        IReadOnlyList<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph);
}

public interface IConceptGraphOrchestrator
{
    Task<ConceptGraphPhaseResult> BuildMeaningGraphAsync(
        Guid sessionId,
        string transcript,
        string? detectedLanguage,
        CancellationToken cancellationToken = default);

    Task<ConceptGraphAnalysisResult> AnalyzeAsync(
        Guid sessionId,
        string transcript,
        string? detectedLanguage,
        PatientClinicalContext? patientContext,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default);

    Task<ConceptGraphAnalysisResult> AnalyzeAsync(
        Guid sessionId,
        string transcript,
        string? detectedLanguage,
        PatientClinicalContext? patientContext,
        AudioCaseSummaryModel? summary,
        DualLanguageMeaningContext? dualLanguage,
        CancellationToken cancellationToken = default);
}

public interface ICaseDecompositionEngine
{
    Task<List<SymptomBlockNodeModel>> DecomposeAsync(string transcript, CancellationToken cancellationToken = default);
}

public interface IMultiSymptomDiscoveryEngine
{
    Task<List<PatientMeaningNodeModel>> ExpandAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        CancellationToken cancellationToken = default);
}

public interface ICategoryDiscoveryEngine
{
    Task ApplyCategoriesAsync(IList<PatientMeaningNodeModel> meanings, CancellationToken cancellationToken = default);
}

public interface IRecallExpansionEngine
{
    Task<List<HomeopathicConceptNodeModel>> ExpandAsync(
        IReadOnlyList<ClinicalConceptNodeModel> clinicalConcepts,
        IReadOnlyList<HomeopathicConceptNodeModel> baseConcepts,
        string transcript,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default);

    Task<List<HomeopathicConceptNodeModel>> ExpandBlockAsync(
        SymptomBlockNodeModel block,
        string transcript,
        CancellationToken cancellationToken = default);
}

public interface IConceptClusterEngine
{
    List<ConceptClusterNodeModel> BuildClusters(IReadOnlyList<HomeopathicConceptNodeModel> concepts);
}

public interface ITranscriptCoverageEngine
{
    CaseCoverageMetricsModel Measure(
        IReadOnlyList<SymptomBlockNodeModel> blocks,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        IReadOnlyList<PatientMeaningNodeModel> meanings);
}

public interface IMissingSymptomDetector
{
    Task<MissingSymptomPassResult> RunSecondPassAsync(
        ConceptGraphFullModel graph,
        string transcript,
        CaseCoverageMetricsModel coverage,
        CancellationToken cancellationToken = default);
}

public interface ICaseCompletenessScoringEngine
{
    decimal Score(
        IReadOnlyList<SymptomBlockNodeModel> blocks,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        CaseCoverageMetricsModel coverage);
}

public interface IMultiConceptDiscoveryEngine
{
    Task<MultiConceptDiscoveryResult> DiscoverAsync(
        IReadOnlyList<PatientMeaningNodeModel> meanings,
        IReadOnlyList<MetaphorResolutionNodeModel> metaphors,
        IReadOnlyList<SymptomBlockNodeModel> symptomBlocks,
        string transcript,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default);
}

public interface IRubricCandidateEngine
{
    Task<RubricCandidateEngineResult> DiscoverFromGraphAsync(
        ConceptGraphFullModel graph,
        RubricCandidateEngineRequest? request = null,
        CancellationToken cancellationToken = default);
}
