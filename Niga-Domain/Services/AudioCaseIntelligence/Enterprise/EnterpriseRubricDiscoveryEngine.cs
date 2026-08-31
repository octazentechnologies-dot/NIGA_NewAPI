using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.Enterprise;

public class EnterpriseRubricDiscoveryEngine : IEnterpriseRubricDiscoveryEngine
{
    public const string StageName = "EnterpriseRubricDiscovery";
    public const string EngineVersionLabel = "v4.0";

    private readonly IRubricCandidateEngine _candidateEngine;
    private readonly IRubricDiscoveryEngineV3 _legacyDiscoveryEngine;
    private readonly IConceptGraphEvidenceEngine _evidenceEngine;
    private readonly IKnowledgeGraphOrchestratorBridge _knowledgeGraphBridge;
    private readonly EnterpriseRubricExpansionEngine _expansionEngine;
    private readonly EnterpriseRubricCandidateMerger _merger;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<EnterpriseRubricDiscoveryEngine> _logger;

    public EnterpriseRubricDiscoveryEngine(
        IRubricCandidateEngine candidateEngine,
        IRubricDiscoveryEngineV3 legacyDiscoveryEngine,
        IConceptGraphEvidenceEngine evidenceEngine,
        IKnowledgeGraphOrchestratorBridge knowledgeGraphBridge,
        EnterpriseRubricExpansionEngine expansionEngine,
        EnterpriseRubricCandidateMerger merger,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<EnterpriseRubricDiscoveryEngine> logger)
    {
        _candidateEngine = candidateEngine;
        _legacyDiscoveryEngine = legacyDiscoveryEngine;
        _evidenceEngine = evidenceEngine;
        _knowledgeGraphBridge = knowledgeGraphBridge;
        _expansionEngine = expansionEngine;
        _merger = merger;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EnterpriseRubricDiscoveryResult> DiscoverCandidatesAsync(
        EnterpriseRubricDiscoveryRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new EnterpriseRubricDiscoveryResult
        {
            Diagnostics = new PipelineDiagnosticReportModel { SessionId = request.SessionId },
        };

        if (request.Graph.HomeopathicConcepts.Count == 0)
        {
            result.Error = "No homeopathic concepts available for rubric discovery.";
            result.Diagnostics.FailureStage = StageName;
            result.Diagnostics.FailureReason = result.Error;
            return result;
        }

        var merged = new Dictionary<int, EnterpriseRubricDiscoveryCandidate>();
        RubricCandidateEngineResult? embeddingResult = null;

        if (request.IncludeRuleExpansion && _options.EnableEnterpriseRubricExpansion)
        {
            var stage = StartDiag(result, "RuleExpansion");
            var sw = Stopwatch.StartNew();
            var expanded = await _expansionEngine.ExpandAsync(request.Graph, cancellationToken);
            _merger.MergeDiscoveries(merged, expanded, request.Graph);
            stage.DurationMs = (int)sw.ElapsedMilliseconds;
            stage.OutputCount = expanded.Count;
            stage.Detail = $"Expanded {expanded.Count} rubric(s) from bootstrap patterns.";
        }

        if (request.IncludeLegacyDiscovery)
        {
            var stage = StartDiag(result, "BootstrapLegacyDiscovery");
            var sw = Stopwatch.StartNew();
            var legacy = await _legacyDiscoveryEngine.DiscoverAsync(request.Graph.HomeopathicConcepts, cancellationToken);
            legacy = _evidenceEngine.BuildEvidenceChains(legacy, request.Graph);
            _merger.MergeDiscoveries(merged, legacy, request.Graph, RubricDiscoverySources.Bootstrap);
            stage.DurationMs = (int)sw.ElapsedMilliseconds;
            stage.OutputCount = legacy.Count;
            stage.Detail = legacy.Count == 0
                ? "No bootstrap/legacy embedding matches."
                : $"Bootstrap + legacy embedding: {legacy.Count} rubric(s).";
            if (legacy.Count == 0)
                stage.Status = "WARN";
        }

        if (request.IncludeEnterpriseEmbeddings && _options.EnableRubricCandidateEngine)
        {
            var stage = StartDiag(result, "EnterpriseConceptEmbedding");
            var sw = Stopwatch.StartNew();
            embeddingResult = await _candidateEngine.DiscoverFromGraphAsync(request.Graph, null, cancellationToken);
            result.EmbeddingEngineResult = embeddingResult;

            if (embeddingResult.Success && embeddingResult.Discoveries.Count > 0)
            {
                _merger.MergeDiscoveries(merged, embeddingResult.Discoveries, request.Graph, RubricDiscoverySources.EnterpriseEmbedding);
                foreach (var c in embeddingResult.Candidates)
                    _merger.MergeCandidate(merged, c, RubricDiscoverySources.EnterpriseEmbedding);
                stage.OutputCount = embeddingResult.Discoveries.Count;
                stage.Detail = $"Enterprise embeddings: {embeddingResult.Discoveries.Count} rubric(s), version={embeddingResult.VersionCode}.";
            }
            else
            {
                stage.Status = "WARN";
                stage.Error = embeddingResult.Error ?? "Enterprise embedding search returned zero candidates.";
                stage.Detail = stage.Error;
            }

            stage.DurationMs = (int)sw.ElapsedMilliseconds;
        }

        if (request.IncludeKnowledgeGraph && _options.EnableEnterpriseKnowledgeGraph)
        {
            var stage = StartDiag(result, "KnowledgeGraphDiscovery");
            var sw = Stopwatch.StartNew();
            var kg = await _knowledgeGraphBridge.DiscoverAsync(
                request.SessionId, request.Graph, request.Transcript, embeddingResult, cancellationToken);
            result.KnowledgeGraphResult = kg;

            if (kg.Success && kg.Discoveries.Count > 0)
            {
                _merger.MergeDiscoveries(merged, kg.Discoveries, request.Graph, RubricDiscoverySources.KnowledgeGraph);
                stage.OutputCount = kg.Discoveries.Count;
                stage.Detail = $"KG paths={kg.Paths.Count}, discoveries={kg.Discoveries.Count}.";
            }
            else
            {
                stage.Status = "WARN";
                stage.Detail = kg.Error ?? "Knowledge graph returned zero rubric paths.";
            }

            stage.DurationMs = (int)sw.ElapsedMilliseconds;
        }

        var ranked = _merger.RankAndTake(merged.Values, request.MaxCandidates, _options);
        result.Candidates = ranked;
        result.RubricCandidates = ranked.Select(_merger.ToRubricCandidate).ToList();
        result.Discoveries = ranked.Select(c => _merger.ToDiscoveryNode(c)).ToList();
        result.Success = ranked.Count > 0;

        result.Diagnostics.TotalCandidates = ranked.Count;
        result.Diagnostics.CandidatesBySource = ranked
            .GroupBy(c => c.DiscoverySource)
            .ToDictionary(g => g.Key, g => g.Count());

        if (!result.Success)
        {
            result.Error = "All discovery sources returned zero rubric candidates.";
            result.Diagnostics.FailureStage = StageName;
            result.Diagnostics.FailureReason = BuildFailureSummary(result.Diagnostics);
        }

        _logger.LogInformation(
            "Enterprise rubric discovery session={SessionId}: candidates={Count} sources={Sources} stages={Stages}",
            request.SessionId,
            ranked.Count,
            string.Join(',', result.Diagnostics.CandidatesBySource.Select(kv => $"{kv.Key}:{kv.Value}")),
            string.Join(',', result.Diagnostics.Stages.Select(s => $"{s.StageName}:{s.DurationMs}ms")));

        return result;
    }

    private static PipelineStageDiagnosticModel StartDiag(EnterpriseRubricDiscoveryResult result, string name)
    {
        var stage = new PipelineStageDiagnosticModel { StageName = name };
        result.Diagnostics.Stages.Add(stage);
        return stage;
    }

    private static string BuildFailureSummary(PipelineDiagnosticReportModel diag) =>
        string.Join("; ", diag.Stages.Select(s =>
            $"{s.StageName}={s.Status}({s.OutputCount}){(string.IsNullOrWhiteSpace(s.Error) ? "" : $":{s.Error}")}"));
}

public class EnterpriseRubricCandidateMerger
{
    private static readonly Dictionary<string, int> SourcePriority = new(StringComparer.OrdinalIgnoreCase)
    {
        [RubricDiscoverySources.RepertoryDb] = 1,
        [RubricDiscoverySources.Bootstrap] = 2,
        [RubricDiscoverySources.DoctorLearning] = 3,
        [RubricDiscoverySources.KnowledgeGraph] = 4,
        [RubricDiscoverySources.RuleExpansion] = 4,
        [RubricDiscoverySources.EnterpriseEmbedding] = 5,
        [RubricDiscoverySources.Embedding] = 6,
        [RubricDiscoverySources.AiInference] = 7,
    };

