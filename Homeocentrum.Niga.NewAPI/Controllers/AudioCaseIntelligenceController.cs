using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V6;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Validation;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Merging;
using System.Reflection;

namespace Homeocentrum.Niga.NewAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AudioCaseIntelligenceController : ControllerBase
{
    private readonly IRubricIntelligenceSettingsService _settings;
    private readonly OpenAiOptions _openAiOptions;
    private readonly IRubricEmbeddingIndexerService _indexerService;
    private readonly IRubricEmbeddingRepository _embeddingRepository;
    private readonly IRubricEmbeddingMemoryCache _embeddingCache;
    private readonly IRubricBenchmarkService _benchmarkService;
    private readonly IV6BenchmarkEvaluationService _v6BenchmarkService;
    private readonly IV7AccuracyBenchmarkService _v7AccuracyBenchmarkService;
    private readonly IDoctorLearningWeightProvider _learningProvider;
    private readonly IRepertoryMappingRepository _repertoryRepository;
    private readonly NIGACentrumContext _context;

    public AudioCaseIntelligenceController(
        IRubricIntelligenceSettingsService settings,
        IOptions<OpenAiOptions> openAiOptions,
        IRubricEmbeddingIndexerService indexerService,
        IRubricEmbeddingRepository embeddingRepository,
        IRubricEmbeddingMemoryCache embeddingCache,
        IRubricBenchmarkService benchmarkService,
        IV6BenchmarkEvaluationService v6BenchmarkService,
        IV7AccuracyBenchmarkService v7AccuracyBenchmarkService,
        IDoctorLearningWeightProvider learningProvider,
        IRepertoryMappingRepository repertoryRepository,
        NIGACentrumContext context)
    {
        _settings = settings;
        _openAiOptions = openAiOptions.Value;
        _indexerService = indexerService;
        _embeddingRepository = embeddingRepository;
        _embeddingCache = embeddingCache;
        _benchmarkService = benchmarkService;
        _v6BenchmarkService = v6BenchmarkService;
        _v7AccuracyBenchmarkService = v7AccuracyBenchmarkService;
        _learningProvider = learningProvider;
        _repertoryRepository = repertoryRepository;
        _context = context;
    }

    /// <summary>V2 engine health, repertory mapping, and rollout gate status.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RubricIntelligenceHealthModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<RubricIntelligenceHealthModel>> GetHealth(CancellationToken cancellationToken)
    {
        var config = _settings.GetConfig();
        var rollout = await _settings.GetRolloutStatusAsync(cancellationToken);
        var repertory = await _repertoryRepository.GetMappingStatusAsync(cancellationToken);
        var stamp = RubricEngineStamp.FromRuntime(_settings.GetBaseOptions(), _settings);

        return Ok(new RubricIntelligenceHealthModel
        {
            V2Enabled = config.IsV2Active,
            RollbackToV1Only = config.RollbackToV1Only,
            RequiresManualApproval = config.RequiresManualApproval,
            EnableRepertoryMapping = config.EnableRepertoryMapping,
            HasRuntimeOverride = config.HasRuntimeOverride,
            EngineVersion = stamp,
            Status = config.RollbackToV1Only ? "RollbackActive" : "Healthy",
            CheckedAtUtc = DateTime.UtcNow,
            RepertoryMapping = repertory,
            RolloutStatus = rollout,
            EnableFastClinicalRetrievalPipeline = config.EnableFastClinicalRetrievalPipeline,
            FastPipelineEngineVersion = config.FastPipelineEngineVersion,
            EnableEciV8Engine = config.EnableEciV8Engine,
            EnableV6ClinicalReasoningEngine = config.EnableV6ClinicalReasoningEngine,
            EnableV7RepertoryIntelligenceEngine = config.EnableV7RepertoryIntelligenceEngine,
            AppVersion = config.AppVersion,
            GitSha = ResolveGitSha(),
        });
    }

    [HttpGet("config")]
    [Authorize]
    [ProducesResponseType(typeof(RubricIntelligenceConfigModel), StatusCodes.Status200OK)]
    public ActionResult<RubricIntelligenceConfigModel> GetConfig()
    {
        return Ok(_settings.GetConfig());
    }

