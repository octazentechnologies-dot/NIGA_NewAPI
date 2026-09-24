using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V6;

/// <summary>V6: per-symptom SQL-first discovery with KG and embedding supplement.</summary>
public class V6PerSymptomDiscoveryPipeline
{
    private readonly ISqlAuthoritativeRepertorySearchEngine _sqlSearch;

    public V6PerSymptomDiscoveryPipeline(ISqlAuthoritativeRepertorySearchEngine sqlSearch)
    {
        _sqlSearch = sqlSearch;
    }

    public async Task<(List<RubricDiscoveryNodeModel> Discoveries, V6SymptomDiscoveryAudit Audit)> SearchAsync(
        V6ClinicalSymptomUnit symptom,
        V6ClinicalReasoningRequest request,
        RubricIntelligenceOptions options,
        CancellationToken cancellationToken = default)
    {
        var audit = new V6SymptomDiscoveryAudit
        {
            SymptomLabel = symptom.HomeopathicLabel,
            HomeopathicConceptId = symptom.HomeopathicConceptId,
        };

        var stageMap = new Dictionary<int, RubricDiscoveryNodeModel>();
        var slotsPerSymptom = Math.Max(2, options.MaxRubricsPerConceptSlots);

        audit.StageLog.Add(V6PipelineStageNames.SqlRepertorySearch);
        var sqlHits = await _sqlSearch.SearchForSymptomAsync(symptom, cancellationToken);
        audit.SqlHits = sqlHits;

        foreach (var hit in sqlHits)
        {
            Upsert(stageMap, SqlHitToDiscovery(hit, symptom), symptom.Confidence);
        }

        audit.StageLog.Add(V6PipelineStageNames.HierarchyExpansion);

        var kgMatches = CollectKnowledgeGraphMatches(symptom, request);
        audit.KnowledgeGraphSubSectionIds = kgMatches.Select(d => d.SubSectionId).ToList();
        audit.StageLog.Add("KnowledgeGraph");
        foreach (var discovery in kgMatches)
        {
            Upsert(stageMap, discovery, symptom.Confidence);
        }

        var embeddingMatches = CollectEmbeddingMatches(symptom, request);
        audit.EmbeddingSubSectionIds = embeddingMatches.Select(d => d.SubSectionId).ToList();
        audit.StageLog.Add(V6PipelineStageNames.EmbeddingSearch);
        foreach (var discovery in embeddingMatches)
        {
            Upsert(stageMap, discovery, symptom.Confidence);
        }

        audit.StageLog.Add(V6PipelineStageNames.CandidateMerge);

        var ranked = stageMap.Values
            .Select(d => (Discovery: d, Score: ScoreDiscovery(d, symptom)))
            .OrderByDescending(x => SourcePriority(x.Discovery.DiscoveryMethod))
            .ThenByDescending(x => x.Score)
            .Take(slotsPerSymptom)
            .Select(x => x.Discovery)
            .ToList();

        audit.OutputCategory = ranked.Count > 0
            ? HybridCompletionOutputCategories.RepertoryDatabase
            : HybridCompletionOutputCategories.AiClinicalConcept;

        audit.ValidationResult = ranked.Count > 0
            ? $"SQL hits={sqlHits.Count}, merged={ranked.Count}"
            : "No repertory match — eligible for AI clinical concept";

        return (ranked, audit);
    }

    private static RubricDiscoveryNodeModel SqlHitToDiscovery(V6SqlSearchHit hit, V6ClinicalSymptomUnit symptom) =>
        new()
        {
            HomeopathicConceptId = symptom.HomeopathicConceptId,
            SubSectionId = hit.SubSectionId,
            SubSectionName = hit.SubSectionName,
            DiscoveryMethod = RubricDiscoverySources.RepertoryDb,
            MatchReason = $"{hit.SearchStage}: {hit.MatchPath}",
            Confidence = hit.SqlConfidence,
            QualityScore = hit.SqlConfidence * 100m,
            RubricTier = ConceptGraphTierHelper.ResolveTier(hit.SqlConfidence),
        };