    public void MergeDiscoveries(
        Dictionary<int, EnterpriseRubricDiscoveryCandidate> map,
        IReadOnlyList<RubricDiscoveryNodeModel> discoveries,
        ConceptGraphFullModel graph,
        string source = RubricDiscoverySources.Bootstrap)
    {
        foreach (var d in discoveries)
        {
            if (d.SubSectionId <= 0) continue;
            var candidate = new EnterpriseRubricDiscoveryCandidate
            {
                SubSectionId = d.SubSectionId,
                SubSectionName = d.SubSectionName,
                HomeopathicConceptId = d.HomeopathicConceptId,
                SourceConceptName = d.EvidenceChain?.HomeopathicConcept ?? graph.HomeopathicConcepts
                    .FirstOrDefault(h => h.HomeopathicConceptId == d.HomeopathicConceptId)?.ConceptName ?? string.Empty,
                DiscoverySource = MapDiscoveryMethodToSource(d.DiscoveryMethod, source),
                DiscoveryMethod = d.DiscoveryMethod,
                MatchReason = d.MatchReason,
                SimilarityScore = d.Confidence,
                CompositeScore = d.Confidence,
                QualityScore = d.QualityScore ?? d.Confidence * 100m,
                EvidenceScore = d.EvidenceChain?.Confidence ?? 0m,
                EvidenceChain = d.EvidenceChain,
                SourcePriority = GetPriority(MapDiscoveryMethodToSource(d.DiscoveryMethod, source)),
            };
            Upsert(map, candidate);
        }
    }

