using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.V3.Engines;

namespace Niga_Domain.Services.AudioCaseIntelligence.Enterprise;

/// <summary>Phase 5: expand each concept to multiple clinically related repertory rubrics via bootstrap patterns.</summary>
public class EnterpriseRubricExpansionEngine
{
    private readonly NIGACentrumContext _context;
    private readonly RubricIntelligenceOptions _options;

    public EnterpriseRubricExpansionEngine(
        NIGACentrumContext context,
        IOptions<RubricIntelligenceOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<List<RubricDiscoveryNodeModel>> ExpandAsync(
        ConceptGraphFullModel graph,
        CancellationToken cancellationToken = default)
    {
        var mappings = await _context.AiConceptMappingBootstraps.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PriorityOrder)
            .Select(x => new AiConceptMappingBootstrapModel
            {
                HomeopathicConceptPattern = x.HomeopathicConceptPattern,
                SubSectionNamePattern = x.SubSectionNamePattern,
                Domain = x.Domain,
                PriorityOrder = x.PriorityOrder,
            })
            .ToListAsync(cancellationToken);

        if (mappings.Count == 0) return new List<RubricDiscoveryNodeModel>();

        var discoveries = new Dictionary<int, RubricDiscoveryNodeModel>();
        var maxPerConcept = Math.Max(_options.MaxRubricsPerPattern, 8);
        var patternCache = new Dictionary<string, List<(int SubSectionId, string SubSectionName)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var concept in graph.HomeopathicConcepts.Where(c => !string.IsNullOrWhiteSpace(c.ConceptName)))
        {
            var matched = mappings
                .Where(m => ConceptGraphTierHelper.ConceptNamesMatch(concept.ConceptName, m.HomeopathicConceptPattern))
                .ToList();

            foreach (var mapping in matched)
            {
                if (!patternCache.TryGetValue(mapping.SubSectionNamePattern, out var rubrics))
                {
                    rubrics = await FindRubricsByPatternAsync(mapping.SubSectionNamePattern, maxPerConcept, cancellationToken);
                    patternCache[mapping.SubSectionNamePattern] = rubrics;

                    foreach (var related in DeriveRelatedPatterns(mapping.SubSectionNamePattern))
                    {
                        if (patternCache.ContainsKey(related)) continue;
                        patternCache[related] = await FindRubricsByPatternAsync(related, maxPerConcept, cancellationToken);
                    }
                }

                foreach (var rubric in rubrics)
                    AddDiscovery(discoveries, concept, rubric.SubSectionId, rubric.SubSectionName, mapping, "RuleExpansion");

                foreach (var relatedPattern in DeriveRelatedPatterns(mapping.SubSectionNamePattern))
                {
                    if (!patternCache.TryGetValue(relatedPattern, out var relatedRubrics)) continue;
                    foreach (var rubric in relatedRubrics)
                        AddDiscovery(discoveries, concept, rubric.SubSectionId, rubric.SubSectionName, mapping, "RuleExpansionRelated");
                }
            }
        }

