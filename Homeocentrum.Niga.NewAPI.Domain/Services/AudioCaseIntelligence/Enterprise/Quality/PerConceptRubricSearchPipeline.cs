using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

/// <summary>V5.2: per-concept candidate search — exact → synonym → bootstrap → KG → embedding.</summary>
public class PerConceptRubricSearchPipeline
{
    private readonly IHierarchicalRepertorySearchEngine _repertorySearch;

    public PerConceptRubricSearchPipeline(IHierarchicalRepertorySearchEngine repertorySearch)
    {
        _repertorySearch = repertorySearch;
    }

    public async Task<(List<RubricDiscoveryNodeModel> Discoveries, HybridConceptAuditEntry Audit)> SearchAsync(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        PatientMeaningNodeModel? meaning,
        HybridCompletionRequest request,
        RubricIntelligenceOptions options,
        CancellationToken cancellationToken = default)
    {
        var audit = new HybridConceptAuditEntry
        {
            HomeopathicConceptId = homeo.HomeopathicConceptId,
            ConceptName = homeo.ConceptName,
            ConceptConfidence = homeo.Confidence,
        };

        var stageMap = new Dictionary<int, RubricDiscoveryNodeModel>();
        var slotsPerConcept = Math.Max(2, options.MaxRubricsPerConceptSlots);

        var dbMatches = await _repertorySearch.DiscoverForConceptAsync(
            homeo, clinical, meaning, cancellationToken);

        var exactIds = dbMatches
            .Where(d => d.MatchReason?.Contains("Exact repertory", StringComparison.OrdinalIgnoreCase) == true)
            .Select(d => d.SubSectionId)
            .ToList();
        audit.Stages.Add(new HybridCompletionStageResult
        {
            StageName = HybridCompletionStageNames.ExactRepertory,
            MatchCount = exactIds.Count,
            SubSectionIds = exactIds,
        });

        var synonymIds = dbMatches
            .Where(d => d.MatchReason?.Contains("hotspot", StringComparison.OrdinalIgnoreCase) == true)
            .Select(d => d.SubSectionId)
            .ToList();
        audit.Stages.Add(new HybridCompletionStageResult
        {
            StageName = HybridCompletionStageNames.SynonymSearch,
            MatchCount = synonymIds.Count,
            SubSectionIds = synonymIds,
        });

        var bootstrapIds = dbMatches
            .Where(d => d.MatchReason?.Contains("bootstrap", StringComparison.OrdinalIgnoreCase) == true)
            .Select(d => d.SubSectionId)
            .ToList();
        audit.Stages.Add(new HybridCompletionStageResult
        {
            StageName = HybridCompletionStageNames.BootstrapMapping,
            MatchCount = bootstrapIds.Count,
            SubSectionIds = bootstrapIds,
        });

        foreach (var discovery in dbMatches)
        {
            Upsert(stageMap, discovery, homeo.Confidence);
        }

        audit.DatabaseMatchCount = dbMatches.Count;

        var kgMatches = CollectKnowledgeGraphMatches(homeo, clinical, request);
        audit.KnowledgeGraphMatchCount = kgMatches.Count;
        audit.Stages.Add(new HybridCompletionStageResult
        {
            StageName = HybridCompletionStageNames.KnowledgeGraph,
            MatchCount = kgMatches.Count,
            SubSectionIds = kgMatches.Select(d => d.SubSectionId).ToList(),
        });
        foreach (var discovery in kgMatches)
        {
            Upsert(stageMap, discovery, homeo.Confidence);
        }

        var embeddingMatches = CollectEmbeddingMatches(homeo, request)
            .OrderByDescending(d => d.Confidence)
            .Take(Math.Max(slotsPerConcept * 3, 12))
            .ToList();
        audit.EmbeddingMatchCount = embeddingMatches.Count;
        audit.Stages.Add(new HybridCompletionStageResult
        {
            StageName = HybridCompletionStageNames.EmbeddingSimilarity,
            MatchCount = embeddingMatches.Count,
            SubSectionIds = embeddingMatches.Select(d => d.SubSectionId).ToList(),
        });
        foreach (var discovery in embeddingMatches)
        {
            Upsert(stageMap, discovery, homeo.Confidence);
        }

        var ranked = stageMap.Values
            .Select(d => (Discovery: d, Score: ScoreDiscovery(d, homeo)))
            .OrderByDescending(x => SourcePriority(x.Discovery.DiscoveryMethod))
            .ThenByDescending(x => x.Score)
            .Take(slotsPerConcept)
            .Select(x => x.Discovery)
            .ToList();

        audit.SlotsAllocated = ranked.Count;
        audit.OutputCategory = ranked.Count > 0
            ? HybridCompletionOutputCategories.RepertoryDatabase
            : HybridCompletionOutputCategories.AiClinicalConcept;

        return (ranked, audit);
    }

