using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.API.Helpers;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Interfaces;

namespace Niga_Domain.API.Controllers;

[Route("api/AiEmbeddingInfrastructure")]
[ApiController]
[Authorize]
public class AiEmbeddingInfrastructureController : ControllerBase
{
    private readonly IAiEmbeddingInfrastructureService _infrastructureService;
    private readonly IAiEnterpriseRubricEmbeddingBuilder _builder;
    private readonly IAiEnterpriseConceptEmbeddingBuilder _conceptBuilder;
    private readonly IAiEnterpriseSemanticSearchService _semanticSearchService;
    private readonly IRubricCandidateEngine _rubricCandidateEngine;
    private readonly IRubricEnterpriseEvidenceChainEnricher _evidenceChainEnricher;
    private readonly IConceptGraphRepository _conceptGraphRepository;
    private readonly IAiIncrementalEmbeddingRefreshService _incrementalRefreshService;
    private readonly IAiEmbeddingRefreshJobRunner _refreshJobRunner;
    private readonly ILogger<AiEmbeddingInfrastructureController> _logger;

    public AiEmbeddingInfrastructureController(
        IAiEmbeddingInfrastructureService infrastructureService,
        IAiEnterpriseRubricEmbeddingBuilder builder,
        IAiEnterpriseConceptEmbeddingBuilder conceptBuilder,
        IAiEnterpriseSemanticSearchService semanticSearchService,
        IRubricCandidateEngine rubricCandidateEngine,
        IRubricEnterpriseEvidenceChainEnricher evidenceChainEnricher,
        IConceptGraphRepository conceptGraphRepository,
        IAiIncrementalEmbeddingRefreshService incrementalRefreshService,
        IAiEmbeddingRefreshJobRunner refreshJobRunner,
        ILogger<AiEmbeddingInfrastructureController> logger)
    {
        _infrastructureService = infrastructureService;
        _builder = builder;
        _conceptBuilder = conceptBuilder;
        _semanticSearchService = semanticSearchService;
        _rubricCandidateEngine = rubricCandidateEngine;
        _evidenceChainEnricher = evidenceChainEnricher;
        _conceptGraphRepository = conceptGraphRepository;
        _incrementalRefreshService = incrementalRefreshService;
        _refreshJobRunner = refreshJobRunner;
        _logger = logger;
    }

