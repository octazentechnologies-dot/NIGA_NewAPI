using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Embeddings;
using Niga_Domain.Services.AudioCaseIntelligence.Engines;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;

namespace Niga_Domain.Services.AudioCaseIntelligence.Orchestration;

/// <summary>
/// Production fast path (Stages C–F + Accuracy/Retrieval packs):
/// symptoms → speaker/negation filter → multi-query expand
/// → parallel Exact/V1 + Keyword/FTS + Alias + Embedding + Catalog
/// → quality / gender / hierarchy / hallucination gates
/// → canonical score → MMR ≤ N DB-backed rubrics + evidence contract.
/// </summary>
public sealed class FastClinicalRetrievalOrchestrator : IFastClinicalRetrievalOrchestrator
{
    private readonly IConceptKeywordDiscoveryEngine _keywordDiscovery;
    private readonly IRubricAliasEngine _aliasEngine;
    private readonly IEmbeddingSearchEngine _embeddingSearch;
    private readonly IFastClinicalRubricCatalog _rubricCatalog;
    private readonly IDoctorLearningWeightProvider _doctorLearning;
    private readonly IAudioCaseIntelligenceRepository _intelligenceRepository;
    private readonly RubricIntelligenceOptions _options;
    private readonly IRubricPipelineTelemetry _telemetry;
    private readonly ILogger<FastClinicalRetrievalOrchestrator> _logger;

