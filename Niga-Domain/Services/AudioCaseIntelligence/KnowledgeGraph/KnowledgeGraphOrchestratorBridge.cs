using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.KnowledgeGraph;

public class KnowledgeGraphOrchestratorBridge : IKnowledgeGraphOrchestratorBridge
{
    private readonly IKgExpressionResolverEngine _expressionResolver;
    private readonly IKgEmbeddingBridge _embeddingBridge;
    private readonly IKnowledgeGraphRanker _ranker;
    private readonly IEnterpriseKnowledgeGraphRepository _repository;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<KnowledgeGraphOrchestratorBridge> _logger;

    public KnowledgeGraphOrchestratorBridge(
        IKgExpressionResolverEngine expressionResolver,
        IKgEmbeddingBridge embeddingBridge,
        IKnowledgeGraphRanker ranker,
        IEnterpriseKnowledgeGraphRepository repository,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<KnowledgeGraphOrchestratorBridge> logger)
    {
        _expressionResolver = expressionResolver;
        _embeddingBridge = embeddingBridge;
        _ranker = ranker;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<KgDiscoveryResult> DiscoverAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        string transcript,
        RubricCandidateEngineResult? embeddingResult,
        CancellationToken cancellationToken = default)
    {
        var result = new KgDiscoveryResult();
        if (!_options.EnableEnterpriseKnowledgeGraph)
        {
            result.Error = "Enterprise Knowledge Graph is disabled.";
            return result;
        }

        var allPaths = new List<KgRubricPathModel>();

        foreach (var meaning in graph.Meanings)
        {
            var metaphor = graph.Metaphors.FirstOrDefault(m => m.PatientMeaningId == meaning.PatientMeaningId);
            var resolved = await _expressionResolver.ResolveAsync(new KgExpressionResolveRequest
            {
                Text = meaning.RawStatement,
                LanguageCode = meaning.LanguageCode,
                ExpressionKind = metaphor != null ? KgExpressionKinds.Metaphor : KgExpressionKinds.Literal,
                ClinicalMeaning = metaphor?.ClinicalMeaning ?? meaning.NormalizedMeaning,
                LiteralMeaning = metaphor?.LiteralMeaning,
                SessionId = sessionId,
            }, cancellationToken);

            result.ExpressionsResolved++;
            allPaths.AddRange(resolved.RubricPaths);
        }

        foreach (var homeo in graph.HomeopathicConcepts)
        {
            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex);
            if (clinical == null) continue;

            var medical = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.MedicalConcept,
                clinical.ConceptName,
                clinical.ConceptName,
                "en",
                confidence: clinical.Confidence,
                cancellationToken: cancellationToken);

            var clinicalNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.ClinicalMeaning,
                clinical.ConceptName,
                clinical.ConceptName,
                "en",
                confidence: clinical.Confidence,
                cancellationToken: cancellationToken);

            var homeoNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.HomeopathicConcept,
                homeo.ConceptName,
                homeo.ConceptName,
                "en",
                confidence: homeo.Confidence,
                cancellationToken: cancellationToken);

            await _repository.UpsertEdgeAsync(
                clinicalNode.NodeId, medical.NodeId, KgEdgeTypes.ImpliesClinical,
                homeo.Weight, clinical.Confidence, false, "SessionProjector", sessionId, cancellationToken);
            await _repository.UpsertEdgeAsync(
                medical.NodeId, homeoNode.NodeId, KgEdgeTypes.MapsHomeopathic,
                homeo.Weight, homeo.Confidence, false, "SessionProjector", sessionId, cancellationToken);

            var embeddingPaths = embeddingResult?.Candidates
                .Where(c => string.Equals(c.SourceConceptName, homeo.ConceptName, StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<RubricCandidateModel>();

            if (embeddingPaths.Count > 0)
            {
                allPaths.AddRange(await _embeddingBridge.ProposeFromEmbeddingsAsync(
                    homeo.ConceptName, embeddingPaths, cancellationToken));
            }
        }

        if (embeddingResult?.Candidates.Count > 0)
        {
            var bridged = await _embeddingBridge.ProposeFromEmbeddingsAsync(
                transcript[..Math.Min(transcript.Length, 200)],
                embeddingResult.Candidates,
                cancellationToken);
            allPaths.AddRange(bridged);
        }

        result.GraphPathsFound = allPaths.Count(x => x.HasKnowledgeGraphPath);
        result.Paths = allPaths
            .GroupBy(x => x.SubSectionId)
            .Select(g => g.OrderByDescending(x => x.CompositeScore).First())
            .ToList();

        foreach (var path in result.Paths)
            await _repository.SaveSessionPathAsync(sessionId, path, cancellationToken);

        var embeddingDiscoveries = embeddingResult?.Discoveries ?? new List<RubricDiscoveryNodeModel>();
        result.Discoveries = _ranker.RankAndMerge(result.Paths, embeddingDiscoveries);
        result.Success = result.Discoveries.Count > 0;

        _logger.LogInformation(
            "KG discovery session={SessionId} expressions={Expressions} paths={Paths} discoveries={Discoveries}",
            sessionId,
            result.ExpressionsResolved,
            result.Paths.Count,
            result.Discoveries.Count);

        return result;
    }

    public async Task ProjectSessionAsync(
        Guid sessionId,
        ConceptGraphFullModel graph,
        IReadOnlyList<AudioCaseSuggestedRubricModel> acceptedRubrics,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableEnterpriseKnowledgeGraph) return;

        foreach (var rubric in acceptedRubrics)
        {
            var rubricNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.RepertoryRubric,
                $"subsection:{rubric.SubSectionId}",
                rubric.SubSectionName,
                subSectionId: rubric.SubSectionId,
                confidence: rubric.ConfidenceScore ?? rubric.MatchScore,
                cancellationToken: cancellationToken);

            var homeoName = rubric.MatchedFrom ?? rubric.WhySuggested ?? rubric.SubSectionName;
            var homeoNode = await _repository.FindOrCreateNodeAsync(
                KgNodeTypes.HomeopathicConcept,
                homeoName,
                homeoName,
                "en",
                confidence: rubric.ConfidenceScore ?? rubric.MatchScore,
                cancellationToken: cancellationToken);

            await _repository.UpsertEdgeAsync(
                homeoNode.NodeId,
                rubricNode.NodeId,
                KgEdgeTypes.SuggestsRubric,
                1.2m,
                rubric.ConfidenceScore ?? rubric.MatchScore,
                isProvisional: false,
                source: "DoctorValidated",
                sourceSessionId: sessionId,
                cancellationToken: cancellationToken);
        }
    }
}
