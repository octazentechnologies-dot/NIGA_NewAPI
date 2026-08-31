using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services;
using Niga_Domain.Services.AudioCaseIntelligence.Enterprise;
using Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Niga_Domain.Services.AudioCaseIntelligence.Validation;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;
using Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence;
using Niga_Domain.Services.AudioCaseIntelligence.V6;

namespace Niga_Domain.Services.AudioCaseIntelligence.V3.Orchestration;

public class ConceptGraphOrchestrator : IConceptGraphOrchestrator
{
    private readonly RubricIntelligenceOptions _options;
    private readonly IPatientMeaningGraphEngine _meaningEngine;
    private readonly ICaseDecompositionEngine _decompositionEngine;
    private readonly IMultiSymptomDiscoveryEngine _multiSymptomEngine;
    private readonly ICategoryDiscoveryEngine _categoryEngine;
    private readonly IMetaphorUnderstandingEngine _metaphorEngine;
    private readonly IClinicalConceptEngineV3 _clinicalEngine;
    private readonly IHomeopathicConceptEngineV3 _homeopathicEngine;
    private readonly IMultiConceptDiscoveryEngine _multiConceptDiscoveryEngine;
    private readonly IRecallExpansionEngine _recallEngine;
    private readonly IConceptClusterEngine _clusterEngine;
    private readonly IRubricDiscoveryEngineV3 _discoveryEngine;
    private readonly IRubricCandidateEngine _rubricCandidateEngine;
    private readonly IEnterpriseRubricDiscoveryEngine _enterpriseDiscoveryEngine;
    private readonly IHierarchicalRepertorySearchEngine _repertorySearchEngine;
    private readonly IRepertoryMappingRepository _repertoryMappingRepository;
    private readonly IEnterpriseHybridCompletionEngine _hybridCompletionEngine;
    private readonly IV6ClinicalReasoningEngine _v6ClinicalReasoningEngine;
    private readonly IRepertoryIntelligenceOrchestrator _repertoryIntelligenceOrchestrator;
    private readonly IPipelineDiagnosticService _pipelineDiagnostics;
    private readonly IConceptGraphEvidenceEngine _evidenceEngine;
    private readonly IRubricEnterpriseEvidenceChainEnricher _evidenceChainEnricher;
    private readonly IKnowledgeGraphOrchestratorBridge _knowledgeGraphBridge;
    private readonly IClinicalValidationEngine _validationEngine;
    private readonly IPrimarySymptomEngine _primarySymptomEngine;
    private readonly ITranscriptCoverageEngine _coverageEngine;
    private readonly IMissingSymptomDetector _missingDetector;
    private readonly ICaseCompletenessScoringEngine _completenessEngine;
    private readonly IConceptGraphRepository _repository;
    private readonly IAudioCaseSessionProgressReporter _progress;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRubricPipelineTelemetry _pipelineTelemetry;
    private readonly ILogger<ConceptGraphOrchestrator> _logger;

    public ConceptGraphOrchestrator(
        IOptions<RubricIntelligenceOptions> options,
        IPatientMeaningGraphEngine meaningEngine,
        ICaseDecompositionEngine decompositionEngine,
        IMultiSymptomDiscoveryEngine multiSymptomEngine,
        ICategoryDiscoveryEngine categoryEngine,
        IMetaphorUnderstandingEngine metaphorEngine,
        IClinicalConceptEngineV3 clinicalEngine,
        IHomeopathicConceptEngineV3 homeopathicEngine,
        IMultiConceptDiscoveryEngine multiConceptDiscoveryEngine,
        IRecallExpansionEngine recallEngine,
        IConceptClusterEngine clusterEngine,
        IRubricDiscoveryEngineV3 discoveryEngine,
        IRubricCandidateEngine rubricCandidateEngine,
        IEnterpriseRubricDiscoveryEngine enterpriseDiscoveryEngine,
        IHierarchicalRepertorySearchEngine repertorySearchEngine,
        IRepertoryMappingRepository repertoryMappingRepository,
        IEnterpriseHybridCompletionEngine hybridCompletionEngine,
        IV6ClinicalReasoningEngine v6ClinicalReasoningEngine,
        IRepertoryIntelligenceOrchestrator repertoryIntelligenceOrchestrator,
        IPipelineDiagnosticService pipelineDiagnostics,
        IConceptGraphEvidenceEngine evidenceEngine,
        IRubricEnterpriseEvidenceChainEnricher evidenceChainEnricher,
        IKnowledgeGraphOrchestratorBridge knowledgeGraphBridge,
        IClinicalValidationEngine validationEngine,
        IPrimarySymptomEngine primarySymptomEngine,
        ITranscriptCoverageEngine coverageEngine,
        IMissingSymptomDetector missingDetector,
        ICaseCompletenessScoringEngine completenessEngine,
        IConceptGraphRepository repository,
        IAudioCaseSessionProgressReporter progress,
        IServiceScopeFactory scopeFactory,
        IRubricPipelineTelemetry pipelineTelemetry,
        ILogger<ConceptGraphOrchestrator> logger)
    {
        _options = options.Value;
        _meaningEngine = meaningEngine;
        _decompositionEngine = decompositionEngine;
        _multiSymptomEngine = multiSymptomEngine;
        _categoryEngine = categoryEngine;
        _metaphorEngine = metaphorEngine;
        _clinicalEngine = clinicalEngine;
        _homeopathicEngine = homeopathicEngine;
        _multiConceptDiscoveryEngine = multiConceptDiscoveryEngine;
        _recallEngine = recallEngine;
        _clusterEngine = clusterEngine;
        _discoveryEngine = discoveryEngine;
        _rubricCandidateEngine = rubricCandidateEngine;
        _enterpriseDiscoveryEngine = enterpriseDiscoveryEngine;
        _repertorySearchEngine = repertorySearchEngine;
        _repertoryMappingRepository = repertoryMappingRepository;
        _hybridCompletionEngine = hybridCompletionEngine;
        _v6ClinicalReasoningEngine = v6ClinicalReasoningEngine;
        _repertoryIntelligenceOrchestrator = repertoryIntelligenceOrchestrator;
        _pipelineDiagnostics = pipelineDiagnostics;
        _evidenceEngine = evidenceEngine;
        _evidenceChainEnricher = evidenceChainEnricher;
        _knowledgeGraphBridge = knowledgeGraphBridge;
        _validationEngine = validationEngine;
        _primarySymptomEngine = primarySymptomEngine;
        _coverageEngine = coverageEngine;
        _missingDetector = missingDetector;
        _completenessEngine = completenessEngine;
        _repository = repository;
        _progress = progress;
        _scopeFactory = scopeFactory;
        _pipelineTelemetry = pipelineTelemetry;
        _logger = logger;
    }

    public async Task<ConceptGraphPhaseResult> BuildMeaningGraphAsync(
        Guid sessionId,
        string transcript,
        string? detectedLanguage,
        CancellationToken cancellationToken = default)
    {
        var result = await AnalyzeAsync(sessionId, transcript, detectedLanguage, null, null, cancellationToken);
        return new ConceptGraphPhaseResult
        {
            Success = result.Graph.Meanings.Count > 0,
            Error = result.Error,
            EngineVersion = result.EngineVersion,
            StagesCompleted = result.StagesCompleted,
            MeaningGraph = result.Graph.Meanings.Count > 0
                ? new PatientMeaningGraphResult
                {
                    Success = true,
                    Meanings = result.Graph.Meanings,
                    ModelVersion = PatientMeaningGraphEngine.ModelId,
                }
                : null,
        };
    }

    public Task<ConceptGraphAnalysisResult> AnalyzeAsync(
        Guid sessionId,
        string transcript,
        string? detectedLanguage,
        PatientClinicalContext? patientContext,
        AudioCaseSummaryModel? summary,
        CancellationToken cancellationToken = default) =>
        AnalyzeAsync(sessionId, transcript, detectedLanguage, patientContext, summary, dualLanguage: null, cancellationToken);

