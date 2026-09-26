using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Controllers;

[ApiController]
[Route("api/AudioCaseIntelligence/v3")]
[Authorize]
public class AudioCaseIntelligenceV3Controller : ControllerBase
{
    private readonly IConceptGraphRepository _conceptGraphRepository;
    private readonly IRubricIntelligenceSettingsService _settings;
    private readonly NIGACentrumContext _context;

    public AudioCaseIntelligenceV3Controller(
        IConceptGraphRepository conceptGraphRepository,
        IRubricIntelligenceSettingsService settings,
        NIGACentrumContext context)
    {
        _conceptGraphRepository = conceptGraphRepository;
        _settings = settings;
        _context = context;
    }

    [HttpGet("graph/{sessionId:guid}")]
    [ProducesResponseType(typeof(ConceptGraphFullModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConceptGraphFullModel>> GetGraph(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsV3Active)
            return NotFound(new { message = "V3 Concept Graph is not enabled." });

        var graph = await _conceptGraphRepository.GetFullGraphAsync(sessionId, cancellationToken);
        if (graph.Meanings.Count == 0)
            return NotFound(new { message = "No V3 concept graph found for this session." });

        return Ok(graph);
    }

    [HttpGet("meanings/{sessionId:guid}")]
    [ProducesResponseType(typeof(ConceptGraphMeaningGraphModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ConceptGraphMeaningGraphModel>> GetMeanings(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsV3Active)
            return NotFound(new { message = "V3 Concept Graph is not enabled." });

        var meanings = await _conceptGraphRepository.GetPatientMeaningsAsync(sessionId, cancellationToken);
        if (meanings.Count == 0)
            return NotFound(new { message = "No V3 meaning graph found for this session." });

        return Ok(new ConceptGraphMeaningGraphModel
        {
            SessionId = sessionId,
            EngineVersion = "v3.5",
            Meanings = meanings,
        });
    }

    [HttpGet("coverage/{sessionId:guid}")]
    [ProducesResponseType(typeof(CaseCoverageMetricsModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<CaseCoverageMetricsModel>> GetCoverage(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        if (!_settings.IsV3Active)
            return NotFound(new { message = "V3 is not enabled." });

        var metrics = await _context.AiCaseCoverageMetrics.AsNoTracking()
            .Where(x => x.AudioCaseSessionId == sessionId)
            .OrderByDescending(x => x.EnteredDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (metrics == null)
            return NotFound(new { message = "No coverage metrics for this session." });

        return Ok(new CaseCoverageMetricsModel
        {
            TranscriptCoverage = metrics.TranscriptCoverage,
            CaseCompleteness = metrics.CaseCompleteness,
            TotalBlocks = metrics.TotalBlocks,
            CoveredBlocks = metrics.CoveredBlocks,
            Tier1Count = metrics.Tier1Count,
            Tier2Count = metrics.Tier2Count,
            Tier3Count = metrics.Tier3Count,
            MissingSymptomCount = metrics.MissingSymptomCount,
        });
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<object> GetHealth()
    {
        var opts = _settings.GetBaseOptions();
        return Ok(new
        {
            v3Enabled = _settings.IsV3Active,
            v35RecallEnabled = opts.EnableV35RecallEngine,
            engineVersion = _settings.IsV3Active
                ? (opts.EnableV35RecallEngine ? "v3.5" : "v3")
                : _settings.IsV2Active ? "v2" : "v1",
            phasesAvailable = new[]
            {
                "CaseDecomposition",
                "PatientMeaningGraph",
                "MultiSymptomDiscovery",
                "CategoryDiscovery",
                "MetaphorUnderstanding",
                "ClinicalConcept",
                "HomeopathicConcept",
                "RecallExpansion",
                "ConceptClustering",
                "RubricDiscovery",
                "ClinicalValidationV21",
                "EvidenceChain",
                "TranscriptCoverage",
                "MissingSymptomDetector",
                "CaseCompletenessScore",
            },
            checkedAtUtc = DateTime.UtcNow,
        });
    }
}