        return discoveries.Values
            .OrderByDescending(d => d.Confidence)
            .Take(_options.MaxDiscoveryCandidates)
            .ToList();
    }

    private static IEnumerable<string> DeriveRelatedPatterns(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) yield break;

        var normalized = pattern.Trim();
        if (normalized.Contains("CONVULS", StringComparison.OrdinalIgnoreCase)
            && normalized.Contains("AURA", StringComparison.OrdinalIgnoreCase))
        {
            yield return "%CONVULS%";
            yield return "%EPILEP%";
            yield return "%PREMONIT%";
        }

        if (normalized.Contains("FEAR", StringComparison.OrdinalIgnoreCase)
            && (normalized.Contains("FIT", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("CONVULS", StringComparison.OrdinalIgnoreCase)))
        {
            yield return "%FEAR%CONVULS%";
            yield return "%FEAR%FIT%";
            yield return "%ANXIETY%BEFORE%";
        }

        if (normalized.Contains("FORGET", StringComparison.OrdinalIgnoreCase))
            yield return "%FORGET%";

        if (normalized.Contains("DROPPING", StringComparison.OrdinalIgnoreCase))
            yield return "%DROP%THING%";
    }

    private void AddDiscovery(
        Dictionary<int, RubricDiscoveryNodeModel> map,
        HomeopathicConceptNodeModel concept,
        int subSectionId,
        string subSectionName,
        AiConceptMappingBootstrapModel mapping,
        string method)
    {
        var confidence = Math.Round(Math.Min(0.96m, 0.58m + concept.Confidence * 0.22m + concept.Weight * 0.04m), 4);
        if (map.TryGetValue(subSectionId, out var existing) && existing.Confidence >= confidence) return;

        map[subSectionId] = new RubricDiscoveryNodeModel
        {
            HomeopathicConceptId = concept.HomeopathicConceptId,
            SubSectionId = subSectionId,
            SubSectionName = subSectionName,
            MatchReason = $"Rule expansion: '{concept.ConceptName}' → pattern '{mapping.SubSectionNamePattern}'",
            DiscoveryMethod = method,
            Confidence = confidence,
            RubricTier = ConceptGraphTierHelper.ResolveTier(confidence),
        };
    }

    private async Task<List<(int SubSectionId, string SubSectionName)>> FindRubricsByPatternAsync(
        string pattern,
        int take,
        CancellationToken cancellationToken)
    {
        var likePattern = pattern.Replace('*', '%');
        if (!likePattern.Contains('%')) likePattern = $"%{likePattern}%";

        return await _context.SubSectionMasters.AsNoTracking()
            .Where(s => !s.DeleteStatus && s.SubSectionName != null
                && EF.Functions.Like(s.SubSectionName, likePattern))
            .OrderBy(s => s.SubSectionName)
            .Take(take)
            .Select(s => new ValueTuple<int, string>(s.SubSectionId, s.SubSectionName!))
            .ToListAsync(cancellationToken);
    }
}

public class PipelineDiagnosticService : IPipelineDiagnosticService
{
    private readonly IConceptGraphRepository _repository;

    public PipelineDiagnosticService(IConceptGraphRepository repository) => _repository = repository;

    public PipelineStageDiagnosticModel StartStage(string stageName) =>
        new() { StageName = stageName, Status = "PASS" };

    public void CompleteStage(PipelineStageDiagnosticModel stage, int outputCount, string? detail = null)
    {
        stage.OutputCount = outputCount;
        stage.Detail = detail;
        stage.Status = outputCount > 0 ? "PASS" : "WARN";
    }

    public void FailStage(PipelineStageDiagnosticModel stage, string error, int outputCount = 0)
    {
        stage.Status = "FAIL";
        stage.Error = error;
        stage.OutputCount = outputCount;
    }

    public async Task PersistAsync(
        Guid sessionId,
        PipelineDiagnosticReportModel report,
        CancellationToken cancellationToken = default)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(report);
        await _repository.SaveReasoningAuditAsync(sessionId, new AiReasoningAuditModel
        {
            PipelineStage = "PipelineDiagnosticV4",
            ModelId = report.EngineVersion,
            Success = string.IsNullOrWhiteSpace(report.FailureStage),
            ResponseJson = json,
            ErrorMessage = report.FailureReason,
        }, cancellationToken);

        foreach (var stage in report.Stages)
        {
            await _repository.SaveReasoningAuditAsync(sessionId, new AiReasoningAuditModel
            {
                PipelineStage = stage.StageName,
                ModelId = report.EngineVersion,
                Success = stage.Status != "FAIL",
                ErrorMessage = stage.Error,
                ResponseJson = System.Text.Json.JsonSerializer.Serialize(stage),
                LatencyMs = stage.DurationMs,
            }, cancellationToken);
        }
    }
}