    public async Task<ConceptGraphAnalysisResult> AnalyzeAsync(
        Guid sessionId,
        string transcript,
        string? detectedLanguage,
        PatientClinicalContext? patientContext,
        AudioCaseSummaryModel? summary,
        DualLanguageMeaningContext? dualLanguage,
        CancellationToken cancellationToken = default)
    {
        var stages = new List<string>();
        var graph = new ConceptGraphFullModel
        {
            SessionId = sessionId,
            EngineVersion = _options.EnableV7RepertoryIntelligenceEngine
                ? "v7.0"
                : _options.EnableV6ClinicalReasoningEngine
                ? "v6.0"
                : _options.EnableHybridCompletionEngine
                ? "v5.2"
                : _options.EnableEnterpriseRubricDiscoveryEngine
                ? EnterpriseRubricDiscoveryEngine.EngineVersionLabel
                : _options.EnableEnterpriseKnowledgeGraph
                ? "v11"
                : _options.EnableMultiConceptDiscovery
                ? "v5"
                : _options.EnableV35RecallEngine ? "v3.5" : "v3",
        };

        var fast = _options.EnableV35RecallEngine && _options.EnableV35FastPipeline;
        Task<List<SymptomBlockNodeModel>>? decompositionTask = null;

        if (_options.EnableV35RecallEngine)
        {
            if (fast)
            {
                decompositionTask = _decompositionEngine.DecomposeAsync(transcript, cancellationToken);
            }
            else
            {
                graph.SymptomBlocks = await _decompositionEngine.DecomposeAsync(transcript, cancellationToken);
                stages.Add("CaseDecomposition");
                await AuditAsync(sessionId, CaseDecompositionEngine.StageName, CaseDecompositionEngine.ModelId, true, null, null, cancellationToken);
            }
        }

        await ReportProgressAsync(sessionId, "PatientMeaningGraph", 86, cancellationToken);
        var meaningStart = DateTime.UtcNow;
        var meaningSw = System.Diagnostics.Stopwatch.StartNew();
        var meaningResult = await _meaningEngine.BuildAsync(transcript, detectedLanguage, dualLanguage, cancellationToken);
        meaningSw.Stop();
        await _pipelineTelemetry.RecordStageAsync(
            "ConceptExtraction_PatientMeaning",
            meaningSw.ElapsedMilliseconds,
            meaningStart,
            DateTime.UtcNow,
            status: meaningResult.Success ? "Success" : "Failure",
            candidateCount: meaningResult.Meanings?.Count,
            cancellationToken: cancellationToken);
        if (!fast)
            await AuditAsync(sessionId, PatientMeaningGraphEngine.StageName, PatientMeaningGraphEngine.ModelId, meaningResult.Success, meaningResult.Error, meaningResult.LatencyMs, cancellationToken);

        if (!meaningResult.Success)
            return Fail(meaningResult.Error, stages);

        if (decompositionTask != null)
        {
            graph.SymptomBlocks = await decompositionTask;
            stages.Add("CaseDecomposition");
        }

        var primaryMeanings = meaningResult.Meanings;
        List<PatientMeaningNodeModel> expandedMeanings = new();

        if (_options.EnableV35RecallEngine)
        {
            var skipMultiSymptom = fast
                && primaryMeanings.Count >= _options.MinPrimaryMeaningsToSkipMultiSymptom;

            if (!skipMultiSymptom)
            {
                expandedMeanings = await _multiSymptomEngine.ExpandAsync(primaryMeanings, cancellationToken);
                stages.Add("MultiSymptomDiscovery");
                if (!fast)
                    await AuditAsync(sessionId, MultiSymptomDiscoveryEngine.StageName, MultiSymptomDiscoveryEngine.ModelId, true, null, null, cancellationToken);
            }
        }

        graph.Meanings = _options.EnableV35RecallEngine
            ? ConceptGraphTierHelper.MergeMeanings(primaryMeanings, expandedMeanings, graph.SymptomBlocks)
            : primaryMeanings;
        stages.Add("PatientMeaningGraph");

        if (_options.EnableV35RecallEngine)
        {
            await ReportProgressAsync(sessionId, "CategoryDiscovery", 88, cancellationToken);
            await _categoryEngine.ApplyCategoriesAsync(graph.Meanings, cancellationToken);
            stages.Add("CategoryDiscovery");
        }

        await ReportProgressAsync(sessionId, "MetaphorUnderstanding", 89, cancellationToken);
        var metaphors = await _metaphorEngine.ResolveAsync(graph.Meanings, cancellationToken);
        graph.Metaphors = metaphors;
        stages.Add("MetaphorUnderstanding");

        if (_options.EnableMultiConceptDiscovery)
        {
            await ReportProgressAsync(sessionId, "MultiConceptDiscovery", 90, cancellationToken);
            var multiStart = DateTime.UtcNow;
            var multiSw = System.Diagnostics.Stopwatch.StartNew();
            var multi = await _multiConceptDiscoveryEngine.DiscoverAsync(
                graph.Meanings, graph.Metaphors, graph.SymptomBlocks, transcript, summary, cancellationToken);
            multiSw.Stop();
            graph.ClinicalConcepts = multi.ClinicalConcepts;
            graph.HomeopathicConcepts = multi.HomeopathicConcepts;
            graph.ConceptGraphEdges = multi.Edges;
            graph.PrimaryConcepts = multi.PrimaryConcepts;
            graph.SecondaryConcepts = multi.SecondaryConcepts;
            graph.SupportingConcepts = multi.SupportingConcepts;
            stages.Add("MultiConceptDiscovery");
            graph.Clusters = _clusterEngine.BuildClusters(graph.HomeopathicConcepts);
            stages.Add("ConceptClustering");
            await _pipelineTelemetry.RecordStageAsync(
                "ConceptExtraction_MultiConcept",
                multiSw.ElapsedMilliseconds,
                multiStart,
                DateTime.UtcNow,
                candidateCount: graph.HomeopathicConcepts.Count,
                cancellationToken: cancellationToken);
        }
        else
        {
            await ReportProgressAsync(sessionId, "ClinicalConcept", 90, cancellationToken);
            var clinical = await _clinicalEngine.DeriveAsync(graph.Meanings, graph.Metaphors, cancellationToken);
            graph.ClinicalConcepts = clinical;
            stages.Add("ClinicalConcept");

            var homeopathic = await _homeopathicEngine.MapAsync(graph.ClinicalConcepts, summary, cancellationToken);
            graph.HomeopathicConcepts = homeopathic;
            stages.Add("HomeopathicConcept");

            if (_options.EnableV35RecallEngine)
            {
                await ReportProgressAsync(sessionId, "RecallExpansion", 91, cancellationToken);
                graph.HomeopathicConcepts = await _recallEngine.ExpandAsync(
                    graph.ClinicalConcepts, graph.HomeopathicConcepts, transcript, summary, cancellationToken);
                stages.Add("RecallExpansion");
                graph.Clusters = _clusterEngine.BuildClusters(graph.HomeopathicConcepts);
                stages.Add("ConceptClustering");
            }
        }

        await ReportProgressAsync(sessionId, "RubricDiscovery", 93, cancellationToken);
        List<RubricDiscoveryNodeModel> discoveries;
        RubricCandidateEngineResult? candidateResult = null;
        EnterpriseRubricDiscoveryResult? enterpriseDiscovery = null;
        List<HybridConceptAuditEntry>? hybridAuditEntries = null;
        HybridCompletionRequest? hybridRequest = null;
        V6ClinicalReasoningRequest? v6Request = null;
        List<V6SymptomDiscoveryAudit>? v6AuditTrail = null;
        V7RepertoryIntelligenceRequest? v7Request = null;
        List<V7SymptomSearchAudit>? v7AuditTrail = null;
        List<V7ExtractedSymptom>? v7ExtractedSymptoms = null;

        if (_options.EnableEnterpriseRubricDiscoveryEngine)
        {
            var fastAudioDiscovery = fast;
            var entStart = DateTime.UtcNow;
            var entSw = System.Diagnostics.Stopwatch.StartNew();
            enterpriseDiscovery = await _enterpriseDiscoveryEngine.DiscoverCandidatesAsync(
                new EnterpriseRubricDiscoveryRequest
                {
                    SessionId = sessionId,
                    Graph = graph,
                    Transcript = transcript,
                    MaxCandidates = _options.MaxEnterpriseDiscoveryCandidates,
                    IncludeKnowledgeGraph = _options.EnableEnterpriseKnowledgeGraph && !fastAudioDiscovery,
                    IncludeEnterpriseEmbeddings = _options.EnableRubricCandidateEngine,
                    // StrictConceptGatedDiscovery requires every concept searched — never skip legacy bootstrap in fast mode.
                    IncludeLegacyDiscovery = !_options.EnableV35FastPipeline
                        || _options.StrictConceptGatedDiscovery
                        || !_options.EnableV35RecallEngine,
                    IncludeRuleExpansion = _options.EnableEnterpriseRubricExpansion && !fastAudioDiscovery,
                },
                cancellationToken);
            entSw.Stop();

            candidateResult = enterpriseDiscovery.EmbeddingEngineResult;
            discoveries = enterpriseDiscovery.Discoveries;
            graph.RubricCandidates = enterpriseDiscovery.RubricCandidates;
            if (enterpriseDiscovery.KnowledgeGraphResult?.Paths.Count > 0)
                graph.KnowledgeGraphPaths = enterpriseDiscovery.KnowledgeGraphResult.Paths;

            stages.Add(enterpriseDiscovery.Success
                ? "EnterpriseRubricDiscovery"
                : "EnterpriseRubricDiscoveryEmpty");

            await _pipelineTelemetry.RecordStageAsync(
                "EnterpriseRubricDiscovery",
                entSw.ElapsedMilliseconds,
                entStart,
                DateTime.UtcNow,
                status: enterpriseDiscovery.Success ? "Success" : "Empty",
                candidateCount: discoveries.Count,
                cancellationToken: cancellationToken);

            await _pipelineDiagnostics.PersistAsync(sessionId, enterpriseDiscovery.Diagnostics, cancellationToken);

            if (!enterpriseDiscovery.Success)
            {
                _logger.LogWarning(
                    "Enterprise rubric discovery returned zero candidates for session {SessionId}: {Reason}",
                    sessionId,
                    enterpriseDiscovery.Error ?? enterpriseDiscovery.Diagnostics.FailureReason);
            }
        }
        else if (_options.EnableRubricCandidateEngine)
        {
            candidateResult = await _rubricCandidateEngine.DiscoverFromGraphAsync(graph, null, cancellationToken);
            if (candidateResult.Success && candidateResult.Discoveries.Count > 0)
            {
                discoveries = candidateResult.Discoveries;
                graph.RubricCandidates = candidateResult.Candidates;
                stages.Add("RubricCandidateEngine");
            }
            else
            {
                discoveries = await _discoveryEngine.DiscoverAsync(graph.HomeopathicConcepts, cancellationToken);
                discoveries = _evidenceEngine.BuildEvidenceChains(discoveries, graph);
                stages.Add("RubricDiscovery");
            }
        }
        else
        {
            discoveries = await _discoveryEngine.DiscoverAsync(graph.HomeopathicConcepts, cancellationToken);
            discoveries = _evidenceEngine.BuildEvidenceChains(discoveries, graph);
            stages.Add("RubricDiscovery");
        }

        hybridRequest = new HybridCompletionRequest
        {
            SessionId = sessionId,
            Graph = graph,
            Transcript = transcript,
            GlobalDiscoveries = discoveries,
            EmbeddingCandidates = graph.RubricCandidates.Count > 0
                ? graph.RubricCandidates
                : candidateResult?.Candidates ?? new List<RubricCandidateModel>(),
            KnowledgeGraphPaths = graph.KnowledgeGraphPaths,
        };

        v6Request = new V6ClinicalReasoningRequest
        {
            SessionId = sessionId,
            Graph = graph,
            Transcript = transcript,
            GlobalDiscoveries = discoveries,
            EmbeddingCandidates = hybridRequest.EmbeddingCandidates,
            KnowledgeGraphPaths = graph.KnowledgeGraphPaths,
        };

        v7Request = new V7RepertoryIntelligenceRequest
        {
            SessionId = sessionId,
            Graph = graph,
            Transcript = transcript,
            GlobalDiscoveries = discoveries,
            EmbeddingCandidates = hybridRequest.EmbeddingCandidates,
            KnowledgeGraphPaths = graph.KnowledgeGraphPaths,
        };

        if (_options.EnableV7RepertoryIntelligenceEngine && _options.EnableEnterpriseClinicalValidation)
        {
            var v7Start = DateTime.UtcNow;
            var v7Sw = System.Diagnostics.Stopwatch.StartNew();
            var v7Result = await _repertoryIntelligenceOrchestrator.DiscoverAsync(v7Request, cancellationToken);
            v7Sw.Stop();
            v7AuditTrail = v7Result.AuditTrail;
            v7ExtractedSymptoms = v7Result.ExtractedSymptoms;
            discoveries = _evidenceEngine.BuildEvidenceChains(v7Result.Discoveries, graph);
            v7Request.GlobalDiscoveries = discoveries;
            hybridRequest.GlobalDiscoveries = discoveries;
            v6Request.GlobalDiscoveries = discoveries;
            stages.Add(v7Result.Success ? "V7RepertoryIntelligence" : "V7RepertoryIntelligenceEmpty");
            stages.AddRange(v7Result.StagesCompleted);
            await _pipelineTelemetry.RecordStageAsync(
                "V7RepertoryIntelligence",
                v7Sw.ElapsedMilliseconds,
                v7Start,
                DateTime.UtcNow,
                status: v7Result.Success ? "Success" : "Empty",
                candidateCount: discoveries.Count,
                cancellationToken: cancellationToken);
        }
        else if (_options.EnableV6ClinicalReasoningEngine && _options.EnableEnterpriseClinicalValidation)
        {
            var v6Result = await _v6ClinicalReasoningEngine.DiscoverAsync(v6Request, cancellationToken);
            v6AuditTrail = v6Result.AuditTrail;
            discoveries = _evidenceEngine.BuildEvidenceChains(v6Result.Discoveries, graph);
            v6Request.GlobalDiscoveries = discoveries;
            hybridRequest.GlobalDiscoveries = discoveries;
            stages.Add(v6Result.Success ? "V6ClinicalReasoning" : "V6ClinicalReasoningEmpty");
            stages.AddRange(v6Result.StagesCompleted);
        }
        else if (_options.EnableHybridCompletionEngine && _options.EnableEnterpriseClinicalValidation)
        {
            var hybridDiscovery = await _hybridCompletionEngine.DiscoverPerConceptAsync(
                hybridRequest, cancellationToken);
            hybridAuditEntries = hybridDiscovery.AuditEntries;
            discoveries = _evidenceEngine.BuildEvidenceChains(hybridDiscovery.Discoveries, graph);
            hybridRequest.GlobalDiscoveries = discoveries;
            stages.Add("HybridCompletionV52");
        }
        else
        {
            discoveries = await MergeRepertoryAuthoritativeDiscoveriesAsync(
                discoveries, graph, transcript, stages, cancellationToken);
        }

        // V7/V6 symptom pipelines do not iterate homeopathic concepts — supplement with per-concept search
        // so StrictConceptGatedDiscovery threshold reflects full concept coverage, not a partial pass.
        var ensureStart = DateTime.UtcNow;
        var ensureSw = System.Diagnostics.Stopwatch.StartNew();
        (discoveries, hybridAuditEntries) = await EnsureAllConceptsDiscoveredAsync(
            sessionId,
            discoveries,
            graph,
            transcript,
            hybridRequest,
            hybridAuditEntries,
            stages,
            cancellationToken);
        ensureSw.Stop();
        await _pipelineTelemetry.RecordStageAsync(
            "EnsureAllConceptsDiscovered",
            ensureSw.ElapsedMilliseconds,
            ensureStart,
            DateTime.UtcNow,
            candidateCount: discoveries.Count,
            cancellationToken: cancellationToken);

        if (_options.EnableEnterpriseKnowledgeGraph && !_options.EnableEnterpriseRubricDiscoveryEngine)
        {
            var kgResult = await _knowledgeGraphBridge.DiscoverAsync(
                sessionId, graph, transcript, candidateResult, cancellationToken);

            if (kgResult.Success && kgResult.Discoveries.Count > 0)
            {
                if (_options.EnableKnowledgeGraphShadowMode)
                {
                    stages.Add("KnowledgeGraphShadow");
                }
                else
                {
                    discoveries = await MergeRepertoryAuthoritativeDiscoveriesAsync(
                        kgResult.Discoveries, graph, transcript, stages, cancellationToken);
                    stages.Add("KnowledgeGraphPrimary");
                }

                graph.KnowledgeGraphPaths = kgResult.Paths;
            }
            else
            {
                stages.Add("KnowledgeGraphFallback");
            }
        }

        var primarySymptom = _primarySymptomEngine.Resolve(summary, ToLegacyConcepts(graph), BuildSymptoms(graph));
        stages.Add("PrimarySymptom");

        await ReportProgressAsync(sessionId, "ClinicalValidationV21", 95, cancellationToken);
        var validationStart = DateTime.UtcNow;
        var validationSw = System.Diagnostics.Stopwatch.StartNew();
        var (acceptedRubrics, validationReports) = await ValidateDiscoveriesAsync(
            discoveries, graph, transcript, patientContext, summary, primarySymptom, stages,
            hybridRequest, hybridAuditEntries, v6Request, v6AuditTrail, v7Request, v7AuditTrail, v7ExtractedSymptoms, cancellationToken);
        validationSw.Stop();
        await _pipelineTelemetry.RecordStageAsync(
            "ClinicalValidation",
            validationSw.ElapsedMilliseconds,
            validationStart,
            DateTime.UtcNow,
            candidateCount: discoveries.Count,
            finalRubricCount: acceptedRubrics.Count,
            message: $"rejectedApprox={Math.Max(0, discoveries.Count - acceptedRubrics.Count)}",
            cancellationToken: cancellationToken);

        CaseCoverageMetricsModel? coverage = null;
        if (_options.EnableV35RecallEngine)
        {
            coverage = _coverageEngine.Measure(graph.SymptomBlocks, acceptedRubrics, graph.Meanings);
            stages.Add("TranscriptCoverage");

            var skipSecondPass = fast && acceptedRubrics.Count >= _options.MinRubricsToSkipSecondPass;

            if (!skipSecondPass
                && coverage.TranscriptCoverage < _options.MinTranscriptCoverage
                && coverage.UncoveredSpans.Count > 0)
            {
                var secondPass = await _missingDetector.RunSecondPassAsync(graph, transcript, coverage, cancellationToken);
                if (secondPass.AdditionalDiscoveries.Count > 0)
                {
                    var (additionalAccepted, additionalReports) = await ValidateDiscoveriesAsync(
                        secondPass.AdditionalDiscoveries, graph, transcript, patientContext, summary, primarySymptom, stages,
                        hybridRequest, hybridAuditEntries, v6Request, v6AuditTrail, v7Request, v7AuditTrail, v7ExtractedSymptoms, cancellationToken);
                    acceptedRubrics = MergeRubrics(acceptedRubrics, additionalAccepted);
                    validationReports.AddRange(additionalReports);
                    stages.Add("MissingSymptomDetector");
                    coverage = _coverageEngine.Measure(graph.SymptomBlocks, acceptedRubrics, graph.Meanings);
                }
            }

            var completeness = _completenessEngine.Score(graph.SymptomBlocks, acceptedRubrics, coverage);
            coverage.CaseCompleteness = completeness;
            graph.CoverageMetrics = coverage;
            stages.Add("CaseCompletenessScore");

            await _repository.SaveCoverageMetricsAsync(sessionId, coverage, acceptedRubrics, cancellationToken);
        }

        graph.Discoveries = discoveries
            .Where(d => acceptedRubrics.Any(r => r.SubSectionId == d.SubSectionId))
            .ToList();

        var tiered = candidateResult?.Success == true && candidateResult.Candidates.Count > 0
            ? BuildTieredOutputFromCandidates(candidateResult, graph, acceptedRubrics)
            : BuildTieredOutput(acceptedRubrics);

        if (candidateResult?.Success == true
            && tiered.Tier1.Count + tiered.Tier2.Count + tiered.Tier3.Count == 0
            && acceptedRubrics.Count > 0)
        {
            tiered = BuildTieredOutput(acceptedRubrics);
        }

        var allRubrics = tiered.Tier1
            .Concat(tiered.Tier2)
            .Concat(tiered.Tier3)
            .Concat(tiered.Review)
            .ToList();

        if (_options.EnableEnterpriseRubricEvidenceChain)
        {
            await _evidenceChainEnricher.EnrichRubricsAsync(
                allRubrics,
                new RubricEvidenceChainEnrichmentContext
                {
                    SessionId = sessionId,
                    Transcript = transcript,
                    Graph = graph,
                    Candidates = graph.RubricCandidates,
                    ValidationReports = validationReports,
                    Concepts = ToLegacyConcepts(graph),
                },
                cancellationToken);
            stages.Add("RubricEvidenceChain");

            graph.EnterpriseEvidenceChains = allRubrics
                .Where(r => r.EnterpriseEvidenceChain != null)
                .Select(r => r.EnterpriseEvidenceChain!)
                .ToList();

            foreach (var discovery in graph.Discoveries)
            {
                var rubric = allRubrics.FirstOrDefault(r => r.SubSectionId == discovery.SubSectionId);
                if (rubric?.EnterpriseEvidenceChain != null)
                    discovery.EnterpriseEvidenceChain = rubric.EnterpriseEvidenceChain;
            }
        }

        if (_options.EnableEnterpriseKnowledgeGraph)
        {
            await _knowledgeGraphBridge.ProjectSessionAsync(sessionId, graph, allRubrics, cancellationToken);
            stages.Add("KnowledgeGraphProject");
        }

        await ReportProgressAsync(sessionId, "FinalizingResults", 98, cancellationToken);
        await PersistGraphAsync(sessionId, graph, cancellationToken);

        _logger.LogInformation(
            "V3.5 pipeline session {SessionId}: meanings={M}, homeopathic={H}, discoveries={D}, accepted={A}, T1={T1}, T2={T2}, T3={T3}, review={Rev}, rejected={Rej}, coverage={Cov:P0}, completeness={Comp:P0}",
            sessionId, graph.Meanings.Count, graph.HomeopathicConcepts.Count, discoveries.Count, allRubrics.Count,
            tiered.Tier1.Count, tiered.Tier2.Count, tiered.Tier3.Count, tiered.Review.Count,
            discoveries.Count - graph.Discoveries.Count,
            coverage?.TranscriptCoverage ?? 0, coverage?.CaseCompleteness ?? 0);

        return new ConceptGraphAnalysisResult
        {
            Success = allRubrics.Count > 0 || graph.Meanings.Count > 0,
            EngineVersion = graph.EngineVersion,
            StagesCompleted = stages,
            Graph = graph,
            Rubrics = allRubrics,
            Tier1Rubrics = tiered.Tier1,
            Tier2Rubrics = tiered.Tier2,
            Tier3Rubrics = tiered.Tier3,
            PrimarySymptom = primarySymptom,
            ValidationRejectedCount = discoveries.Count - graph.Discoveries.Count,
            TranscriptCoverageScore = coverage?.TranscriptCoverage,
            CaseCompletenessScore = coverage?.CaseCompleteness,
            CoverageMetrics = coverage,
            PrimaryConcepts = graph.PrimaryConcepts,
            SecondaryConcepts = graph.SecondaryConcepts,
            SupportingConcepts = graph.SupportingConcepts,
            ConceptGraphEdges = graph.ConceptGraphEdges,
            RubricCandidates = graph.RubricCandidates,
            Tier1RubricCandidates = candidateResult?.Tier1Rubrics ?? new List<RubricCandidateModel>(),
            Tier2RubricCandidates = candidateResult?.Tier2Rubrics ?? new List<RubricCandidateModel>(),
            Tier3RubricCandidates = candidateResult?.Tier3Rubrics ?? new List<RubricCandidateModel>(),
            ValidationReports = validationReports,
            EnterpriseEvidenceChains = graph.EnterpriseEvidenceChains,
        };
    }