    public FastClinicalRetrievalOrchestrator(
        IConceptKeywordDiscoveryEngine keywordDiscovery,
        IRubricAliasEngine aliasEngine,
        IEmbeddingSearchEngine embeddingSearch,
        IFastClinicalRubricCatalog rubricCatalog,
        IDoctorLearningWeightProvider doctorLearning,
        IAudioCaseIntelligenceRepository intelligenceRepository,
        IOptions<RubricIntelligenceOptions> options,
        IRubricPipelineTelemetry telemetry,
        ILogger<FastClinicalRetrievalOrchestrator> logger)
    {
        _keywordDiscovery = keywordDiscovery;
        _aliasEngine = aliasEngine;
        _embeddingSearch = embeddingSearch;
        _rubricCatalog = rubricCatalog;
        _doctorLearning = doctorLearning;
        _intelligenceRepository = intelligenceRepository;
        _options = options.Value;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<FastClinicalRetrievalResult> DiscoverAsync(
        Guid sessionId,
        string? correlationId,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        AudioCaseSummaryModel? summary,
        string? detectedLanguage,
        Func<CancellationToken, Task<List<AudioCaseSuggestedRubricModel>>> v1Discovery,
        CancellationToken cancellationToken = default,
        FastClinicalRetrievalContext? context = null)
    {
        var sw = Stopwatch.StartNew();
        var startUtc = DateTime.UtcNow;
        var engineVersion = string.IsNullOrWhiteSpace(_options.FastPipelineEngineVersion)
            ? "fast-f"
            : _options.FastPipelineEngineVersion.Trim();
        if (engineVersion.Length > 10)
            engineVersion = engineVersion[..10];

        var result = new FastClinicalRetrievalResult { EngineVersion = engineVersion };

        try
        {
            var filteredSymptoms = FastClinicalEvidenceGate.FilterSpeakerNegatedSymptoms(
                symptoms, context?.Messages);
            result.StagesCompleted.Add("SpeakerNegationFilter");

            var concepts = ConceptGraphConceptMapper.FromSymptoms(filteredSymptoms, summary);
            concepts = FastClinicalEvidenceGate.FilterSpeakerNegatedConcepts(concepts, context?.Messages);

            if (_options.FastPipelineEnableMultiQueryBlocks)
            {
                concepts = FastClinicalSymptomBlockBuilder.ExpandMultiQuery(
                    concepts,
                    _options.FastPipelineMaxExtraQueriesPerConcept);
                result.StagesCompleted.Add("MultiQueryExpand");
            }

            result.Concepts = concepts;
            result.StagesCompleted.Add("SymptomConceptBuild");
            await _intelligenceRepository.SaveConceptsAsync(sessionId, concepts, cancellationToken);

            var embTimeoutSec = Math.Clamp(_options.FastPipelineEmbeddingTimeoutSeconds, 1, 30);

            var catalogTask = SafeCatalogAsync(concepts, cancellationToken);
            var v1Task = v1Discovery(cancellationToken);
            var keywordTask = _keywordDiscovery.DiscoverAsync(
                sessionId, correlationId, concepts, Array.Empty<AudioCaseSuggestedRubricModel>(), cancellationToken);
            var aliasTask = SafeAliasAsync(concepts, filteredSymptoms, detectedLanguage, cancellationToken);
            var embeddingTask = SafeEmbeddingAsync(concepts, embTimeoutSec, cancellationToken);

            await Task.WhenAll(v1Task, keywordTask, aliasTask, embeddingTask, catalogTask);

            var v1Rubrics = await v1Task;
            var keywordBatch = await keywordTask;
            var aliasRubrics = await aliasTask;
            var embeddingRubrics = await embeddingTask;
            var catalogRubrics = await catalogTask;

            result.V1RubricCount = v1Rubrics.Count;
            result.KeywordRubricCount = keywordBatch.Rubrics.Count;
            result.AliasRubricCount = aliasRubrics.Count;
            result.EmbeddingRubricCount = embeddingRubrics.Count;
            result.CatalogRubricCount = catalogRubrics.Count;
            result.KeywordTraces = keywordBatch.Traces;
            result.StagesCompleted.Add("ParallelExactFtsAliasEmbeddingCatalog");

            _telemetry.IncrementSqlQueries(Math.Max(1, keywordBatch.Traces.Count));
            if (embeddingRubrics.Count > 0)
                _telemetry.IncrementEmbeddingCalls();

            var maxResults = Math.Clamp(_options.FastPipelineMaxFinalRubrics, 5, 20);
            var funnelCap = Math.Clamp(_options.FastPipelineCandidateFunnelSize, 40, 200);

            var merged = RubricResultMerger.Merge(v1Rubrics, keywordBatch.Rubrics, maxResults: funnelCap);
            merged = RubricResultMerger.Merge(merged, aliasRubrics, maxResults: funnelCap);
            merged = RubricResultMerger.Merge(merged, embeddingRubrics, maxResults: funnelCap);
            merged = RubricResultMerger.Merge(merged, catalogRubrics, maxResults: funnelCap);

            merged = merged.Where(r => r.SubSectionId > 0).ToList();
            merged = RubricCandidateQualityGate.Apply(merged, concepts);
            merged = FastClinicalEvidenceGate.ApplyGenderGate(merged, context?.Patient);
            merged = FastClinicalEvidenceGate.ApplyHierarchySpecificityGate(merged, concepts);
            merged = FastClinicalEvidenceGate.ApplyHallucinationHardGate(
                merged, concepts, _options.FastPipelineMinEvidenceScore);
            result.StagesCompleted.Add("AccuracyGates");

            merged = FastClinicalRanking.ApplyCanonicalScores(merged, concepts);
            foreach (var r in merged)
                r.CanonicalScore = r.ConfidenceScore;

            DoctorLearningWeightsSnapshot? learned = null;
            if (_options.EnableDoctorLearningEngine && _options.FastPipelineEnableDoctorLearning)
            {
                try
                {
                    learned = await _doctorLearning.LoadAsync(cancellationToken);
                    merged = FastClinicalRanking.ApplyDoctorLearningBoost(merged, concepts, learned, _options);
                    result.StagesCompleted.Add("DoctorLearningBoost");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fast path doctor learning load failed; continuing without boost.");
                }
            }

            merged = FastClinicalRanking.SelectWithMmr(
                merged,
                targetCount: maxResults,
                lambda: _options.FastPipelineMmrLambda,
                minCanonicalScore: _options.FastPipelineMinCanonicalScore);

            FastClinicalEvidenceGate.AttachEvidenceContract(merged, concepts);
            result.StagesCompleted.Add("EvidenceContract");

            result.Rubrics = merged;
            result.Success = merged.Count > 0 || concepts.Count > 0;
            result.StagesCompleted.Add("CanonicalScoreMmr");

            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            var candidateTotal = result.V1RubricCount + result.KeywordRubricCount
                + result.AliasRubricCount + result.EmbeddingRubricCount + result.CatalogRubricCount;
            _telemetry.SetCandidateCount(candidateTotal);
            _telemetry.SetFinalRubricCounts(merged.Count, merged.Count(r => r.SubSectionId > 0), 0);

            var msg =
                $"v1={result.V1RubricCount}; kw={result.KeywordRubricCount}; alias={result.AliasRubricCount}; " +
                $"emb={result.EmbeddingRubricCount}; catalog={result.CatalogRubricCount}; " +
                $"concepts={concepts.Count}; final={merged.Count}";

            await _telemetry.RecordStageAsync(
                "FastClinicalRetrieval",
                result.LatencyMs,
                startUtc,
                DateTime.UtcNow,
                status: result.Success ? "Success" : "Empty",
                message: msg,
                candidateCount: candidateTotal,
                finalRubricCount: merged.Count,
                cancellationToken: cancellationToken);

            await _intelligenceRepository.SaveIntelligenceLogAsync(
                sessionId,
                correlationId,
                "FastClinicalRetrieval",
                result.Success ? "Success" : "Empty",
                $"engine={engineVersion}; latencyMs={result.LatencyMs}; {msg}",
                detailsJson: null,
                result.LatencyMs,
                cancellationToken,
                engineVersion);

            _logger.LogInformation(
                "FastClinicalRetrieval session {SessionId}: {Message} latencyMs={Ms}",
                sessionId, msg, result.LatencyMs);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.Success = false;
            result.Error = ex.Message;
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            _logger.LogError(ex, "FastClinicalRetrieval failed for session {SessionId}", sessionId);
            await _telemetry.RecordStageAsync(
                "FastClinicalRetrieval",
                result.LatencyMs,
                startUtc,
                DateTime.UtcNow,
                status: "Failure",
                message: "fast_path_failed",
                cancellationToken: cancellationToken);
            return result;
        }
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> SafeCatalogAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken)
    {
        if (!_options.FastPipelineEnableCatalogLookup)
            return new List<AudioCaseSuggestedRubricModel>();

        try
        {
            await _rubricCatalog.EnsureBuiltAsync(cancellationToken);
            var tokens = concepts
                .SelectMany(c => (c.SearchTerms ?? new List<string>()).Append(c.RawStatement ?? ""))
                .SelectMany(t => (t ?? "").Split(new[] { ' ', '-', ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Where(t => t.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(40)
                .ToList();

            return _rubricCatalog.LookupByTokens(tokens, maxResults: 40).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fast path catalog lookup failed; continuing without catalog.");
            return new List<AudioCaseSuggestedRubricModel>();
        }
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> SafeAliasAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<AudioCaseSymptomModel> symptoms,
        string? language,
        CancellationToken cancellationToken)
    {
        try
        {
            var alias = await _aliasEngine.SearchRubricsAsync(concepts, symptoms, language, cancellationToken);
            return alias.Rubrics ?? new List<AudioCaseSuggestedRubricModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fast path alias search failed; continuing without aliases.");
            return new List<AudioCaseSuggestedRubricModel>();
        }
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> SafeEmbeddingAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableEmbeddingSearch || !_options.FastPipelineEnableEmbeddingSearch)
            return new List<AudioCaseSuggestedRubricModel>();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var search = await _embeddingSearch.SearchAsync(concepts, timeoutCts.Token);
            if (search.Candidates.Count == 0)
                return new List<AudioCaseSuggestedRubricModel>();

            return search.Candidates
                .Where(c => c.SubSectionId > 0)
                .Select(c => new AudioCaseSuggestedRubricModel
                {
                    SubSectionId = c.SubSectionId,
                    SubSectionName = c.SubSectionName,
                    MatchScore = Math.Round(c.CosineScore * 100m, 1),
                    ConfidenceScore = c.CosineScore,
                    SuggestedIntensityNo = 2,
                    MatchedFrom = c.MatchedConceptText,
                    PatientEvidence = c.MatchedConceptText,
                    MatchSource = "Embedding",
                    DiscoveryMethod = "Embedding",
                    EvidenceType = "Semantic",
                    IsDbBacked = true,
                    WhySuggested = $"Embedding cosine={c.CosineScore:F3}",
                    EngineVersion = _options.FastPipelineEngineVersion,
                    RequiresManualApproval = true,
                    IsAiSuggested = false,
                })
                .ToList();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Fast path embedding search timed out after {Sec}s; continuing without embeddings.", timeoutSeconds);
            return new List<AudioCaseSuggestedRubricModel>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fast path embedding search failed; continuing without embeddings.");
            return new List<AudioCaseSuggestedRubricModel>();
        }
    }
}