    public void MergeCandidate(
        Dictionary<int, EnterpriseRubricDiscoveryCandidate> map,
        RubricCandidateModel c,
        string source)
    {
        var candidate = new EnterpriseRubricDiscoveryCandidate
        {
            SubSectionId = c.SubSectionId,
            SubSectionName = c.SubSectionName,
            HomeopathicConceptId = c.HomeopathicConceptId,
            SourceConceptName = c.SourceConceptName,
            DiscoverySource = source,
            DiscoveryMethod = c.MatchMethod,
            MatchReason = c.MatchReason,
            SimilarityScore = c.SimilarityScore,
            ClinicalRelevanceScore = c.ClinicalRelevanceScore,
            EvidenceScore = c.EvidenceScore,
            DoctorAcceptanceScore = c.DoctorAcceptanceScore,
            CompositeScore = c.CompositeScore,
            QualityScore = c.CompositeScore * 100m,
            EvidenceChain = c.EvidenceChain,
            MatchedConceptKey = c.MappedFromConceptKey,
            SourcePriority = GetPriority(source),
        };
        Upsert(map, candidate);
    }

    public List<EnterpriseRubricDiscoveryCandidate> RankAndTake(
        IEnumerable<EnterpriseRubricDiscoveryCandidate> candidates,
        int max,
        RubricIntelligenceOptions options)
    {
        return candidates
            .OrderBy(c => c.SourcePriority)
            .ThenByDescending(c => EnterpriseRubricRankingEngine.ComputeFinalScore(c, options))
            .ThenByDescending(c => c.CompositeScore)
            .Take(max)
            .Select((c, i) =>
            {
                c.QualityScore = EnterpriseRubricRankingEngine.ComputeFinalScore(c, options);
                return c;
            })
            .ToList();
    }

    public RubricDiscoveryNodeModel ToDiscoveryNode(EnterpriseRubricDiscoveryCandidate c) =>
        new()
        {
            HomeopathicConceptId = c.HomeopathicConceptId,
            SubSectionId = c.SubSectionId,
            SubSectionName = c.SubSectionName,
            MatchReason = c.MatchReason ?? $"{c.DiscoverySource}: {c.SourceConceptName}",
            DiscoveryMethod = c.DiscoveryMethod.Length > 0 ? c.DiscoveryMethod : c.DiscoverySource,
            Confidence = Math.Round(c.CompositeScore, 4),
            QualityScore = Math.Round(c.QualityScore, 2),
            EvidenceChain = c.EvidenceChain,
            RubricTier = ConceptGraphTierHelper.ResolveTier(c.CompositeScore),
        };