    private async Task<(List<AudioCaseSuggestedRubricModel> Accepted, List<RubricEnterpriseValidationReport> Reports)> ValidateDiscoveriesAsync(
        IReadOnlyList<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph,
        string transcript,
        PatientClinicalContext? patientContext,
        AudioCaseSummaryModel? summary,
        PrimarySymptomModel primarySymptom,
        List<string> stages,
        HybridCompletionRequest? hybridRequest,
        IReadOnlyList<HybridConceptAuditEntry>? hybridAuditEntries,
        V6ClinicalReasoningRequest? v6Request,
        IReadOnlyList<V6SymptomDiscoveryAudit>? v6AuditTrail,
        V7RepertoryIntelligenceRequest? v7Request,
        IReadOnlyList<V7SymptomSearchAudit>? v7AuditTrail,
        IReadOnlyList<V7ExtractedSymptom>? v7ExtractedSymptoms,
        CancellationToken cancellationToken)
    {
        var incompleteExcluded = discoveries
            .Where(d => d.SubSectionId > 0 && !IsValidCandidateForRanking(d))
            .ToList();

        if (incompleteExcluded.Count > 0 && _options.EnforceEvidenceChainCompleteGate)
        {
            // Always persist — do not skip under EnableV35FastPipeline (Task 7 acceptance).
            await _repository.SaveReasoningAuditAsync(graph.SessionId, new AiReasoningAuditModel
            {
                PipelineStage = "EvidenceChainCompleteGate",
                ModelId = "v3-gate",
                Success = true,
                ErrorMessage = $"Excluded {incompleteExcluded.Count} discoveries without complete evidence chain before ranking.",
                RequestJson = System.Text.Json.JsonSerializer.Serialize(incompleteExcluded.Select(d => new
                {
                    d.SubSectionId,
                    d.SubSectionName,
                    d.Confidence,
                    EvidenceComplete = d.EvidenceChain?.IsComplete,
                }).Take(50)),
            }, cancellationToken);

            _logger.LogInformation(
                "EvidenceChainCompleteGate session {SessionId}: excluded={Count} before validation/ranking",
                graph.SessionId,
                incompleteExcluded.Count);
        }

        var candidateRubrics = discoveries
            .Where(d => IsValidCandidateForRanking(d))
            .Select(d => ToSuggestedRubric(d, graph))
            .ToList();

        var validation = _validationEngine.ValidateAndFilter(
            candidateRubrics,
            new ClinicalValidationContext
            {
                SessionId = graph.SessionId,
                Patient = patientContext,
                Transcript = transcript,
                Concepts = ToLegacyConcepts(graph),
                Symptoms = BuildSymptoms(graph),
                Summary = summary,
                PrimarySymptom = primarySymptom,
            });

        if (!stages.Contains("ClinicalValidationV21") && !stages.Contains("EnterpriseClinicalValidation"))
            stages.Add(_options.EnableEnterpriseClinicalValidation
                ? "EnterpriseClinicalValidation"
                : "ClinicalValidationV21");

        var accepted = validation.AcceptedRubrics;

        if (!_options.EnableEnterpriseClinicalValidation && _options.EnableV35RecallEngine)
        {
            accepted = accepted.Where(r =>
            {
                if (string.Equals(r.RubricTier, "Review", StringComparison.OrdinalIgnoreCase))
                    return true;

                var confidence = r.ConfidenceScore ?? r.MatchScore;
                var quality = r.QualityScore ?? 0;
                var minQuality = confidence >= 0.75m
                    ? _options.MinRubricQualityScore
                    : _options.MinRubricQualityScoreTier3;

                if (r.EvidenceChain == null)
                    return quality >= minQuality;

                var rubricTail = ExtractTail(r.SubSectionName);
                var evidenceText = string.Join(' ',
                    r.EvidenceChain.PatientStatements
                        .Concat(r.EvidenceChain.ClinicalMeanings)
                        .Where(x => !string.IsNullOrWhiteSpace(x)));

                var similarity = string.IsNullOrWhiteSpace(evidenceText)
                    ? 0m
                    : AudioCaseAiProcessor.ComputeTextSimilarity(rubricTail, evidenceText);

                var minSimilarity = confidence >= 0.75m
                    ? _options.MinEvidenceSimilarityForRubric
                    : _options.MinEvidenceSimilarityTier3;

                return similarity >= minSimilarity && quality >= minQuality;
            }).ToList();
        }

        if (!_options.EnableEnterpriseClinicalValidation)
        {
            accepted = RubricReviewFallbackHelper.ApplyIfEmpty(
                accepted,
                validation.RejectedRubrics,
                _options,
                _options.MaxRubricsTier1 + _options.MaxRubricsTier2 + _options.MaxRubricsTier3);

            if (accepted.Count == 0 && _options.EnableV3ReviewFallback)
            {
                accepted = discoveries
                    .Where(d => d.SubSectionId > 0 && !string.IsNullOrWhiteSpace(d.SubSectionName))
                    .OrderByDescending(d => d.Confidence)
                    .Take(15)
                    .Select(d => RubricReviewFallbackHelper.MarkReviewTier(ToSuggestedRubric(d, graph)))
                    .ToList();
            }
        }
        else if (_options.EnableEnterpriseRubricReviewFallback && !_options.EnableAiClinicalConceptSuggestions)
        {
            accepted = RubricReviewFallbackHelper.ApplyIfEmpty(
                accepted,
                validation.RejectedRubrics,
                _options,
                _options.MaxRubricsTier1 + _options.MaxRubricsTier2 + _options.MaxRubricsTier3);

            if (accepted.Count == 0 && discoveries.Count > 0)
            {
                accepted = discoveries
                    .Where(d => d.SubSectionId > 0 && !string.IsNullOrWhiteSpace(d.SubSectionName))
                    .OrderByDescending(d => d.Confidence)
                    .Take(_options.MaxRubricsTier1 + _options.MaxRubricsTier2 + _options.MaxRubricsTier3)
                    .Select(d => RubricReviewFallbackHelper.MarkReviewTier(ToSuggestedRubric(d, graph)))
                    .ToList();

                _logger.LogWarning(
                    "Enterprise review fallback surfaced {Count} rubrics after strict validation rejected all candidates.",
                    accepted.Count);
            }
        }

        if (_options.EnableEnterpriseClinicalValidation)
        {
            if (_options.EnableV7RepertoryIntelligenceEngine && v7Request != null)
            {
                var v7Output = _repertoryIntelligenceOrchestrator.FinalizeOutput(
                    accepted,
                    v7Request,
                    v7AuditTrail ?? Array.Empty<V7SymptomSearchAudit>(),
                    v7ExtractedSymptoms ?? Array.Empty<V7ExtractedSymptom>(),
                    graph);
                accepted = await EnrichRepertoryMappingAsync(v7Output.AllRubrics, cancellationToken);
                stages.Add("V7RepertoryIntelligenceFinalize");
            }
            else if (_options.EnableV6ClinicalReasoningEngine && v6Request != null)
            {
                var v6Output = _v6ClinicalReasoningEngine.FinalizeOutput(
                    accepted,
                    v6Request,
                    v6AuditTrail ?? Array.Empty<V6SymptomDiscoveryAudit>(),
                    graph);
                accepted = await EnrichRepertoryMappingAsync(v6Output.AllRubrics, cancellationToken);
                stages.Add("V6ClinicalReasoningFinalize");
            }
            else if (_options.EnableHybridCompletionEngine && hybridRequest != null)
            {
                var hybridOutput = _hybridCompletionEngine.FinalizeOutput(
                    accepted,
                    hybridRequest,
                    hybridAuditEntries ?? Array.Empty<HybridConceptAuditEntry>(),
                    graph);
                accepted = await EnrichRepertoryMappingAsync(hybridOutput.AllRubrics, cancellationToken);
                stages.Add("HybridCompletionFinalizeV52");
            }
            else
            {
                accepted = await ApplyEnterpriseQualityPipelineAsync(
                    accepted, discoveries, graph, cancellationToken);
            }
        }

        if (accepted.Count == 0 && discoveries.Count > 0)
        {
            _logger.LogWarning(
                "V3 rubric validation accepted zero of {CandidateCount} discoveries for session graph; review fallback also empty.",
                discoveries.Count);
        }
        else if (validation.RejectedRubrics.Count > 0 && accepted.Any(r =>
            string.Equals(r.ValidationStatus, "ReviewSuggested", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogInformation(
                "V3 review fallback surfaced {ReviewCount} rubrics after {RejectedCount} strict rejections.",
                accepted.Count(r => string.Equals(r.ValidationStatus, "ReviewSuggested", StringComparison.OrdinalIgnoreCase)),
                validation.RejectedRubrics.Count);
        }

        foreach (var acceptedRubric in accepted)
        {
            var discovery = discoveries.FirstOrDefault(d => d.SubSectionId == acceptedRubric.SubSectionId);
            if (discovery == null) continue;
            discovery.ValidationStatus = "Accepted";
            discovery.QualityScore = acceptedRubric.QualityScore;
        }

        foreach (var rejected in validation.RejectedRubrics)
        {
            var discovery = discoveries.FirstOrDefault(d => d.SubSectionId == rejected.SubSectionId);
            if (discovery == null) continue;
            discovery.ValidationStatus = "Rejected";
            discovery.QualityScore = rejected.QualityScore;

            var report = validation.ValidationReports.FirstOrDefault(r => r.SubSectionId == rejected.SubSectionId)
                ?? rejected.EnterpriseValidation;
            if (report != null)
            {
                discovery.ValidationFlagsJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    failedSteps = report.Steps.Where(s => !s.Passed).Select(s => s.StepName).ToList(),
                    issueCodes = report.Issues.Select(i => i.Code).Distinct().ToList(),
                });
            }
        }

        return (accepted, validation.ValidationReports);
    }