    [HttpPut("config")]
    [Authorize]
    [ProducesResponseType(typeof(RubricIntelligenceConfigModel), StatusCodes.Status200OK)]
    public ActionResult<RubricIntelligenceConfigModel> UpdateConfig([FromBody] RubricIntelligenceConfigUpdateModel request)
    {
        var userId = User.GetUserId();
        _settings.ApplyRuntimeUpdate(request, userId);
        return Ok(_settings.GetConfig());
    }

    [HttpGet("rollout/status")]
    [Authorize]
    [ProducesResponseType(typeof(RubricIntelligenceRolloutStatusModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<RubricIntelligenceRolloutStatusModel>> GetRolloutStatus(CancellationToken cancellationToken)
    {
        return Ok(await _settings.GetRolloutStatusAsync(cancellationToken));
    }

    [HttpPost("rollout/gate")]
    [Authorize]
    public async Task<ActionResult<AiRolloutGate>> RecordRolloutGate(
        [FromBody] AiRolloutGateCreateModel request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FlagName))
            return BadRequest("FlagName is required.");

        var gate = new AiRolloutGate
        {
            FlagName = request.FlagName.Trim(),
            BenchmarkRunUtc = request.BenchmarkRunUtc == default ? DateTime.UtcNow : request.BenchmarkRunUtc,
            Top5Accuracy = request.Top5Accuracy,
            DoctorAcceptanceRate = request.DoctorAcceptanceRate,
            ApprovedByUserId = User.GetUserId(),
            Notes = request.Notes?.Trim(),
            EnteredDate = DateTime.UtcNow,
        };

        _context.AiRolloutGates.Add(gate);
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(gate);
    }

    [HttpGet("repertory/status")]
    [Authorize]
    [ProducesResponseType(typeof(RepertoryMappingStatusModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<RepertoryMappingStatusModel>> GetRepertoryStatus(CancellationToken cancellationToken)
    {
        return Ok(await _repertoryRepository.GetMappingStatusAsync(cancellationToken));
    }

    [HttpGet("embeddings/status")]
    [Authorize]
    [ProducesResponseType(typeof(EmbeddingIndexStatusModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmbeddingIndexStatusModel>> GetEmbeddingStatus(CancellationToken cancellationToken)
    {
        var count = await _embeddingRepository.CountAsync(_openAiOptions.EmbeddingModel, cancellationToken);
        var lastUpdated = await _embeddingRepository.GetLastUpdatedUtcAsync(_openAiOptions.EmbeddingModel, cancellationToken);
        var config = _settings.GetConfig();

        return Ok(new EmbeddingIndexStatusModel
        {
            IndexedRubrics = count,
            CachedVectors = _embeddingCache.Entries.Count,
            ModelName = _openAiOptions.EmbeddingModel,
            LastIndexedUtc = lastUpdated ?? _embeddingCache.LastRefreshedUtc,
            EnableEmbeddingSearch = config.EnableEmbeddingSearch,
        });
    }

    [HttpPost("embeddings/reindex")]
    [Authorize]
    [ProducesResponseType(typeof(EmbeddingReindexResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmbeddingReindexResult>> ReindexEmbeddings(
        [FromQuery] int? maxRubrics,
        CancellationToken cancellationToken)
    {
        var result = await _indexerService.ReindexAsync(maxRubrics, cancellationToken);
        return Ok(result);
    }

    [HttpGet("benchmark/summary")]
    [Authorize]
    [ProducesResponseType(typeof(RubricBenchmarkSummaryModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<RubricBenchmarkSummaryModel>> GetBenchmarkSummary(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await _benchmarkService.GetSummaryAsync(days, cancellationToken);
        return Ok(result);
    }

    [HttpGet("benchmark/trends")]
    [Authorize]
    [ProducesResponseType(typeof(RubricBenchmarkTrendsModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<RubricBenchmarkTrendsModel>> GetBenchmarkTrends(
        [FromQuery] int weeks = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await _benchmarkService.GetTrendsAsync(weeks, cancellationToken);
        return Ok(result);
    }

    [HttpGet("benchmark/v6/curated")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<V6BenchmarkCase>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<V6BenchmarkCase>> GetV6CuratedBenchmarkCases()
    {
        return Ok(_v6BenchmarkService.GetCuratedBenchmarkCases());
    }

    /// <summary>V6: self-validation quality report against curated expert benchmark cases.</summary>
    [HttpPost("benchmark/v6/evaluate")]
    [Authorize]
    [ProducesResponseType(typeof(V6BenchmarkReport), StatusCodes.Status200OK)]
    public async Task<ActionResult<V6BenchmarkReport>> EvaluateV6Benchmark(
        [FromBody] List<V6BenchmarkEvaluateRequest>? cases,
        CancellationToken cancellationToken = default)
    {
        var benchmarkCases = cases?.Count > 0
            ? cases.Select(c => new V6BenchmarkCase
            {
                CaseId = c.CaseId,
                Description = c.Description ?? string.Empty,
                ExpectedRubricPatterns = c.ExpectedRubricPatterns ?? new List<string>(),
                ForbiddenGenericPatterns = c.ForbiddenGenericPatterns ?? new List<string>(),
            }).ToList()
            : _v6BenchmarkService.GetCuratedBenchmarkCases().ToList();

        var requestCases = cases ?? new List<V6BenchmarkEvaluateRequest>();

        var report = await _v6BenchmarkService.EvaluateAsync(
            benchmarkCases,
            async benchmarkCase =>
            {
                var requestCase = requestCases.FirstOrDefault(c => c.CaseId == benchmarkCase.CaseId);
                await Task.CompletedTask;
                return (IReadOnlyList<AudioCaseSuggestedRubricModel>)(requestCase?.ProducedRubrics
                    ?? new List<AudioCaseSuggestedRubricModel>());
            },
            cancellationToken);

        return Ok(report);
    }

    /// <summary>V7: precision/recall/F1/top-5/top-10 accuracy report.</summary>
    [HttpPost("benchmark/v7/accuracy")]
    [Authorize]
    [ProducesResponseType(typeof(V7BenchmarkAccuracyReport), StatusCodes.Status200OK)]
    public ActionResult<V7BenchmarkAccuracyReport> EvaluateV7Accuracy(
        [FromBody] List<V6BenchmarkEvaluateRequest>? cases)
    {
        var inputs = cases?.Count > 0
            ? cases.Select(c => new V7BenchmarkCaseAccuracyInput
            {
                CaseId = c.CaseId,
                ExpectedPatterns = c.ExpectedRubricPatterns ?? new List<string>(),
                ProducedRubrics = c.ProducedRubrics ?? new List<AudioCaseSuggestedRubricModel>(),
            }).ToList()
            : _v7AccuracyBenchmarkService.GetEpilepsyReferenceCases().ToList();

        return Ok(_v7AccuracyBenchmarkService.Evaluate(inputs));
    }

    [HttpGet("feedback/queue")]
    [Authorize]
    [ProducesResponseType(typeof(RubricFeedbackQueueModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<RubricFeedbackQueueModel>> GetFeedbackQueue(
        [FromQuery] int topN = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _benchmarkService.GetFeedbackQueueAsync(topN, cancellationToken);
        return Ok(result);
    }

    /// <summary>Phase 9: aggregated doctor learning signals from AICaseLearning (no repertory mutation).</summary>
    [HttpGet("learning/summary")]
    [Authorize]
    [ProducesResponseType(typeof(DoctorLearningSummaryModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<DoctorLearningSummaryModel>> GetLearningSummary(
        [FromQuery] int topN = 10,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _learningProvider.GetSummaryAsync(topN, cancellationToken));
    }

    private static string? ResolveGitSha()
    {
        var env = Environment.GetEnvironmentVariable("GIT_SHA")
            ?? Environment.GetEnvironmentVariable("GIT_COMMIT")
            ?? Environment.GetEnvironmentVariable("APP_GIT_SHA");
        if (!string.IsNullOrWhiteSpace(env))
            return env.Trim();

        var informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(informational))
            return null;

        var plus = informational.IndexOf('+');
        return plus >= 0 && plus < informational.Length - 1
            ? informational[(plus + 1)..]
            : informational.Trim();
    }
}
