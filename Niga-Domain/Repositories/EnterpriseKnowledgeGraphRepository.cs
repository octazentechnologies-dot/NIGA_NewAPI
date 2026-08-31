using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories;

public class EnterpriseKnowledgeGraphRepository : IEnterpriseKnowledgeGraphRepository
{
    private readonly NIGACentrumContext _context;

    public EnterpriseKnowledgeGraphRepository(NIGACentrumContext context) => _context = context;

    public async Task<KgNodeModel> FindOrCreateNodeAsync(
        string nodeType,
        string canonicalKey,
        string displayText,
        string? languageCode = null,
        string? expressionKind = null,
        int? subSectionId = null,
        int? remedyId = null,
        decimal confidence = 0.7m,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizeKey(canonicalKey);
        var lang = NormalizeLanguage(languageCode);

        var existing = await _context.AiKgNodes
            .FirstOrDefaultAsync(x =>
                x.NodeType == nodeType
                && x.CanonicalKey == key
                && x.LanguageCode == lang,
                cancellationToken);

        if (existing != null)
        {
            existing.DisplayText = displayText.Trim();
            existing.Confidence = Math.Max(existing.Confidence, confidence);
            existing.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapNode(existing);
        }

        var node = new AiKgNode
        {
            NodeType = nodeType,
            CanonicalKey = key,
            DisplayText = displayText.Trim(),
            LanguageCode = lang,
            ExpressionKind = expressionKind,
            SubSectionId = subSectionId,
            RemedyId = remedyId,
            Confidence = confidence,
            Status = "Active",
            EnteredDate = DateTime.UtcNow,
        };
        _context.AiKgNodes.Add(node);
        await _context.SaveChangesAsync(cancellationToken);
        return MapNode(node);
    }