    private static List<RubricDiscoveryNodeModel> CollectKnowledgeGraphMatches(
        V6ClinicalSymptomUnit symptom,
        V6ClinicalReasoningRequest request)
    {
        var fromDiscoveries = request.GlobalDiscoveries
            .Where(d => d.SubSectionId > 0
                && (d.HomeopathicConceptId == symptom.HomeopathicConceptId
                    || TextMatchesSymptom(d.MatchReason, symptom)))
            .Where(d => string.Equals(d.DiscoveryMethod, RubricDiscoverySources.KnowledgeGraph, StringComparison.OrdinalIgnoreCase)
                || (d.MatchReason?.Contains("Knowledge graph", StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(d => CloneForSymptom(d, symptom, RubricDiscoverySources.KnowledgeGraph,
                d.MatchReason ?? "Knowledge graph path"))
            .ToList();

        var fromPaths = request.KnowledgeGraphPaths
            .Where(p => p.SubSectionId > 0 && PathMatchesSymptom(p, symptom))
            .Select(p => new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = symptom.HomeopathicConceptId,
                SubSectionId = p.SubSectionId,
                SubSectionName = p.SubSectionName,
                DiscoveryMethod = RubricDiscoverySources.KnowledgeGraph,
                MatchReason = $"Knowledge graph: {string.Join(" → ", p.Steps.Select(s => s.DisplayText).Take(4))}",
                Confidence = Math.Max(p.PathConfidence, p.CompositeScore),
                RubricTier = ConceptGraphTierHelper.ResolveTier(p.CompositeScore),
            })
            .ToList();

        return MergeDiscoveryLists(fromDiscoveries, fromPaths);
    }

    private static List<RubricDiscoveryNodeModel> CollectEmbeddingMatches(
        V6ClinicalSymptomUnit symptom,
        V6ClinicalReasoningRequest request)
    {
        var fromDiscoveries = request.GlobalDiscoveries
            .Where(d => d.SubSectionId > 0 && d.HomeopathicConceptId == symptom.HomeopathicConceptId)
            .Where(d => string.Equals(d.DiscoveryMethod, RubricDiscoverySources.EnterpriseEmbedding, StringComparison.OrdinalIgnoreCase)
                || string.Equals(d.DiscoveryMethod, RubricDiscoverySources.Embedding, StringComparison.OrdinalIgnoreCase)
                || (d.DiscoveryMethod?.Contains("Embedding", StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        var fromCandidates = request.EmbeddingCandidates
            .Where(c => c.SubSectionId > 0
                && (c.HomeopathicConceptId == symptom.HomeopathicConceptId
                    || string.Equals(c.SourceConceptName, symptom.HomeopathicLabel, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(c.SourceConceptName, symptom.ClinicalLabel, StringComparison.OrdinalIgnoreCase)))
            .Select(c => new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = symptom.HomeopathicConceptId,
                SubSectionId = c.SubSectionId,
                SubSectionName = c.SubSectionName,
                DiscoveryMethod = RubricDiscoverySources.EnterpriseEmbedding,
                MatchReason = c.MatchReason ?? $"Embedding: {c.SourceConceptName}",
                Confidence = c.CompositeScore,
                QualityScore = c.CompositeScore * 100m,
                RubricTier = c.RubricTier,
            })
            .ToList();

        return MergeDiscoveryLists(fromDiscoveries, fromCandidates);
    }

    private static bool TextMatchesSymptom(string? text, V6ClinicalSymptomUnit symptom) =>
        !string.IsNullOrWhiteSpace(text)
        && (text.Contains(symptom.HomeopathicLabel, StringComparison.OrdinalIgnoreCase)
            || text.Contains(symptom.ClinicalLabel, StringComparison.OrdinalIgnoreCase));

    private static bool PathMatchesSymptom(KgRubricPathModel path, V6ClinicalSymptomUnit symptom)
    {
        var pathText = string.Join(' ', path.Steps.Select(s => s.DisplayText));
        return pathText.Contains(symptom.HomeopathicLabel, StringComparison.OrdinalIgnoreCase)
            || pathText.Contains(symptom.ClinicalLabel, StringComparison.OrdinalIgnoreCase);
    }

    private static RubricDiscoveryNodeModel CloneForSymptom(
        RubricDiscoveryNodeModel source,
        V6ClinicalSymptomUnit symptom,
        string method,
        string reason) =>
        new()
        {
            HomeopathicConceptId = symptom.HomeopathicConceptId,
            SubSectionId = source.SubSectionId,
            SubSectionName = source.SubSectionName,
            DiscoveryMethod = method,
            MatchReason = reason,
            Confidence = source.Confidence,
            QualityScore = source.QualityScore,
            RubricTier = source.RubricTier,
            EvidenceChain = source.EvidenceChain,
        };

    private static List<RubricDiscoveryNodeModel> MergeDiscoveryLists(
        params IEnumerable<RubricDiscoveryNodeModel>[] lists)
    {
        var map = new Dictionary<int, RubricDiscoveryNodeModel>();
        foreach (var list in lists)
        {
            foreach (var item in list)
            {
                if (item.SubSectionId <= 0)
                {
                    continue;
                }

                if (!map.TryGetValue(item.SubSectionId, out var existing)
                    || item.Confidence > existing.Confidence)
                {
                    map[item.SubSectionId] = item;
                }
            }
        }

        return map.Values.ToList();
    }

    private static void Upsert(
        Dictionary<int, RubricDiscoveryNodeModel> map,
        RubricDiscoveryNodeModel discovery,
        decimal symptomConfidence)
    {
        if (discovery.SubSectionId <= 0)
        {
            return;
        }

        discovery.Confidence = Math.Round(
            Math.Min(1m, discovery.Confidence * 0.85m + symptomConfidence * 0.15m),
            4);

        if (!map.TryGetValue(discovery.SubSectionId, out var existing)
            || discovery.Confidence > existing.Confidence)
        {
            map[discovery.SubSectionId] = discovery;
        }
    }

    private static decimal ScoreDiscovery(RubricDiscoveryNodeModel d, V6ClinicalSymptomUnit symptom) =>
        (d.Confidence * 0.45m)
        + (symptom.Confidence * 0.25m)
        + (SourcePriority(d.DiscoveryMethod) * 0.10m)
        + ((d.QualityScore ?? 0m) / 100m * 0.20m);

    private static int SourcePriority(string? method) =>
        method switch
        {
            RubricDiscoverySources.RepertoryDb => 6,
            RubricDiscoverySources.Bootstrap => 5,
            RubricDiscoverySources.KnowledgeGraph => 4,
            RubricDiscoverySources.DoctorLearning => 4,
            RubricDiscoverySources.EnterpriseEmbedding => 3,
            RubricDiscoverySources.Embedding => 2,
            _ => 1,
        };
}