    [HttpGet("status")]
    public async Task<object> GetStatus(CancellationToken cancellationToken)
    {
        try
        {
            var status = await _infrastructureService.GetStatusAsync(cancellationToken);
            return ThreeDBodyPartApiResponseHelper.Success(status, "Embedding infrastructure status fetched.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get embedding infrastructure status failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("rubrics/build")]
    public async Task<object> BuildRubricEmbeddings(
        [FromBody] BuildEnterpriseRubricEmbeddingsRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            request ??= new BuildEnterpriseRubricEmbeddingsRequest();
            request.ActorUserId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(request.TriggerSource))
                request.TriggerSource = "Api";

            var result = await _builder.BuildAsync(request, cancellationToken);
            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Enterprise rubric embeddings built.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Enterprise rubric embedding build failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enterprise rubric embedding build failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("incremental/sync-state")]
    public async Task<object> GetIncrementalSyncState(
        [FromQuery] Guid? embeddingVersionId,
        CancellationToken cancellationToken)
    {
        try
        {
            var state = await _incrementalRefreshService.GetSyncStateAsync(embeddingVersionId, cancellationToken);
            return ThreeDBodyPartApiResponseHelper.Success(state, "Incremental sync state fetched.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get incremental sync state failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("incremental/refresh")]
    public async Task<object> RunIncrementalRefresh(
        [FromBody] IncrementalEmbeddingRefreshRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            request ??= new IncrementalEmbeddingRefreshRequest();
            request.ActorUserId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(request.TriggerSource))
                request.TriggerSource = "ApiManual";

            var result = await _refreshJobRunner.RunAsync(request, cancellationToken);
            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Incremental embedding refresh completed.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Incremental embedding refresh failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental embedding refresh failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("incremental/detect")]
    public async Task<object> DetectIncrementalChanges(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _refreshJobRunner.RunAsync(new IncrementalEmbeddingRefreshRequest
            {
                ActorUserId = User.GetUserId(),
                TriggerSource = "ApiDetectOnly",
                DetectOnly = true,
            }, cancellationToken);

            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Incremental change detection completed.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Incremental change detection failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental change detection failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("incremental/process")]
    public async Task<object> ProcessIncrementalQueue(
        [FromBody] IncrementalEmbeddingRefreshRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            request.ActorUserId = User.GetUserId();
            request.ProcessOnly = true;
            if (string.IsNullOrWhiteSpace(request.TriggerSource))
                request.TriggerSource = "ApiProcessOnly";

            var result = await _refreshJobRunner.RunAsync(request, cancellationToken);
            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Incremental queue processing completed.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Incremental queue processing failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental queue processing failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("concepts/build")]
    public async Task<object> BuildConceptEmbeddings(
        [FromBody] BuildEnterpriseConceptEmbeddingsRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            request ??= new BuildEnterpriseConceptEmbeddingsRequest();
            request.ActorUserId = User.GetUserId();
            if (string.IsNullOrWhiteSpace(request.TriggerSource))
                request.TriggerSource = "Api";

            var result = await _conceptBuilder.BuildAsync(request, cancellationToken);
            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Enterprise concept embeddings built.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Enterprise concept embedding build failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enterprise concept embedding build failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("semantic-search")]
    public async Task<object> SemanticSearch(
        [FromBody] EnterpriseSemanticSearchRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ClinicalConcept))
                return ThreeDBodyPartApiResponseHelper.Failure("Clinical concept is required.");

            var result = await _semanticSearchService.SearchAsync(request, cancellationToken);
            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Enterprise semantic search completed.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Enterprise semantic search returned no matches.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enterprise semantic search failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpPost("rubric-candidates")]
    public async Task<object> DiscoverRubricCandidates(
        [FromBody] RubricCandidateGraphRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            ConceptGraphFullModel graph;
            if (request.SessionId.HasValue)
            {
                graph = await _conceptGraphRepository.GetFullGraphAsync(request.SessionId.Value, cancellationToken);
                graph.SessionId = request.SessionId.Value;
            }
            else if (request.Graph != null && request.Graph.HomeopathicConcepts.Count > 0)
            {
                graph = request.Graph;
            }
            else
            {
                return ThreeDBodyPartApiResponseHelper.Failure("SessionId or Graph with homeopathic concepts is required.");
            }

            var engineRequest = new RubricCandidateEngineRequest
            {
                EmbeddingVersionId = request.EmbeddingVersionId,
                MaxCandidatesPerConcept = request.MaxCandidatesPerConcept,
                MaxTotalCandidates = request.MaxTotalCandidates,
                IncludeValidation = request.IncludeValidation,
            };

            var result = await _rubricCandidateEngine.DiscoverFromGraphAsync(graph, engineRequest, cancellationToken);
            return result.Success
                ? ThreeDBodyPartApiResponseHelper.Success(result, "Rubric candidate discovery completed.")
                : ThreeDBodyPartApiResponseHelper.Failure(result.Error ?? "Rubric candidate discovery returned no matches.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rubric candidate discovery failed.");
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }

    [HttpGet("rubric-evidence-chains/{sessionId:guid}")]
    public async Task<object> GetRubricEvidenceChains(Guid sessionId, CancellationToken cancellationToken)
    {
        try
        {
            var graph = await _conceptGraphRepository.GetFullGraphAsync(sessionId, cancellationToken);
            if (graph.EnterpriseEvidenceChains.Count > 0)
            {
                return ThreeDBodyPartApiResponseHelper.Success(
                    new RubricEvidenceChainSessionResult
                    {
                        SessionId = sessionId,
                        EngineVersion = graph.EngineVersion,
                        Chains = graph.EnterpriseEvidenceChains,
                    },
                    "Enterprise rubric evidence chains fetched.");
            }

            return ThreeDBodyPartApiResponseHelper.Failure(
                "No enterprise evidence chains are stored for this session. Run case analysis with EnableEnterpriseRubricEvidenceChain enabled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get rubric evidence chains failed for session {SessionId}", sessionId);
            return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
        }
    }
}
