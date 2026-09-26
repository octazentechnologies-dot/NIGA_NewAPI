using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Repositories;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.KnowledgeGraph;

public class KgGraphTraversalEngine : IKgGraphTraversalEngine
{
    private readonly IEnterpriseKnowledgeGraphRepository _repository;

    public KgGraphTraversalEngine(IEnterpriseKnowledgeGraphRepository repository) =>
        _repository = repository;

    public async Task<List<KgRubricPathModel>> FindRubricPathsAsync(
        long startNodeId,
        decimal minPathConfidence,
        CancellationToken cancellationToken = default)
    {
        var results = new List<KgRubricPathModel>();
        var queue = new Queue<(long NodeId, List<KgPathStepModel> Steps, decimal Confidence, bool Provisional)>();
        queue.Enqueue((startNodeId, new List<KgPathStepModel>(), 1m, false));

        while (queue.Count > 0)
        {
            var (nodeId, steps, confidence, provisional) = queue.Dequeue();
            if (steps.Count > 6) continue;

            var node = await _repository.GetNodeByIdAsync(nodeId, cancellationToken);
            if (node == null) continue;

            if (node.NodeType == KgNodeTypes.RepertoryRubric && node.SubSectionId.HasValue)
            {
                if (confidence >= minPathConfidence)
                {
                    results.Add(new KgRubricPathModel
                    {
                        SubSectionId = node.SubSectionId.Value,
                        SubSectionName = node.DisplayText,
                        PathConfidence = Math.Round(confidence, 4),
                        CompositeScore = Math.Round(confidence, 4),
                        HasKnowledgeGraphPath = true,
                        IsProvisionalPath = provisional,
                        Steps = steps.ToList(),
                        DiscoveryMethod = "KnowledgeGraph",
                    });
                }
                continue;
            }

            var outgoing = await _repository.GetOutgoingEdgesAsync(nodeId, cancellationToken);
            foreach (var (edge, target) in outgoing)
            {
                if (edge.EdgeType == KgEdgeTypes.Contradicts) continue;

                var stepConfidence = confidence * edge.Confidence * Math.Clamp(edge.Weight / 2m, 0.2m, 1.5m);
                var nextSteps = new List<KgPathStepModel>(steps)
                {
                    new()
                    {
                        Layer = target.NodeType,
                        NodeId = target.NodeId,
                        DisplayText = target.DisplayText,
                        EdgeType = edge.EdgeType,
                        Confidence = edge.Confidence,
                    },
                };

                queue.Enqueue((target.NodeId, nextSteps, stepConfidence, provisional || edge.IsProvisional));
            }
        }

        return results
            .GroupBy(x => x.SubSectionId)
            .Select(g => g.OrderByDescending(x => x.PathConfidence).First())
            .OrderByDescending(x => x.PathConfidence)
            .ToList();
    }
}

public class KgExpressionResolverEngine : IKgExpressionResolverEngine
{
    private readonly IEnterpriseKnowledgeGraphRepository _repository;
    private readonly IKgGraphTraversalEngine _traversal;
    private readonly RubricIntelligenceOptions _options;

    public KgExpressionResolverEngine(
        IEnterpriseKnowledgeGraphRepository repository,
        IKgGraphTraversalEngine traversal,
        IOptions<RubricIntelligenceOptions> options)
    {
        _repository = repository;
        _traversal = traversal;
        _options = options.Value;
    }

    public async Task<KgExpressionResolveResult> ResolveAsync(
        KgExpressionResolveRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new KgExpressionResolveResult();
        if (string.IsNullOrWhiteSpace(request.Text))
            return result;

        var language = NormalizeLanguage(request.LanguageCode);
        var expressionKind = request.ExpressionKind ?? KgExpressionKinds.Literal;

        var matches = await _repository.SearchExpressionNodesAsync(request.Text, language, cancellationToken);
        KgNodeModel? expressionNode = matches.FirstOrDefault();

        if (expressionNode == null)
        {
            expressionNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.PatientExpression,
                request.Text,
                request.Text,
                language,
                expressionKind,
                confidence: 0.6m,
                cancellationToken: cancellationToken);
        }

        result.ExpressionNode = expressionNode;

