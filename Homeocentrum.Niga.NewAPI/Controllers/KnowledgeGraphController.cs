using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeGraphController : ControllerBase
{
    private readonly IEnterpriseKnowledgeGraphRepository _repository;
    private readonly IKgExpressionResolverEngine _expressionResolver;
    private readonly IKgGraphTraversalEngine _traversal;
    private readonly IKgBootstrapImporter _bootstrapImporter;
    private readonly IKgRemedyProjectionSync _remedySync;

    public KnowledgeGraphController(
        IEnterpriseKnowledgeGraphRepository repository,
        IKgExpressionResolverEngine expressionResolver,
        IKgGraphTraversalEngine traversal,
        IKgBootstrapImporter bootstrapImporter,
        IKgRemedyProjectionSync remedySync)
    {
        _repository = repository;
        _expressionResolver = expressionResolver;
        _traversal = traversal;
        _bootstrapImporter = bootstrapImporter;
        _remedySync = remedySync;
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(KgGraphStatsModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<KgGraphStatsModel>> GetStats(CancellationToken cancellationToken) =>
        Ok(await _repository.GetStatsAsync(cancellationToken));

    [HttpGet("expressions/search")]
    [ProducesResponseType(typeof(List<KgNodeModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KgNodeModel>>> SearchExpressions(
        [FromQuery] string text,
        [FromQuery] string? languageCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return BadRequest("text is required.");

        return Ok(await _repository.SearchExpressionNodesAsync(text, languageCode, cancellationToken));
    }

    [HttpPost("expressions/resolve")]
    [ProducesResponseType(typeof(KgExpressionResolveResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<KgExpressionResolveResult>> ResolveExpression(
        [FromBody] KgExpressionResolveRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _expressionResolver.ResolveAsync(request, cancellationToken));

    [HttpGet("paths")]
    [ProducesResponseType(typeof(List<KgRubricPathModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KgRubricPathModel>>> GetSessionPaths(
        [FromQuery] Guid sessionId,
        CancellationToken cancellationToken) =>
        Ok(await _repository.GetSessionPathsAsync(sessionId, cancellationToken));

    [HttpGet("nodes/{nodeId:long}/paths")]
    [ProducesResponseType(typeof(List<KgRubricPathModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KgRubricPathModel>>> GetPathsFromNode(
        long nodeId,
        [FromQuery] decimal minConfidence = 0.55m,
        CancellationToken cancellationToken = default) =>
        Ok(await _traversal.FindRubricPathsAsync(nodeId, minConfidence, cancellationToken));

    [HttpGet("rubrics/{subSectionId:int}/remedies")]
    [ProducesResponseType(typeof(List<KgRemedyProjectionModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<KgRemedyProjectionModel>>> GetRemedies(
        int subSectionId,
        CancellationToken cancellationToken) =>
        Ok(await _remedySync.GetRemediesForRubricAsync(subSectionId, cancellationToken));

    [HttpPost("admin/bootstrap/import")]
    [ProducesResponseType(typeof(KgBootstrapImportResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<KgBootstrapImportResult>> ImportBootstrap(CancellationToken cancellationToken) =>
        Ok(await _bootstrapImporter.ImportAsync(cancellationToken));

    [HttpPost("admin/remedies/sync")]
    [ProducesResponseType(typeof(KgRemedySyncResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<KgRemedySyncResult>> SyncRemedies(CancellationToken cancellationToken) =>
        Ok(await _remedySync.SyncAsync(cancellationToken));
}