    public async Task<KgNodeModel?> FindNodeAsync(
        string nodeType,
        string canonicalKey,
        string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        var entity = await _context.AiKgNodes.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.NodeType == nodeType
                && x.CanonicalKey == NormalizeKey(canonicalKey)
                && x.LanguageCode == NormalizeLanguage(languageCode),
                cancellationToken);
        return entity == null ? null : MapNode(entity);
    }

    public async Task<List<KgNodeModel>> SearchExpressionNodesAsync(
        string text,
        string? languageCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<KgNodeModel>();

        var normalized = text.Trim().ToLowerInvariant();
        var lang = NormalizeLanguage(languageCode);

        var query = _context.AiKgNodes.AsNoTracking()
            .Where(x => x.NodeType == KgNodeTypes.PatientExpression && x.Status == "Active");

        if (lang != null)
            query = query.Where(x => x.LanguageCode == lang || x.LanguageCode == null);

        var nodes = await query
            .Where(x =>
                x.CanonicalKey.Contains(normalized)
                || x.DisplayText.ToLower().Contains(normalized))
            .OrderByDescending(x => x.Confidence)
            .Take(20)
            .ToListAsync(cancellationToken);

        return nodes.Select(MapNode).ToList();
    }

    public async Task<KgEdgeModel> UpsertEdgeAsync(
        long fromNodeId,
        long toNodeId,
        string edgeType,
        decimal weight,
        decimal confidence,
        bool isProvisional,
        string source,
        Guid? sourceSessionId = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.AiKgEdges
            .FirstOrDefaultAsync(x =>
                x.FromNodeId == fromNodeId
                && x.ToNodeId == toNodeId
                && x.EdgeType == edgeType,
                cancellationToken);

        if (existing != null)
        {
            existing.Weight = Math.Clamp(existing.Weight + weight * 0.1m, 0.1m, 5m);
            existing.Confidence = Math.Max(existing.Confidence, confidence);
            existing.IsProvisional = existing.IsProvisional && isProvisional;
            existing.Source = source;
            existing.SourceSessionId = sourceSessionId ?? existing.SourceSessionId;
            existing.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return MapEdge(existing);
        }

        var edge = new AiKgEdge
        {
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            EdgeType = edgeType,
            Weight = weight,
            Confidence = confidence,
            IsProvisional = isProvisional,
            Source = source,
            SourceSessionId = sourceSessionId,
            EnteredDate = DateTime.UtcNow,
        };
        _context.AiKgEdges.Add(edge);
        await _context.SaveChangesAsync(cancellationToken);
        return MapEdge(edge);
    }

    public async Task AdjustEdgeWeightAsync(
        long edgeId,
        decimal weightDelta,
        bool confirmProvisional = false,
        CancellationToken cancellationToken = default)
    {
        var edge = await _context.AiKgEdges.FirstOrDefaultAsync(x => x.EdgeId == edgeId, cancellationToken);
        if (edge == null) return;

        edge.Weight = Math.Clamp(edge.Weight + weightDelta, 0.05m, 5m);
        if (confirmProvisional)
            edge.IsProvisional = false;
        edge.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSessionPathAsync(
        Guid sessionId,
        KgRubricPathModel path,
        CancellationToken cancellationToken = default)
    {
        await _context.AiKgSessionPaths
            .Where(x => x.AudioCaseSessionId == sessionId && x.SubSectionId == path.SubSectionId)
            .ExecuteDeleteAsync(cancellationToken);

        _context.AiKgSessionPaths.Add(new AiKgSessionPath
        {
            AudioCaseSessionId = sessionId,
            SubSectionId = path.SubSectionId,
            PathJson = JsonSerializer.Serialize(path.Steps),
            PathConfidence = path.PathConfidence,
            DiscoveryMethod = path.DiscoveryMethod,
            EnteredDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<KgRubricPathModel>> GetSessionPathsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _context.AiKgSessionPaths.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new KgRubricPathModel
        {
            SubSectionId = r.SubSectionId,
            PathConfidence = r.PathConfidence,
            DiscoveryMethod = r.DiscoveryMethod,
            HasKnowledgeGraphPath = true,
            Steps = JsonSerializer.Deserialize<List<KgPathStepModel>>(r.PathJson) ?? new List<KgPathStepModel>(),
        }).ToList();
    }

    public async Task RecordFeedbackMutationAsync(
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
        CancellationToken cancellationToken = default)
    {
        _context.AiKgFeedbackMutations.Add(new AiKgFeedbackMutation
        {
            AudioCaseSessionId = sessionId,
            FeedbackId = feedbackId,
            FeedbackType = feedbackType,
            MutationType = mutationType,
            EdgeId = edgeId,
            NodeId = nodeId,
            SubSectionId = subSectionId,
            CorrectedSubSectionId = correctedSubSectionId,
            WeightDelta = weightDelta,
            DetailsJson = detailsJson,
            DoctorUserId = doctorUserId,
            EnteredDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveFigurativeResolutionAsync(
        long expressionNodeId,
        long clinicalMeaningNodeId,
        string expressionKind,
        string? literalMeaning,
        string? explanation,
        decimal confidence,
        string source,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.AiKgFigurativeResolutions
            .AnyAsync(x => x.ExpressionNodeId == expressionNodeId, cancellationToken);
        if (exists) return;

        _context.AiKgFigurativeResolutions.Add(new AiKgFigurativeResolution
        {
            ExpressionNodeId = expressionNodeId,
            ClinicalMeaningNodeId = clinicalMeaningNodeId,
            ExpressionKind = expressionKind,
            LiteralMeaning = literalMeaning,
            ResolutionExplanation = explanation,
            Confidence = confidence,
            Source = source,
            EnteredDate = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<KgNodeModel?> GetFigurativeClinicalNodeAsync(
        long expressionNodeId,
        CancellationToken cancellationToken = default)
    {
        var resolution = await _context.AiKgFigurativeResolutions.AsNoTracking()
            .Where(x => x.ExpressionNodeId == expressionNodeId)
            .OrderByDescending(x => x.Confidence)
            .FirstOrDefaultAsync(cancellationToken);

        if (resolution == null) return null;

        var node = await _context.AiKgNodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NodeId == resolution.ClinicalMeaningNodeId, cancellationToken);
        return node == null ? null : MapNode(node);
    }

    public async Task<KgGraphStatsModel> GetStatsAsync(CancellationToken cancellationToken = default) =>
        new()
        {
            EngineVersion = "v11",
            NodeCount = await _context.AiKgNodes.CountAsync(cancellationToken),
            EdgeCount = await _context.AiKgEdges.CountAsync(cancellationToken),
            ExpressionCount = await _context.AiKgNodes.CountAsync(
                x => x.NodeType == KgNodeTypes.PatientExpression, cancellationToken),
            RubricLinkCount = await _context.AiKgEdges.CountAsync(
                x => x.EdgeType == KgEdgeTypes.SuggestsRubric, cancellationToken),
            RemedyProjectionCount = await _context.AiKgRemedyProjections.CountAsync(cancellationToken),
            FigurativeResolutionCount = await _context.AiKgFigurativeResolutions.CountAsync(cancellationToken),
            FeedbackMutationCount = await _context.AiKgFeedbackMutations.CountAsync(cancellationToken),
            GeneratedAtUtc = DateTime.UtcNow,
        };

    internal async Task<List<(AiKgEdge Edge, AiKgNode Node)>> GetOutgoingEdgesInternalAsync(
        long nodeId,
        CancellationToken cancellationToken) =>
        await (from edge in _context.AiKgEdges.AsNoTracking()
            join node in _context.AiKgNodes.AsNoTracking() on edge.ToNodeId equals node.NodeId
            where edge.FromNodeId == nodeId
            select new ValueTuple<AiKgEdge, AiKgNode>(edge, node))
            .ToListAsync(cancellationToken);

    public async Task<List<(KgEdgeModel Edge, KgNodeModel Node)>> GetOutgoingEdgesAsync(
        long nodeId,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetOutgoingEdgesInternalAsync(nodeId, cancellationToken);
        return rows.Select(r => (MapEdge(r.Item1), MapNode(r.Item2))).ToList();
    }

    public async Task<KgNodeModel?> GetNodeByIdAsync(long nodeId, CancellationToken cancellationToken = default)
    {
        var node = await _context.AiKgNodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NodeId == nodeId, cancellationToken);
        return node == null ? null : MapNode(node);
    }

    internal async Task<AiKgNode?> GetNodeEntityAsync(long nodeId, CancellationToken cancellationToken) =>
        await _context.AiKgNodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NodeId == nodeId, cancellationToken);

    private static string NormalizeKey(string value) =>
        value.Trim().ToLowerInvariant();

    private static string? NormalizeLanguage(string? languageCode) =>
        string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();

    private static KgNodeModel MapNode(AiKgNode x) => new()
    {
        NodeId = x.NodeId,
        NodeType = x.NodeType,
        CanonicalKey = x.CanonicalKey,
        DisplayText = x.DisplayText,
        LanguageCode = x.LanguageCode,
        ExpressionKind = x.ExpressionKind,
        SubSectionId = x.SubSectionId,
        RemedyId = x.RemedyId,
        Confidence = x.Confidence,
        Status = x.Status,
    };

    private static KgEdgeModel MapEdge(AiKgEdge x) => new()
    {
        EdgeId = x.EdgeId,
        FromNodeId = x.FromNodeId,
        ToNodeId = x.ToNodeId,
        EdgeType = x.EdgeType,
        Weight = x.Weight,
        Confidence = x.Confidence,
        IsProvisional = x.IsProvisional,
        Source = x.Source,
    };
}