        var figurativeClinical = await _repository.GetFigurativeClinicalNodeAsync(expressionNode.NodeId, cancellationToken);
        if (figurativeClinical != null)
        {
            result.ResolvedClinicalNode = figurativeClinical;
            result.ResolvedFromFigurativeStore = true;
        }
        else if (!string.IsNullOrWhiteSpace(request.ClinicalMeaning))
        {
            result.ResolvedClinicalNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.ClinicalMeaning,
                request.ClinicalMeaning,
                request.ClinicalMeaning,
                "en",
                confidence: 0.75m,
                cancellationToken: cancellationToken);

            await _repository.UpsertEdgeAsync(
                expressionNode.NodeId,
                result.ResolvedClinicalNode.NodeId,
                expressionKind is KgExpressionKinds.Metaphor or KgExpressionKinds.Idiom or KgExpressionKinds.Colloquial
                    ? KgEdgeTypes.ResolvesTo
                    : KgEdgeTypes.Expresses,
                1m,
                0.75m,
                isProvisional: true,
                source: "Session",
                sourceSessionId: request.SessionId,
                cancellationToken: cancellationToken);

            if (expressionKind != KgExpressionKinds.Literal)
            {
                await _repository.SaveFigurativeResolutionAsync(
                    expressionNode.NodeId,
                    result.ResolvedClinicalNode.NodeId,
                    expressionKind,
                    request.LiteralMeaning,
                    request.ClinicalMeaning,
                    0.75m,
                    "Session",
                    cancellationToken);
            }
        }

        if (result.ResolvedClinicalNode == null && matches.Count > 1)
        {
            foreach (var alt in matches.Skip(1))
            {
                var crossClinical = await _repository.GetFigurativeClinicalNodeAsync(alt.NodeId, cancellationToken);
                if (crossClinical == null) continue;
                result.ResolvedClinicalNode = crossClinical;
                result.ResolvedFromCrossLanguage = true;
                await _repository.UpsertEdgeAsync(
                    expressionNode.NodeId,
                    alt.NodeId,
                    KgEdgeTypes.SameMeaning,
                    0.5m,
                    0.7m,
                    isProvisional: false,
                    source: "CrossLanguage",
                    cancellationToken: cancellationToken);
                break;
            }
        }

        var startNodeId = result.ResolvedClinicalNode?.NodeId ?? expressionNode.NodeId;
        result.RubricPaths = await _traversal.FindRubricPathsAsync(
            startNodeId,
            _options.KnowledgeGraphMinPathConfidence,
            cancellationToken);

        return result;
    }

    private static string? NormalizeLanguage(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "en";
        var c = code.Trim().ToLowerInvariant();
        return c switch
        {
            "marathi" or "mr-in" => "mr",
            "hindi" or "hi-in" => "hi",
            _ => c.Length > 2 ? c[..2] : c,
        };
    }
}

public class KgEmbeddingBridge : IKgEmbeddingBridge
{
    private readonly IEnterpriseKnowledgeGraphRepository _repository;

    public KgEmbeddingBridge(IEnterpriseKnowledgeGraphRepository repository) => _repository = repository;