    private async Task<List<RubricDiscoveryNodeModel>> MergeRepertoryAuthoritativeDiscoveriesAsync(
        List<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph,
        string transcript,
        List<string> stages,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableEnterpriseClinicalValidation && !_options.EnableEnterpriseRubricDiscoveryEngine)
        {
            return _evidenceEngine.BuildEvidenceChains(discoveries, graph);
        }

        var repertoryMatches = await _repertorySearchEngine.DiscoverFromGraphAsync(graph, transcript, cancellationToken);
        if (!stages.Contains("HierarchicalRepertorySearch"))
        {
            stages.Add("HierarchicalRepertorySearch");
        }

        var merged = EnterpriseRubricDeduplicator.MergeRepertoryFirst(repertoryMatches, discoveries);
        merged = EnterpriseRubricDeduplicator.DeduplicateDiscoveries(merged);
        return _evidenceEngine.BuildEvidenceChains(merged, graph);
    }

    /// <summary>
    /// Ensures every extracted homeopathic concept receives a genuine discovery attempt before
    /// StrictConceptGatedDiscovery evaluates the fallback threshold. V7/V6 paths search by symptom,
    /// not by concept — this supplement closes that gap.
    /// </summary>
    private async Task<(List<RubricDiscoveryNodeModel> Discoveries, List<HybridConceptAuditEntry>? AuditEntries)>
        EnsureAllConceptsDiscoveredAsync(
        Guid sessionId,
        List<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph,
        string transcript,
        HybridCompletionRequest hybridRequest,
        List<HybridConceptAuditEntry>? hybridAuditEntries,
        List<string> stages,
        CancellationToken cancellationToken)
    {
        var conceptCount = graph.HomeopathicConcepts.Count(c => !string.IsNullOrWhiteSpace(c.ConceptName));
        if (conceptCount == 0)
            return (discoveries, hybridAuditEntries);

        List<HybridConceptAuditEntry> auditEntries;
        List<RubricDiscoveryNodeModel> perConceptDiscoveries;

        if (stages.Contains("HybridCompletionV52") && hybridAuditEntries != null)
        {
            auditEntries = hybridAuditEntries.ToList();
            perConceptDiscoveries = discoveries;
        }
        else if (_options.EnableHybridCompletionEngine)
        {
            hybridRequest.GlobalDiscoveries = discoveries;
            var hybridDiscovery = await _hybridCompletionEngine.DiscoverPerConceptAsync(
                hybridRequest, cancellationToken);
            auditEntries = hybridDiscovery.AuditEntries;
            hybridAuditEntries = auditEntries;
            perConceptDiscoveries = hybridDiscovery.Discoveries;
            if (!stages.Contains("HybridCompletionV52"))
                stages.Add("HybridCompletionV52AllConcepts");
        }
        else
        {
            var repertoryMatches = await _repertorySearchEngine.DiscoverFromGraphAsync(
                graph, transcript, cancellationToken);
            auditEntries = graph.HomeopathicConcepts
                .Where(c => !string.IsNullOrWhiteSpace(c.ConceptName))
                .Select(c => new HybridConceptAuditEntry
                {
                    HomeopathicConceptId = c.HomeopathicConceptId,
                    ConceptName = c.ConceptName,
                    ConceptConfidence = c.Confidence,
                    DatabaseMatchCount = repertoryMatches.Count(d => d.HomeopathicConceptId == c.HomeopathicConceptId),
                })
                .ToList();
            hybridAuditEntries = auditEntries;
            perConceptDiscoveries = repertoryMatches;
            if (!stages.Contains("HierarchicalRepertorySearch"))
                stages.Add("HierarchicalRepertorySearchAllConcepts");
        }

        var conceptsSearched = auditEntries.Count;
        var merged = EnterpriseRubricDeduplicator.MergeRepertoryFirst(perConceptDiscoveries, discoveries);
        merged = EnterpriseRubricDeduplicator.DeduplicateDiscoveries(merged);
        var withEvidence = _evidenceEngine.BuildEvidenceChains(merged, graph);

        await _repository.SaveReasoningAuditAsync(sessionId, new AiReasoningAuditModel
        {
            PipelineStage = "ConceptDiscoveryCoverage",
            ModelId = graph.EngineVersion ?? "v3",
            Success = true,
            ErrorMessage = $"Searched {conceptsSearched}/{conceptCount} homeopathic concept(s); " +
                           $"discoveries before={discoveries.Count}, after={withEvidence.Count}.",
            RequestJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                conceptsExtracted = conceptCount,
                conceptsSearched,
                discoveriesBefore = discoveries.Count,
                discoveriesAfter = withEvidence.Count,
                stage = stages.LastOrDefault(s => s.Contains("Hybrid") || s.Contains("Hierarchical")),
                perConcept = auditEntries.Select(a => new
                {
                    a.HomeopathicConceptId,
                    a.ConceptName,
                    a.ConceptConfidence,
                    a.DatabaseMatchCount,
                    a.EmbeddingMatchCount,
                    a.KnowledgeGraphMatchCount,
                    a.SlotsAllocated,
                    a.OutputCategory,
                    a.LatencyMs,
                    a.ValidationSummary,
                    stages = a.Stages.Select(s => new { s.StageName, s.MatchCount, subSectionIds = s.SubSectionIds.Take(5) }),
                }),
                totalCoverageMs = auditEntries.Sum(a => a.LatencyMs),
                maxConceptMs = auditEntries.Count == 0 ? 0 : auditEntries.Max(a => a.LatencyMs),
            }),
        }, cancellationToken);

        _logger.LogInformation(
            "Concept discovery coverage session {SessionId}: extracted={Extracted}, searched={Searched}, discoveries={Discoveries}",
            sessionId,
            conceptCount,
            conceptsSearched,
            withEvidence.Count);

        return (withEvidence, hybridAuditEntries);
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> EnrichRepertoryMappingAsync(
        List<AudioCaseSuggestedRubricModel> rubrics,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableRepertoryMapping)
        {
            return rubrics;
        }

        var subSectionIds = rubrics
            .Where(r => r.SubSectionId > 0)
            .Select(r => r.SubSectionId)
            .Distinct()
            .ToList();

        if (subSectionIds.Count == 0)
        {
            return rubrics;
        }

        var maps = await _repertoryMappingRepository.GetMapsForSubSectionsAsync(subSectionIds, cancellationToken);
        foreach (var rubric in rubrics.Where(r => r.SubSectionId > 0))
        {
            if (!maps.TryGetValue(rubric.SubSectionId, out var mapList) || mapList.Count == 0)
            {
                continue;
            }

            var primary = mapList.OrderBy(m => m.PriorityOrder).First();
            rubric.RepertoryPath = primary.SourceRubricPath ?? rubric.RepertoryPath;
            rubric.PrimaryRepertorySource = primary.SourceCode;
            rubric.RepertorySources = mapList.Select(m => m.SourceCode).Distinct().ToList();
        }

        return rubrics;
    }

    private async Task<List<AudioCaseSuggestedRubricModel>> ApplyEnterpriseQualityPipelineAsync(
        List<AudioCaseSuggestedRubricModel> accepted,
        IReadOnlyList<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph,
        CancellationToken cancellationToken)
    {
        foreach (var rubric in accepted.Where(EnterpriseRubricPresentationHelper.IsAuthoritativeRepertory))
        {
            EnterpriseRubricPresentationHelper.MarkRepertoryRubric(
                rubric, rubric.RepertoryPath, rubric.SelectionReason ?? rubric.WhySuggested);
        }

        accepted = accepted
            .Select(r =>
            {
                r.EnterpriseConfidenceScore = EnterpriseRubricConfidenceEngine.Compute(r, _options);
                r.SelectionReason ??= r.WhySuggested;
                return r;
            })
            .Where(r => string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase)
                || EnterpriseRubricConfidenceEngine.MeetsDisplayThreshold(r.EnterpriseConfidenceScore ?? 0, _options))
            .Where(r => string.Equals(r.ResultKind, EnterpriseRubricPresentationHelper.ResultKindAiConcept, StringComparison.OrdinalIgnoreCase)
                || !EnterpriseGenericRubricFilter.ShouldSuppress(r))
            .ToList();

        accepted = EnterpriseRubricDeduplicator.DeduplicateRubrics(accepted);

        if (_options.EnableRepertoryMapping)
        {
            accepted = await EnrichRepertoryMappingAsync(accepted, cancellationToken);
        }

        if (!_options.EnableAiClinicalConceptSuggestions)
        {
            return accepted;
        }

        var conceptsWithRubric = discoveries
            .Where(d => d.SubSectionId > 0)
            .Select(d => d.HomeopathicConceptId)
            .ToHashSet();

        // Also treat already-accepted repertory rows as coverage (keyword/hybrid may have filled gaps).
        foreach (var rubric in accepted.Where(r => r.SubSectionId > 0))
        {
            if (rubric.SourceConceptId.HasValue)
            {
                foreach (var homeo in graph.HomeopathicConcepts)
                {
                    if (homeo.HomeopathicConceptId is > 0
                        && rubric.SourceConceptId == ConceptIdentity.FromHomeopathicConceptId(homeo.HomeopathicConceptId.Value))
                    {
                        conceptsWithRubric.Add(homeo.HomeopathicConceptId);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(rubric.MatchedFrom))
            {
                foreach (var homeo in graph.HomeopathicConcepts.Where(h => !string.IsNullOrWhiteSpace(h.ConceptName)))
                {
                    if (rubric.MatchedFrom.Contains(homeo.ConceptName, StringComparison.OrdinalIgnoreCase)
                        || homeo.ConceptName.Contains(rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase))
                    {
                        conceptsWithRubric.Add(homeo.HomeopathicConceptId);
                    }
                }
            }
        }

        var aiConcepts = new List<AudioCaseSuggestedRubricModel>();
        foreach (var homeo in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
        {
            if (conceptsWithRubric.Contains(homeo.HomeopathicConceptId))
            {
                continue;
            }

            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            var meaning = clinical != null
                ? graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                    ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                : null;

            aiConcepts.Add(EnterpriseRubricPresentationHelper.CreateAiClinicalConcept(
                homeo, clinical, meaning, graph.EngineVersion));
        }

        _logger.LogInformation(
            "Enterprise quality pipeline session {SessionId}: repertory={RepertoryCount}, aiConcepts={AiConceptCount}",
            graph.SessionId,
            accepted.Count,
            aiConcepts.Count);

        return RubricCandidateQualityGate.DeduplicateAiConceptsWhenDbMatched(
            EnterpriseRubricPresentationHelper.PartitionResults(accepted, aiConcepts));
    }

    private static bool IsValidCandidate(RubricDiscoveryNodeModel d) =>
        d.SubSectionId > 0
        && (d.EvidenceChain?.IsComplete == true
            || (!string.IsNullOrWhiteSpace(d.EvidenceChain?.TranscriptStatement)
                && !string.IsNullOrWhiteSpace(d.EvidenceChain?.HomeopathicConcept))
            || (d.Confidence >= 0.55m && !string.IsNullOrWhiteSpace(d.SubSectionName)));

    private bool IsValidCandidateForRanking(RubricDiscoveryNodeModel d)
    {
        if (d.SubSectionId <= 0)
            return false;

        if (_options.EnforceEvidenceChainCompleteGate)
            return d.EvidenceChain?.IsComplete == true;

        return IsValidCandidate(d);
    }

    private (List<AudioCaseSuggestedRubricModel> Tier1, List<AudioCaseSuggestedRubricModel> Tier2, List<AudioCaseSuggestedRubricModel> Tier3, List<AudioCaseSuggestedRubricModel> Review)
        BuildTieredOutput(List<AudioCaseSuggestedRubricModel> accepted)
    {
        var review = accepted
            .Where(r => string.Equals(r.RubricTier, "Review", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.ValidationStatus, "ReviewSuggested", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .Take(10)
            .ToList();

        var reviewIds = review.Select(r => r.SubSectionId).ToHashSet();

        var tier1 = accepted
            .Where(r => !reviewIds.Contains(r.SubSectionId))
            .Where(r => (r.ConfidenceScore ?? r.MatchScore) >= 0.90m)
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .Take(_options.MaxRubricsTier1)
            .ToList();

        var tier2 = accepted
            .Where(r => !reviewIds.Contains(r.SubSectionId))
            .Where(r => (r.ConfidenceScore ?? r.MatchScore) >= 0.75m && (r.ConfidenceScore ?? r.MatchScore) < 0.90m)
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .Take(_options.MaxRubricsTier2)
            .ToList();

        var tier3 = accepted
            .Where(r => !reviewIds.Contains(r.SubSectionId))
            .Where(r => (r.ConfidenceScore ?? r.MatchScore) >= 0.60m && (r.ConfidenceScore ?? r.MatchScore) < 0.75m)
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .Take(_options.MaxRubricsTier3)
            .ToList();

        foreach (var r in tier1) r.RubricTier = "Primary";
        foreach (var r in tier2) r.RubricTier = "Strong";
        foreach (var r in tier3) r.RubricTier = "Supporting";

        return (tier1, tier2, tier3, review);
    }

    private (List<AudioCaseSuggestedRubricModel> Tier1, List<AudioCaseSuggestedRubricModel> Tier2, List<AudioCaseSuggestedRubricModel> Tier3, List<AudioCaseSuggestedRubricModel> Review)
        BuildTieredOutputFromCandidates(
            RubricCandidateEngineResult candidateResult,
            ConceptGraphFullModel graph,
            List<AudioCaseSuggestedRubricModel> acceptedRubrics)
    {
        var acceptedMap = IndexRubricsBySubSectionId(acceptedRubrics);
        var review = acceptedRubrics
            .Where(r => string.Equals(r.RubricTier, "Review", StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.ValidationStatus, "ReviewSuggested", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var tier1 = candidateResult.Tier1Rubrics
            .Where(c => acceptedMap.ContainsKey(c.SubSectionId))
            .Select(c => ToSuggestedRubricFromCandidate(c, graph, "Primary"))
            .Take(_options.MaxRubricsTier1)
            .ToList();

        var tier2 = candidateResult.Tier2Rubrics
            .Where(c => acceptedMap.ContainsKey(c.SubSectionId))
            .Select(c => ToSuggestedRubricFromCandidate(c, graph, "Strong"))
            .Take(_options.MaxRubricsTier2)
            .ToList();

        var tier3 = candidateResult.Tier3Rubrics
            .Where(c => acceptedMap.ContainsKey(c.SubSectionId))
            .Select(c => ToSuggestedRubricFromCandidate(c, graph, "Supporting"))
            .Take(_options.MaxRubricsTier3)
            .ToList();

        return (tier1, tier2, tier3, review);
    }

    private static AudioCaseSuggestedRubricModel ToSuggestedRubricFromCandidate(
        RubricCandidateModel candidate,
        ConceptGraphFullModel graph,
        string rubricTierLabel)
    {
        var homeo = graph.HomeopathicConcepts.FirstOrDefault(h => h.HomeopathicConceptId == candidate.HomeopathicConceptId)
            ?? graph.HomeopathicConcepts.FirstOrDefault(h =>
                string.Equals(h.ConceptName, candidate.SourceConceptName, StringComparison.OrdinalIgnoreCase));

        var chain = candidate.EvidenceChain;
        return new AudioCaseSuggestedRubricModel
        {
            SubSectionId = candidate.SubSectionId,
            SubSectionName = candidate.SubSectionName,
            MatchScore = candidate.CompositeScore,
            ConfidenceScore = candidate.CompositeScore,
            SuggestedIntensityNo = homeo?.IsSRP == true ? 3 : 2,
            MatchedFrom = chain?.TranscriptStatement ?? chain?.PatientMeaning,
            MatchSource = "RubricCandidateEngine",
            MatchLayer = candidate.MatchMethod,
            WhySuggested = candidate.MatchReason,
            EngineVersion = graph.EngineVersion,
            RequiresManualApproval = true,
            IsAiSuggested = true,
            HomeopathicWeight = homeo?.Weight ?? 1m,
            RubricTier = rubricTierLabel,
            QualityScore = Math.Round(candidate.CompositeScore * 100m, 2),
            ValidationStatus = candidate.Validation?.IsValid == true ? "Accepted" : "Review",
            EvidenceChain = chain == null ? null : new RubricEvidenceChainModel
            {
                PatientStatements = new List<string> { chain.PatientMeaning ?? string.Empty },
                ClinicalMeanings = new List<string> { chain.ClinicalConcept ?? string.Empty, chain.HomeopathicConcept ?? string.Empty },
                EvidenceStrength = candidate.EvidenceScore,
                TranscriptExcerpt = chain.TranscriptStatement,
            },
        };
    }

    private static List<AudioCaseSuggestedRubricModel> MergeRubrics(
        List<AudioCaseSuggestedRubricModel> existing,
        List<AudioCaseSuggestedRubricModel> additional)
    {
        var map = IndexRubricsBySubSectionId(existing);
        foreach (var r in additional)
        {
            if (!map.ContainsKey(r.SubSectionId))
                map[r.SubSectionId] = r;
        }

        return map.Values
            .OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore)
            .ToList();
    }

    private static Dictionary<int, AudioCaseSuggestedRubricModel> IndexRubricsBySubSectionId(
        IEnumerable<AudioCaseSuggestedRubricModel> rubrics) =>
        rubrics
            .Where(r => r.SubSectionId > 0)
            .GroupBy(r => r.SubSectionId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.QualityScore ?? r.ConfidenceScore ?? r.MatchScore).First());

    private static ConceptGraphAnalysisResult Fail(string? error, List<string> stages) =>
        new() { Success = false, Error = error, StagesCompleted = stages };

    private async Task ReportProgressAsync(
        Guid sessionId, string step, int percentHint, CancellationToken cancellationToken)
    {
        if (!_options.EnableV35RecallEngine) return;

        try
        {
            await _progress.ReportAsync(sessionId, step, percentHint, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Progress report failed for session {SessionId} at step {Step}", sessionId, step);
        }
    }

    private async Task PersistGraphAsync(
        Guid sessionId, ConceptGraphFullModel graph, CancellationToken cancellationToken)
    {
        if (_options.DeferGraphPersistence && _options.EnableV35FastPipeline && _options.EnableV35RecallEngine)
        {
            _logger.LogInformation(
                "LatencyGap3 Session {SessionId}: PersistGraphAsync DEFERRED (fire-and-forget) — does not block Completed",
                sessionId);
            _ = PersistGraphInBackgroundAsync(sessionId, graph);
            return;
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _repository.SaveFullGraphAsync(sessionId, graph, cancellationToken);
        sw.Stop();
        _logger.LogInformation(
            "LatencyGap3 Session {SessionId}: PersistGraphAsync SYNC SaveFullGraphAsync elapsedMs={Ms}",
            sessionId,
            sw.ElapsedMilliseconds);
    }

    private async Task PersistGraphInBackgroundAsync(Guid sessionId, ConceptGraphFullModel graph)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IConceptGraphRepository>();
            await repo.SaveFullGraphAsync(sessionId, graph, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deferred graph persistence failed for session {SessionId}", sessionId);
        }
    }

    private async Task AuditAsync(
        Guid sessionId, string stage, string modelId, bool success, string? error, int? latencyMs,
        CancellationToken cancellationToken)
    {
        if (_options.EnableV35FastPipeline && _options.EnableV35RecallEngine)
            return;

        await _repository.SaveReasoningAuditAsync(sessionId, new AiReasoningAuditModel
        {
            PipelineStage = stage,
            ModelId = modelId,
            Success = success,
            ErrorMessage = error,
            LatencyMs = latencyMs,
        }, cancellationToken);
    }

    private static List<ClinicalConceptModel> ToLegacyConcepts(ConceptGraphFullModel graph)
    {
        var concepts = new List<ClinicalConceptModel>();
        var order = 1;

        foreach (var homeo in graph.HomeopathicConcepts)
        {
            var clinical = graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                ?? graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
            if (clinical == null) continue;

            var meaning = graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                ?? graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId);

            concepts.Add(new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                SequenceOrder = order++,
                RawStatement = meaning?.RawStatement ?? clinical.ConceptName,
                ClinicalMeaning = clinical.ConceptName,
                HomeopathicMeaning = homeo.ConceptName,
                Category = homeo.Category ?? clinical.SymptomCategory ?? clinical.Domain,
                IsSRP = homeo.IsSRP,
                Confidence = homeo.Confidence,
                HomeopathicWeight = homeo.Weight,
                ConceptTier = homeo.ConceptTier ?? clinical.ConceptTier,
            });
        }

        if (concepts.Count > 0)
            return concepts;

        return graph.ClinicalConcepts.Select(c =>
        {
            var meaning = graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == c.PatientMeaningId);
            return new ClinicalConceptModel
            {
                ConceptId = Guid.NewGuid(),
                RawStatement = meaning?.RawStatement ?? c.ConceptName,
                ClinicalMeaning = c.ConceptName,
                HomeopathicMeaning = c.ConceptName,
                Category = c.SymptomCategory ?? c.Domain,
                Confidence = c.Confidence,
                ConceptTier = c.ConceptTier,
            };
        }).ToList();
    }

    private static List<AudioCaseSymptomModel> BuildSymptoms(ConceptGraphFullModel graph) =>
        graph.Meanings.Select(m => new AudioCaseSymptomModel { Phrase = m.NormalizedMeaning }).ToList();

    private static AudioCaseSuggestedRubricModel ToSuggestedRubric(
        RubricDiscoveryNodeModel discovery,
        ConceptGraphFullModel graph)
    {
        var homeo = graph.HomeopathicConcepts.FirstOrDefault(h => h.HomeopathicConceptId == discovery.HomeopathicConceptId);
        var chain = discovery.EvidenceChain;
        var confidence = discovery.Confidence;
        var isRepertoryDb = string.Equals(discovery.DiscoveryMethod, RubricDiscoverySources.RepertoryDb, StringComparison.OrdinalIgnoreCase);
        var sourceConceptId = homeo?.HomeopathicConceptId is > 0
            ? ConceptIdentity.FromHomeopathicConceptId(homeo.HomeopathicConceptId.Value)
            : (Guid?)null;

        return new AudioCaseSuggestedRubricModel
        {
            SubSectionId = discovery.SubSectionId,
            SubSectionName = discovery.SubSectionName,
            MatchScore = confidence,
            ConfidenceScore = confidence,
            SuggestedIntensityNo = homeo?.IsSRP == true ? 3 : 2,
            MatchedFrom = chain?.TranscriptStatement ?? chain?.PatientMeaning ?? homeo?.ConceptName,
            MatchSource = isRepertoryDb ? RubricDiscoverySources.RepertoryDb : MapDiscoveryMethodToMatchSource(discovery.DiscoveryMethod),
            MatchLayer = discovery.DiscoveryMethod,
            WhySuggested = discovery.MatchReason,
            SelectionReason = discovery.MatchReason,
            EngineVersion = graph.EngineVersion,
            RequiresManualApproval = true,
            IsAiSuggested = !isRepertoryDb,
            ResultKind = isRepertoryDb ? EnterpriseRubricPresentationHelper.ResultKindRepertory : null,
            HomeopathicWeight = homeo?.Weight ?? 1m,
            RubricTier = ConceptGraphTierHelper.ResolveRubricTierLabel(confidence),
            QualityScore = discovery.QualityScore,
            ValidationStatus = discovery.ValidationStatus,
            EvidenceChainComplete = chain?.IsComplete == true,
            Source = isRepertoryDb ? "Database" : "AiSuggested",
            GroundedInOntology = graph.Metaphors.Any(m => m.GroundedInOntology),
            SourceConceptId = sourceConceptId,
            MatchedFromDetail = new RubricMatchedFromModel
            {
                PatientStatement = chain?.TranscriptStatement,
                NormalizedMeaning = chain?.PatientMeaning,
                ClinicalConcept = chain?.ClinicalConcept,
                HomeopathicConcept = chain?.HomeopathicConcept,
                SymptomClass = homeo?.IsSRP == true ? "SRP" : null,
            },
            Scores = new RubricUnifiedScoresModel
            {
                ConceptMatchConfidence = confidence,
                FinalHybridScore = confidence,
                CalibratedAcceptanceProbability = confidence,
            },
            EvidenceChain = chain == null ? null : new RubricEvidenceChainModel
            {
                PatientStatements = new List<string> { chain.PatientMeaning ?? string.Empty },
                ClinicalMeanings = new List<string> { chain.ClinicalConcept ?? string.Empty, chain.HomeopathicConcept ?? string.Empty },
                EvidenceStrength = chain.Confidence,
                TranscriptExcerpt = chain.TranscriptStatement,
            },
        };
    }

    private static string MapDiscoveryMethodToMatchSource(string? method) =>
        method switch
        {
            RubricDiscoverySources.RepertoryDb => RubricDiscoverySources.RepertoryDb,
            RubricDiscoverySources.KnowledgeGraph => RubricDiscoverySources.KnowledgeGraph,
            RubricDiscoverySources.EnterpriseEmbedding => RubricDiscoverySources.EnterpriseEmbedding,
            RubricDiscoverySources.Bootstrap => RubricDiscoverySources.Bootstrap,
            var m when m != null && m.Contains("Embedding", StringComparison.OrdinalIgnoreCase) => RubricDiscoverySources.EnterpriseEmbedding,
            _ => "ConceptGraph",
        };

    private static string ExtractTail(string rubricName)
    {
        if (string.IsNullOrWhiteSpace(rubricName)) return string.Empty;
        var dash = rubricName.LastIndexOf('-');
        return dash > 0 && dash < rubricName.Length - 1
            ? rubricName[(dash + 1)..].Trim()
            : rubricName.Trim();
    }
}
