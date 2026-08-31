using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IEnterpriseKnowledgeGraphRepository
{
    Task<KgNodeModel> FindOrCreateNodeAsync(
        string nodeType,
        string canonicalKey,
        string displayText,
        string? languageCode = null,
        string? expressionKind = null,
        int? subSectionId = null,
        int? remedyId = null,
        decimal confidence = 0.7m,
        CancellationToken cancellationToken = default);

    Task<KgNodeModel?> FindNodeAsync(
        string nodeType,
        string canonicalKey,
        string? languageCode = null,
        CancellationToken cancellationToken = default);

    Task<List<KgNodeModel>> SearchExpressionNodesAsync(
        string text,
        string? languageCode,
        CancellationToken cancellationToken = default);

    Task<KgEdgeModel> UpsertEdgeAsync(
        long fromNodeId,
        long toNodeId,
        string edgeType,
        decimal weight,
        decimal confidence,
        bool isProvisional,
        string source,
        Guid? sourceSessionId = null,
        CancellationToken cancellationToken = default);

    Task AdjustEdgeWeightAsync(
        long edgeId,
        decimal weightDelta,
        bool confirmProvisional = false,
        CancellationToken cancellationToken = default);

    Task SaveSessionPathAsync(
        Guid sessionId,
        KgRubricPathModel path,
        CancellationToken cancellationToken = default);

    Task<List<KgRubricPathModel>> GetSessionPathsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task RecordFeedbackMutationAsync(
        Guid sessionId,
        long? feedbackId,
        string feedbackType,
        string mutationType,
        long? edgeId,
        long? nodeId,
        int? subSectionId,
        int? correctedSubSectionId,
        decimal? weightDelta,
        int doctorUserId,
        string? detailsJson = null,
        CancellationToken cancellationToken = default);

    Task SaveFigurativeResolutionAsync(
        long expressionNodeId,
        long clinicalMeaningNodeId,
        string expressionKind,
        string? literalMeaning,
        string? explanation,
        decimal confidence,
        string source,
        CancellationToken cancellationToken = default);

    Task<KgNodeModel?> GetFigurativeClinicalNodeAsync(
        long expressionNodeId,
        CancellationToken cancellationToken = default);

    Task<KgGraphStatsModel> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<List<(KgEdgeModel Edge, KgNodeModel Node)>> GetOutgoingEdgesAsync(
        long nodeId,
        CancellationToken cancellationToken = default);

    Task<KgNodeModel?> GetNodeByIdAsync(long nodeId, CancellationToken cancellationToken = default);
}

public interface IKgExpressionResolverEngine
{
    Task<KgExpressionResolveResult> ResolveAsync(
        KgExpressionResolveRequest request,
        CancellationToken cancellationToken = default);
}

public interface IKgGraphTraversalEngine
{
    Task<List<KgRubricPathModel>> FindRubricPathsAsync(
        long startNodeId,
        decimal minPathConfidence,
        CancellationToken cancellationToken = default);
}

public interface IKgEmbeddingBridge
{
    Task<List<KgRubricPathModel>> ProposeFromEmbeddingsAsync(
        string queryText,
        IReadOnlyList<RubricCandidateModel> embeddingCandidates,
        CancellationToken cancellationToken = default);
}

public interface IKnowledgeGraphRanker
{
    List<RubricDiscoveryNodeModel> RankAndMerge(
        IReadOnlyList<KgRubricPathModel> graphPaths,
        IReadOnlyList<RubricDiscoveryNodeModel> embeddingDiscoveries);
}

public interface IKnowledgeGraphOrchestratorBridge
{
    Task<KgDiscoveryResult> DiscoverAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        string transcript,
        RubricCandidateEngineResult? embeddingResult,
        CancellationToken cancellationToken = default);

    Task ProjectSessionAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        CancellationToken cancellationToken = default);
}

public interface IKgFeedbackGraphWriter
{
    Task ApplyFeedbackAsync(
        Guid sessionId,
        AudioCaseRubricFeedbackRequestModel request,
        long feedbackId,
        int doctorUserId,
        DoctorLearningFeedbackContext context,
        CancellationToken cancellationToken = default);
}

public interface IKgBootstrapImporter
{
    Task<KgBootstrapImportResult> ImportAsync(CancellationToken cancellationToken = default);
}

public interface IKgRemedyProjectionSync
{
    Task<KgRemedySyncResult> SyncAsync(CancellationToken cancellationToken = default);

    Task<List<KgRemedyProjectionModel>> GetRemediesForRubricAsync(
        int subSectionId,
        CancellationToken cancellationToken = default);
}
