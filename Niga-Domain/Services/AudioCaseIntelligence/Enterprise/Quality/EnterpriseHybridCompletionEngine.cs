using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

public interface IEnterpriseHybridCompletionEngine
{
    Task<HybridCompletionDiscoveryResult> DiscoverPerConceptAsync(
        HybridCompletionRequest request,
        CancellationToken cancellationToken = default);

    HybridCompletionOutput FinalizeOutput(
        IReadOnlyList<AudioCaseSuggestedRubricModel> validatedRepertoryRubrics,
        HybridCompletionRequest request,
        IReadOnlyList<HybridConceptAuditEntry> auditEntries,
        ConceptGraphFullModel graph);
}

public class EnterpriseHybridCompletionEngine : IEnterpriseHybridCompletionEngine
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHybridCompletionAuditLogger _auditLogger;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<EnterpriseHybridCompletionEngine> _logger;

    public EnterpriseHybridCompletionEngine(
        IServiceScopeFactory scopeFactory,
        IHybridCompletionAuditLogger auditLogger,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<EnterpriseHybridCompletionEngine> logger)
    {
        _scopeFactory = scopeFactory;
        _auditLogger = auditLogger;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<HybridCompletionDiscoveryResult> DiscoverPerConceptAsync(
        HybridCompletionRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = new HybridCompletionDiscoveryResult();
        var globalMap = new Dictionary<int, RubricDiscoveryNodeModel>();

        var concepts = request.Graph.HomeopathicConcepts
            .Where(c => !string.IsNullOrWhiteSpace(c.ConceptName))
            .OrderByDescending(c => TierBoost(c.ConceptTier) * c.Weight * c.Confidence)
            .ToList();

        result.ConceptCount = concepts.Count;

        // Gap 2: parallel per-concept DB/hotspot search (was sequential → multi-minute for 9 concepts).
        // Each concept gets its own DI scope so EF DbContext is not shared across concurrent searches.
        var maxConcurrency = Math.Clamp(_options.ConceptDiscoveryMaxConcurrency, 1, 8);
        using var gate = new SemaphoreSlim(maxConcurrency);
        var totalSw = System.Diagnostics.Stopwatch.StartNew();

        var tasks = concepts.Select(async homeo =>
        {
            await gate.WaitAsync(cancellationToken);
            var conceptSw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var clinical = request.Graph.ClinicalConcepts.ElementAtOrDefault(homeo.ClinicalConceptIndex)
                    ?? request.Graph.ClinicalConcepts.FirstOrDefault(c => c.ClinicalConceptId == homeo.ClinicalConceptId);
                var meaning = clinical != null
                    ? request.Graph.Meanings.ElementAtOrDefault(clinical.MeaningIndex)
                        ?? request.Graph.Meanings.FirstOrDefault(m => m.PatientMeaningId == clinical.PatientMeaningId)
                    : null;

                using var scope = _scopeFactory.CreateScope();
                var pipeline = scope.ServiceProvider.GetRequiredService<PerConceptRubricSearchPipeline>();
                var (discoveries, audit) = await pipeline.SearchAsync(
                    homeo, clinical, meaning, request, _options, cancellationToken);

                conceptSw.Stop();
                audit.LatencyMs = (int)conceptSw.ElapsedMilliseconds;
                audit.ValidationSummary = $"latencyMs={audit.LatencyMs}; " +
                                          $"db={audit.DatabaseMatchCount}; emb={audit.EmbeddingMatchCount}; " +
                                          $"kg={audit.KnowledgeGraphMatchCount}";

                _logger.LogInformation(
                    "LatencyGap2 Session {SessionId}: concept '{Concept}' latencyMs={Ms} db={Db} emb={Emb} slots={Slots}",
                    request.SessionId,
                    homeo.ConceptName,
                    conceptSw.ElapsedMilliseconds,
                    audit.DatabaseMatchCount,
                    audit.EmbeddingMatchCount,
                    audit.SlotsAllocated);

                return (discoveries, audit, conceptSw.ElapsedMilliseconds);
            }
            finally
            {
                gate.Release();
            }
        });

        var units = await Task.WhenAll(tasks);
        totalSw.Stop();

        foreach (var (discoveries, audit, _) in units)
        {
            result.AuditEntries.Add(audit);
            result.TotalSlotsAllocated += audit.SlotsAllocated;
            _auditLogger.LogConceptPipeline(request.SessionId, audit);

            foreach (var discovery in discoveries)
            {
                if (!globalMap.TryGetValue(discovery.SubSectionId, out var existing)
                    || discovery.Confidence > existing.Confidence)
                {
                    globalMap[discovery.SubSectionId] = discovery;
                }
            }
        }

        result.Discoveries = globalMap.Values
            .OrderByDescending(d => d.Confidence)
            .Take(_options.MaxEnterpriseDiscoveryCandidates)
            .ToList();

        _logger.LogInformation(
            "LatencyGap2 Hybrid completion V5.2 session {SessionId}: concepts={ConceptCount} discoveries={DiscoveryCount} slots={Slots} totalMs={TotalMs} concurrency={Concurrency}",
            request.SessionId,
            result.ConceptCount,
            result.Discoveries.Count,
            result.TotalSlotsAllocated,
            totalSw.ElapsedMilliseconds,
            maxConcurrency);

        return result;
    }

    public HybridCompletionOutput FinalizeOutput(
        IReadOnlyList<AudioCaseSuggestedRubricModel> validatedRepertoryRubrics,
        HybridCompletionRequest request,
        IReadOnlyList<HybridConceptAuditEntry> auditEntries,
        ConceptGraphFullModel graph)
    {
        var repertory = validatedRepertoryRubrics
            .Where(EnterpriseRubricPresentationHelper.IsAuthoritativeRepertory)
            .Select(r =>
            {
                EnterpriseRubricPresentationHelper.MarkRepertoryRubric(
                    r, r.RepertoryPath, r.SelectionReason ?? r.WhySuggested);
                r.EnterpriseConfidenceScore = EnterpriseRubricConfidenceEngine.Compute(r, _options);
                r.SelectionReason ??= r.WhySuggested;
                return r;
            })
            .Where(r => EnterpriseRubricConfidenceEngine.MeetsDisplayThreshold(
                r.EnterpriseConfidenceScore ?? 0, _options))
            .Where(r => !EnterpriseGenericRubricFilter.ShouldSuppress(r))
            .ToList();

        repertory = HybridCompletionRanker.RankRubrics(repertory, graph);
        repertory = EnterpriseRubricDeduplicator.DeduplicateRubrics(repertory);

        var mappedConceptIds = new HashSet<long?>();
        foreach (var rubric in repertory)
        {
            var discovery = request.GlobalDiscoveries.FirstOrDefault(d => d.SubSectionId == rubric.SubSectionId);
            if (discovery?.HomeopathicConceptId != null)
            {
                mappedConceptIds.Add(discovery.HomeopathicConceptId);
            }

            var matchedConcept = graph.HomeopathicConcepts.FirstOrDefault(h =>
                string.Equals(h.ConceptName, rubric.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                || (rubric.WhySuggested?.Contains(h.ConceptName, StringComparison.OrdinalIgnoreCase) ?? false));
            if (matchedConcept?.HomeopathicConceptId != null)
            {
                mappedConceptIds.Add(matchedConcept.HomeopathicConceptId);
            }
        }

        var aiConcepts = new List<AudioCaseSuggestedRubricModel>();
        var finalizedAudit = auditEntries.ToList();

        if (_options.EnableAiClinicalConceptSuggestions)
        {
            foreach (var homeo in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
            {
                if (mappedConceptIds.Contains(homeo.HomeopathicConceptId))
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

                finalizedAudit.Add(new HybridConceptAuditEntry
                {
                    HomeopathicConceptId = homeo.HomeopathicConceptId,
                    ConceptName = homeo.ConceptName,
                    ConceptConfidence = homeo.Confidence,
                    OutputCategory = HybridCompletionOutputCategories.AiClinicalConcept,
                    ValidationSummary = "No repertory rubric passed validation for this concept.",
                    Stages = auditEntries
                        .FirstOrDefault(a => a.HomeopathicConceptId == homeo.HomeopathicConceptId)?.Stages
                        ?? new List<HybridCompletionStageResult>(),
                });

                if (aiConcepts.Count >= _options.MaxAiClinicalConcepts)
                {
                    break;
                }
            }
        }

        var output = new HybridCompletionOutput
        {
            RepertoryRubrics = repertory,
            AiClinicalConcepts = aiConcepts,
            AllRubrics = EnterpriseRubricPresentationHelper.PartitionResults(repertory, aiConcepts),
            AuditEntries = finalizedAudit,
        };

        _auditLogger.LogFinalizeSummary(request.SessionId, output);
        return output;
    }

    private static decimal TierBoost(string? tier) =>
        tier switch
        {
            "Primary" => 1.25m,
            "Secondary" => 1.10m,
            "Supporting" => 1.0m,
            _ => 1.0m,
        };
}

public static class HybridCompletionRanker
{
    public static List<AudioCaseSuggestedRubricModel> RankRubrics(
        List<AudioCaseSuggestedRubricModel> rubrics,
        ConceptGraphFullModel graph)
    {
        return rubrics
            .Select(r =>
            {
                var homeo = graph.HomeopathicConcepts.FirstOrDefault(h =>
                    string.Equals(h.ConceptName, r.MatchedFrom, StringComparison.OrdinalIgnoreCase)
                    || (r.WhySuggested?.Contains(h.ConceptName, StringComparison.OrdinalIgnoreCase) ?? false));
                r.EnterpriseConfidenceScore = ComputeHybridRankScore(r, homeo);
                return r;
            })
            .OrderByDescending(r => r.EnterpriseConfidenceScore ?? 0)
            .ThenByDescending(r => r.QualityScore ?? 0)
            .ThenByDescending(r => r.ConfidenceScore ?? r.MatchScore)
            .ToList();
    }

    public static decimal ComputeHybridRankScore(
        AudioCaseSuggestedRubricModel rubric,
        HomeopathicConceptNodeModel? homeo)
    {
        var evidence = rubric.EvidenceChain?.EvidenceStrength ?? 0m;
        var conceptConfidence = homeo?.Confidence ?? 0.5m;
        var embedding = rubric.ConfidenceScore ?? rubric.MatchScore;
        var validation = (rubric.QualityScore ?? 0m) / 100m;
        var doctorLearning = string.Equals(rubric.MatchSource, RubricDiscoverySources.DoctorLearning, StringComparison.OrdinalIgnoreCase)
            ? 0.15m
            : 0m;
        var repertoryBoost = string.Equals(rubric.MatchSource, RubricDiscoverySources.RepertoryDb, StringComparison.OrdinalIgnoreCase)
            ? 0.12m
            : 0m;

        var score =
            (evidence * 0.25m)
            + (conceptConfidence * 0.20m)
            + (embedding * 0.20m)
            + (validation * 0.20m)
            + doctorLearning
            + repertoryBoost;

        return Math.Round(Math.Clamp(score, 0m, 1m) * 100m, 2);
    }
}
