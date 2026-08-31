using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories;

public class AudioCaseIntelligenceRepository : IAudioCaseIntelligenceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly NIGACentrumContext _context;
    private readonly ILogger<AudioCaseIntelligenceRepository> _logger;

    public AudioCaseIntelligenceRepository(
        NIGACentrumContext context,
        ILogger<AudioCaseIntelligenceRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SaveConceptsAsync(
        Guid sessionId,
        IReadOnlyList<ClinicalConceptModel> concepts,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.AudioCaseClinicalConcepts
                .Where(x => x.AudioCaseSessionId == sessionId)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
            {
                _context.AudioCaseClinicalConcepts.RemoveRange(existing);
            }

            foreach (var concept in concepts)
            {
                var modalities = concept.Modalities.ToList();
                if (concept.IsAmbiguous && !modalities.Contains("ambiguous", StringComparer.OrdinalIgnoreCase))
                    modalities.Add("ambiguous");

                _context.AudioCaseClinicalConcepts.Add(new AudioCaseClinicalConcept
                {
                    ConceptId = concept.ConceptId == Guid.Empty ? Guid.NewGuid() : concept.ConceptId,
                    AudioCaseSessionId = sessionId,
                    RawStatement = concept.RawStatement,
                    ClinicalMeaning = concept.ClinicalMeaning,
                    HomeopathicMeaning = concept.HomeopathicMeaning,
                    Category = concept.Category,
                    IsSRP = concept.IsSRP,
                    ModalitiesJson = modalities.Count > 0 ? JsonSerializer.Serialize(modalities, JsonOptions) : null,
                    ConcomitantsJson = concept.Concomitants.Count > 0 ? JsonSerializer.Serialize(concept.Concomitants, JsonOptions) : null,
                    Confidence = concept.Confidence,
                    SourceLanguage = concept.SourceLanguage,
                    EnteredDate = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist clinical concepts to DB for session {SessionId}. Ensure Phase 1 SQL scripts are deployed.", sessionId);
        }
    }

    public async Task SaveIntelligenceLogAsync(
        Guid sessionId,
        string? correlationId,
        string stageName,
        string status,
        string? message,
        string? detailsJson,
        int? latencyMs,
        CancellationToken cancellationToken = default,
        string engineVersion = "v2")
    {
        AudioCaseIntelligenceLog? row = null;
        try
        {
            row = new AudioCaseIntelligenceLog
            {
                AudioCaseSessionId = sessionId,
                CorrelationId = Truncate(correlationId, 32),
                StageName = Truncate(stageName, 100) ?? "Unknown",
                Status = Truncate(status, 30) ?? "Success",
                Message = Truncate(message, 2000),
                DetailsJson = detailsJson,
                LatencyMs = latencyMs,
                // DB may still be NVARCHAR(10) until script 728; keep writes safe either way.
                EngineVersion = Truncate(
                    string.IsNullOrWhiteSpace(engineVersion) ? "v2" : engineVersion,
                    10) ?? "v2",
                EnteredDate = DateTime.UtcNow,
            };

            _context.AudioCaseIntelligenceLogs.Add(row);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Detach so a telemetry truncation/FK failure cannot poison later session SaveChanges.
            if (row != null)
            {
                var entry = _context.Entry(row);
                if (entry.State != EntityState.Detached)
                    entry.State = EntityState.Detached;
            }

            _logger.LogWarning(ex, "Could not persist intelligence log for session {SessionId}, stage {StageName}.", sessionId, stageName);
        }
    }

    private static string? Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLen ? value : value[..maxLen];
    }

    public async Task<List<ClinicalConceptModel>> GetConceptsAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _context.AudioCaseClinicalConcepts
                .AsNoTracking()
                .Where(x => x.AudioCaseSessionId == sessionId)
                .OrderBy(x => x.EnteredDate)
                .ToListAsync(cancellationToken);

            return rows.Select(MapConcept).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read clinical concepts from DB for session {SessionId}.", sessionId);
            return new List<ClinicalConceptModel>();
        }
    }

    public async Task SaveCausationLinksAsync(
        Guid sessionId,
        IReadOnlyList<CausationLinkModel> links,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.AudioCaseSessions
                .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId, cancellationToken);

            if (session != null)
            {
                session.CausationLinksJson = links.Count > 0
                    ? JsonSerializer.Serialize(links, JsonOptions)
                    : null;
                session.ChangedDate = DateTime.UtcNow;
            }

            var existing = await _context.AudioCaseCausationLinks
                .Where(x => x.AudioCaseSessionId == sessionId)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
            {
                _context.AudioCaseCausationLinks.RemoveRange(existing);
            }

            foreach (var link in links)
            {
                _context.AudioCaseCausationLinks.Add(new AudioCaseCausationLink
                {
                    CausationLinkId = link.LinkId == Guid.Empty ? Guid.NewGuid() : link.LinkId,
                    AudioCaseSessionId = sessionId,
                    CauseConceptId = link.CauseConceptId,
                    EffectConceptId = link.EffectConceptId,
                    CauseText = link.CauseText,
                    EffectText = link.EffectText,
                    LinkType = link.LinkType,
                    Confidence = link.Confidence,
                    SequenceOrder = link.SequenceOrder,
                    EnteredDate = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist causation links for session {SessionId}. Ensure Phase 3 SQL scripts are deployed.", sessionId);
        }
    }

    public async Task<List<CausationLinkModel>> GetCausationLinksAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _context.AudioCaseSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AudioCaseSessionId == sessionId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(session?.CausationLinksJson))
            {
                return JsonSerializer.Deserialize<List<CausationLinkModel>>(session.CausationLinksJson, JsonOptions)
                    ?? new List<CausationLinkModel>();
            }

            var rows = await _context.AudioCaseCausationLinks
                .AsNoTracking()
                .Where(x => x.AudioCaseSessionId == sessionId)
                .OrderBy(x => x.SequenceOrder)
                .ToListAsync(cancellationToken);

            return rows.Select(MapCausationLink).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read causation links for session {SessionId}.", sessionId);
            return new List<CausationLinkModel>();
        }
    }

    public async Task<List<HomeopathicWeightRule>> GetActiveWeightRulesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _context.HomeopathicWeightRules
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            return rows;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read homeopathic weight rules. Ensure Phase 3 SQL scripts are deployed.");
            return new List<HomeopathicWeightRule>();
        }
    }

    public static CausationLinkModel MapCausationLink(AudioCaseCausationLink row) => new()
    {
        LinkId = row.CausationLinkId,
        CauseConceptId = row.CauseConceptId,
        EffectConceptId = row.EffectConceptId,
        CauseText = row.CauseText,
        EffectText = row.EffectText,
        LinkType = row.LinkType,
        Confidence = row.Confidence,
        SequenceOrder = row.SequenceOrder,
    };

    public async Task SaveInferenceLogsAsync(
        Guid sessionId,
        IReadOnlyList<ClinicalInferenceLogModel> logs,
        CancellationToken cancellationToken = default)
    {
        if (logs.Count == 0) return;

        try
        {
            var existing = await _context.AudioCaseClinicalInferenceLogs
                .Where(x => x.AudioCaseSessionId == sessionId)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
            {
                _context.AudioCaseClinicalInferenceLogs.RemoveRange(existing);
            }

            foreach (var log in logs)
            {
                _context.AudioCaseClinicalInferenceLogs.Add(new AudioCaseClinicalInferenceLog
                {
                    AudioCaseSessionId = sessionId,
                    SourceConceptId = log.SourceConceptId,
                    InferredRubricName = log.InferredRubricName,
                    SubSectionId = log.SubSectionId,
                    Reason = log.Reason,
                    SourceSymptom = log.SourceSymptom,
                    Confidence = log.Confidence,
                    EnteredDate = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist inference logs for session {SessionId}. Ensure Phase 5 SQL script 008 is deployed.", sessionId);
        }
    }

    public static ClinicalConceptModel MapConcept(AudioCaseClinicalConcept row)
    {
        return new ClinicalConceptModel
        {
            ConceptId = row.ConceptId,
            RawStatement = row.RawStatement,
            ClinicalMeaning = row.ClinicalMeaning,
            HomeopathicMeaning = row.HomeopathicMeaning,
            Category = row.Category,
            IsSRP = row.IsSRP,
            Modalities = DeserializeList(row.ModalitiesJson),
            Concomitants = DeserializeList(row.ConcomitantsJson),
            Confidence = row.Confidence,
            SourceLanguage = row.SourceLanguage,
        };
    }

    private static List<string> DeserializeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? new List<string>();
    }
}