    public RubricCandidateModel ToRubricCandidate(EnterpriseRubricDiscoveryCandidate c) =>
        new()
        {
            SubSectionId = c.SubSectionId,
            SubSectionName = c.SubSectionName,
            HomeopathicConceptId = c.HomeopathicConceptId,
            SourceConceptName = c.SourceConceptName,
            SimilarityScore = c.SimilarityScore,
            ClinicalRelevanceScore = c.ClinicalRelevanceScore,
            EvidenceScore = c.EvidenceScore,
            DoctorAcceptanceScore = c.DoctorAcceptanceScore,
            CompositeScore = c.CompositeScore,
            MatchMethod = c.DiscoveryMethod,
            MatchReason = c.MatchReason,
            MappedFromConceptKey = c.MatchedConceptKey,
            EvidenceChain = c.EvidenceChain,
            RubricTier = ConceptGraphTierHelper.ResolveTier(c.CompositeScore),
        };

    private static void Upsert(Dictionary<int, EnterpriseRubricDiscoveryCandidate> map, EnterpriseRubricDiscoveryCandidate incoming)
    {
        if (!map.TryGetValue(incoming.SubSectionId, out var existing))
        {
            map[incoming.SubSectionId] = incoming;
            return;
        }

        if (incoming.SourcePriority < existing.SourcePriority
            || (incoming.SourcePriority == existing.SourcePriority && incoming.CompositeScore > existing.CompositeScore))
        {
            map[incoming.SubSectionId] = incoming;
        }
    }

    private static int GetPriority(string source) =>
        SourcePriority.TryGetValue(source, out var p) ? p : 99;

    private static string MapDiscoveryMethodToSource(string? method, string defaultSource) =>
        method switch
        {
            RubricDiscoverySources.RepertoryDb => RubricDiscoverySources.RepertoryDb,
            "ConceptMapping" or "SelfLearning" => RubricDiscoverySources.Bootstrap,
            "ScopedEmbedding" => RubricDiscoverySources.Embedding,
            "KnowledgeGraph" => RubricDiscoverySources.KnowledgeGraph,
            var m when m.Contains("Embedding", StringComparison.OrdinalIgnoreCase) => RubricDiscoverySources.EnterpriseEmbedding,
            var m when m.Contains("Inference", StringComparison.OrdinalIgnoreCase) => RubricDiscoverySources.AiInference,
            _ => defaultSource,
        };
}

public static class EnterpriseRubricRankingEngine
{
    /// <summary>Phase 2: clinical meaning → homeopathic → KG → embedding → evidence → repertory.</summary>
    public static decimal ComputeFinalScore(EnterpriseRubricDiscoveryCandidate c, RubricIntelligenceOptions options)
    {
        var clinicalMeaning = c.ClinicalRelevanceScore > 0 ? c.ClinicalRelevanceScore : c.EvidenceScore;
        var homeopathic = c.CompositeScore;
        var kg = string.Equals(c.DiscoverySource, RubricDiscoverySources.KnowledgeGraph, StringComparison.OrdinalIgnoreCase)
            ? 0.85m
            : 0m;
        var embedding = c.SimilarityScore;
        var evidence = c.EvidenceScore;
        var repertory = SourceAuthorityWeight(c.DiscoverySource);
        var doctor = c.DoctorAcceptanceScore;

        var score =
            (clinicalMeaning * 0.28m)
            + (homeopathic * 0.20m)
            + (kg * 0.12m)
            + (embedding * 0.15m)
            + (evidence * 0.15m)
            + (repertory * 0.07m)
            + (doctor * 0.03m);

        score = Math.Clamp(score, 0m, 1m);
        return Math.Round(score * 100m, 2);
    }

    private static decimal SourceAuthorityWeight(string source) =>
        source switch
        {
            RubricDiscoverySources.RepertoryDb => 1.0m,
            RubricDiscoverySources.Bootstrap => 0.92m,
            RubricDiscoverySources.DoctorLearning => 0.88m,
            RubricDiscoverySources.KnowledgeGraph => 0.84m,
            RubricDiscoverySources.RuleExpansion => 0.80m,
            RubricDiscoverySources.EnterpriseEmbedding => 0.72m,
            RubricDiscoverySources.Embedding => 0.68m,
            _ => 0.50m,
        };
}