    private static List<RubricDiscoveryNodeModel> CollectKnowledgeGraphMatches(
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical,
        HybridCompletionRequest request)
    {
        var fromDiscoveries = request.GlobalDiscoveries
            .Where(d => d.SubSectionId > 0
                && (d.HomeopathicConceptId == homeo.HomeopathicConceptId
                    || ConceptBelongsToHomeo(d, homeo, clinical)))
            .Where(d => string.Equals(d.DiscoveryMethod, RubricDiscoverySources.KnowledgeGraph, StringComparison.OrdinalIgnoreCase)
                || (d.MatchReason?.Contains("Knowledge graph", StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(d => CloneForConcept(d, homeo, RubricDiscoverySources.KnowledgeGraph,
                d.MatchReason ?? "Knowledge graph path"))
            .ToList();

        var fromPaths = request.KnowledgeGraphPaths
            .Where(p => p.SubSectionId > 0
                && PathMatchesConcept(p, homeo, clinical))
            .Select(p => new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = homeo.HomeopathicConceptId,
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
        HomeopathicConceptNodeModel homeo,
        HybridCompletionRequest request)
    {
        var fromDiscoveries = request.GlobalDiscoveries
            .Where(d => d.SubSectionId > 0 && d.HomeopathicConceptId == homeo.HomeopathicConceptId)
            .Where(d => string.Equals(d.DiscoveryMethod, RubricDiscoverySources.EnterpriseEmbedding, StringComparison.OrdinalIgnoreCase)
                || string.Equals(d.DiscoveryMethod, RubricDiscoverySources.Embedding, StringComparison.OrdinalIgnoreCase)
                || (d.DiscoveryMethod?.Contains("Embedding", StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        var fromCandidates = request.EmbeddingCandidates
            .Where(c => c.SubSectionId > 0
                && (c.HomeopathicConceptId == homeo.HomeopathicConceptId
                    || string.Equals(c.SourceConceptName, homeo.ConceptName, StringComparison.OrdinalIgnoreCase)))
            .Select(c => new RubricDiscoveryNodeModel
            {
                HomeopathicConceptId = homeo.HomeopathicConceptId,
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

    private static bool ConceptBelongsToHomeo(
        RubricDiscoveryNodeModel discovery,
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical)
    {
        if (discovery.HomeopathicConceptId == homeo.HomeopathicConceptId)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(discovery.MatchReason)
            && discovery.MatchReason.Contains(homeo.ConceptName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathMatchesConcept(
        KgRubricPathModel path,
        HomeopathicConceptNodeModel homeo,
        ClinicalConceptNodeModel? clinical)
    {
        var pathText = string.Join(' ', path.Steps.Select(s => s.DisplayText));
        if (pathText.Contains(homeo.ConceptName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return clinical != null
            && pathText.Contains(clinical.ConceptName, StringComparison.OrdinalIgnoreCase);
    }

    private static RubricDiscoveryNodeModel CloneForConcept(
        RubricDiscoveryNodeModel source,
        HomeopathicConceptNodeModel homeo,
        string method,
        string reason) =>
        new()
        {
            HomeopathicConceptId = homeo.HomeopathicConceptId,
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
        decimal conceptConfidence)
    {
        if (discovery.SubSectionId <= 0)
        {
            return;
        }

        discovery.Confidence = Math.Round(
            Math.Min(1m, discovery.Confidence * 0.85m + conceptConfidence * 0.15m),
            4);

        if (!map.TryGetValue(discovery.SubSectionId, out var existing)
            || discovery.Confidence > existing.Confidence)
        {
            map[discovery.SubSectionId] = discovery;
        }
    }

    private static decimal ScoreDiscovery(RubricDiscoveryNodeModel d, HomeopathicConceptNodeModel homeo) =>
        (d.Confidence * 0.45m)
        + (homeo.Confidence * 0.25m)
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
