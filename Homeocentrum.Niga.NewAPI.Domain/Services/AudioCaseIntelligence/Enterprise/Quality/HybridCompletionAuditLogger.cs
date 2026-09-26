using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Enterprise.Quality;

public interface IHybridCompletionAuditLogger
{
    void LogConceptPipeline(Guid sessionId, HybridConceptAuditEntry entry);

    void LogFinalizeSummary(Guid sessionId, HybridCompletionOutput output);
}

public class HybridCompletionAuditLogger : IHybridCompletionAuditLogger
{
    private readonly ILogger<HybridCompletionAuditLogger> _logger;

    public HybridCompletionAuditLogger(ILogger<HybridCompletionAuditLogger> logger)
    {
        _logger = logger;
    }

    public void LogConceptPipeline(Guid sessionId, HybridConceptAuditEntry entry)
    {
        var stageSummary = string.Join(
            " | ",
            entry.Stages.Select(s => $"{s.StageName}={s.MatchCount}[{string.Join(',', s.SubSectionIds.Take(5))}]"));

        _logger.LogInformation(
            "HybridCompletionV52 Session={SessionId} Concept={ConceptId}|{ConceptName} Confidence={Confidence:0.00} " +
            "DB={DbCount} KG={KgCount} Embed={EmbedCount} Slots={Slots} Output={OutputCategory} Stages: {Stages}",
            sessionId,
            entry.HomeopathicConceptId,
            entry.ConceptName,
            entry.ConceptConfidence,
            entry.DatabaseMatchCount,
            entry.KnowledgeGraphMatchCount,
            entry.EmbeddingMatchCount,
            entry.SlotsAllocated,
            entry.OutputCategory,
            stageSummary);
    }

    public void LogFinalizeSummary(Guid sessionId, HybridCompletionOutput output)
    {
        _logger.LogInformation(
            "HybridCompletionV52 Session={SessionId} Finalize repertory={RepertoryCount} aiConcepts={AiConceptCount} " +
            "conceptsMapped={Mapped} conceptsUnmapped={Unmapped}",
            sessionId,
            output.RepertoryRubrics.Count,
            output.AiClinicalConcepts.Count,
            output.AuditEntries.Count(e =>
                string.Equals(e.OutputCategory, HybridCompletionOutputCategories.RepertoryDatabase, StringComparison.OrdinalIgnoreCase)),
            output.AuditEntries.Count(e =>
                string.Equals(e.OutputCategory, HybridCompletionOutputCategories.AiClinicalConcept, StringComparison.OrdinalIgnoreCase)));
    }
}