    public async Task<List<KgRubricPathModel>> ProposeFromEmbeddingsAsync(
        string queryText,
        IReadOnlyList<RubricCandidateModel> embeddingCandidates,
        CancellationToken cancellationToken = default)
    {
        var homeoNode = await _repository.FindOrCreateNodeAsync(
            KgNodeTypes.HomeopathicConcept,
            queryText,
            queryText,
            "en",
            confidence: 0.65m,
            cancellationToken: cancellationToken);

        var paths = new List<KgRubricPathModel>();
        foreach (var candidate in embeddingCandidates.Take(10))
        {
            var rubricNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.RepertoryRubric,
                $"subsection:{candidate.SubSectionId}",
                candidate.SubSectionName,
                subSectionId: candidate.SubSectionId,
                confidence: candidate.CompositeScore,
                cancellationToken: cancellationToken);

            await _repository.UpsertEdgeAsync(
                homeoNode.NodeId,
                rubricNode.NodeId,
                KgEdgeTypes.SuggestsRubric,
                candidate.CompositeScore,
                candidate.SimilarityScore,
                isProvisional: true,
                source: "EmbeddingBridge",
                cancellationToken: cancellationToken);

            paths.Add(new KgRubricPathModel
            {
                SubSectionId = candidate.SubSectionId,
                SubSectionName = candidate.SubSectionName,
                PathConfidence = candidate.CompositeScore * 0.85m,
                CompositeScore = candidate.CompositeScore,
                HasKnowledgeGraphPath = false,
                IsProvisionalPath = true,
                EmbeddingScore = candidate.SimilarityScore,
                DiscoveryMethod = "EmbeddingBridge",
                Steps =
                [
                    new KgPathStepModel
                    {
                        Layer = KgNodeTypes.HomeopathicConcept,
                        NodeId = homeoNode.NodeId,
                        DisplayText = queryText,
                        EdgeType = KgEdgeTypes.SuggestsRubric,
                        Confidence = candidate.SimilarityScore,
                    },
                    new KgPathStepModel
                    {
                        Layer = KgNodeTypes.RepertoryRubric,
                        NodeId = rubricNode.NodeId,
                        DisplayText = candidate.SubSectionName,
                        EdgeType = KgEdgeTypes.SuggestsRubric,
                        Confidence = candidate.CompositeScore,
                    },
                ],
            });
        }

        return paths;
    }
}

public class KnowledgeGraphRanker : IKnowledgeGraphRanker
{
    private readonly RubricIntelligenceOptions _options;

    public KnowledgeGraphRanker(IOptions<RubricIntelligenceOptions> options) => _options = options.Value;

    public List<RubricDiscoveryNodeModel> RankAndMerge(
        IReadOnlyList<KgRubricPathModel> graphPaths,
        IReadOnlyList<RubricDiscoveryNodeModel> embeddingDiscoveries)
    {
        var blend = _options.KnowledgeGraphEmbeddingBlendRatio;
        var map = new Dictionary<int, RubricDiscoveryNodeModel>();

        foreach (var path in graphPaths)
        {
            var kgScore = path.HasKnowledgeGraphPath ? path.PathConfidence : path.CompositeScore * 0.7m;
            var embedding = embeddingDiscoveries.FirstOrDefault(d => d.SubSectionId == path.SubSectionId);
            var embeddingScore = embedding?.Confidence ?? path.EmbeddingScore ?? 0m;
            var composite = path.HasKnowledgeGraphPath
                ? (kgScore * (1m - blend)) + (embeddingScore * blend)
                : embeddingScore * blend;

            if (path.IsProvisionalPath && composite < _options.KnowledgeGraphMinPathConfidence)
                composite *= 0.85m;

            map[path.SubSectionId] = new RubricDiscoveryNodeModel
            {
                SubSectionId = path.SubSectionId,
                SubSectionName = path.SubSectionName,
                Confidence = Math.Round(composite, 4),
                MatchReason = path.HasKnowledgeGraphPath
                    ? $"KG path ({path.Steps.Count} hops, conf={path.PathConfidence:0.00})"
                    : $"Embedding+provisional KG (sim={path.EmbeddingScore:0.00})",
                DiscoveryMethod = path.DiscoveryMethod,
                RubricTier = ResolveTier(composite),
            };
        }

        foreach (var discovery in embeddingDiscoveries)
        {
            if (map.ContainsKey(discovery.SubSectionId)) continue;
            if (!discovery.DiscoveryMethod.Contains("Embedding", StringComparison.OrdinalIgnoreCase))
            {
                map[discovery.SubSectionId] = discovery;
                continue;
            }

            discovery.Confidence = Math.Round(discovery.Confidence * blend, 4);
            discovery.MatchReason = $"Embedding-only review ({discovery.Confidence:0.00})";
            discovery.RubricTier = "Review";
            map[discovery.SubSectionId] = discovery;
        }

        return map.Values
            .OrderByDescending(d => d.Confidence)
            .ToList();
    }

    private static string ResolveTier(decimal score) =>
        score >= 0.90m ? "Tier1" : score >= 0.75m ? "Tier2" : score >= 0.60m ? "Tier3" : "Review";
}
