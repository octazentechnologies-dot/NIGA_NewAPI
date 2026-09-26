using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.V3.Engines;

public interface ISensationOntologyService
{
    /// <summary>Scan SubSectionMaster (read-only) and upsert AISensationOntology rows. Returns insert/update counts.</summary>
    Task<(int Extracted, int ActiveTotal)> RebuildFromSubSectionMasterAsync(CancellationToken cancellationToken = default);

    Task<SensationOntologyMatch?> FindBestMatchAsync(
        string clinicalMeaning,
        string? expression,
        CancellationToken cancellationToken = default);

    Task GroundMetaphorResolutionsAsync(
        IList<MetaphorResolutionNodeModel> metaphors,
        CancellationToken cancellationToken = default);
}

public sealed class SensationOntologyMatch
{
    public long OntologyId { get; init; }

    public string Pattern { get; init; } = string.Empty;

    public int SubSectionId { get; init; }

    public string? SubSectionName { get; init; }

    public string SensationCategory { get; init; } = string.Empty;

    public decimal Score { get; init; }
}

/// <summary>
/// Task 3 — repertory-grounded sensation/causation ontology extracted from SubSectionMaster names.
/// Does not modify SubSectionMaster.
/// </summary>
public sealed class SensationOntologyService : ISensationOntologyService
{
    private static readonly (string Pattern, string Category)[] ControlledPatterns =
    [
        ("as if", "SensationAsIf"),
        ("sensation of", "SensationOf"),
        ("as from", "AsFrom"),
        ("as though", "AsThough"),
        ("after fright", "Causation"),
        ("from grief", "Causation"),
        ("after anger", "Causation"),
        ("agg.", "ModalityAgg"),
        ("amel.", "ModalityAmel"),
    ];

    private readonly NIGACentrumContext _context;
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<SensationOntologyService> _logger;

    public SensationOntologyService(
        NIGACentrumContext context,
        IOptions<RubricIntelligenceOptions> options,
        ILogger<SensationOntologyService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(int Extracted, int ActiveTotal)> RebuildFromSubSectionMasterAsync(
        CancellationToken cancellationToken = default)
    {
        var names = await _context.SubSectionMasters
            .AsNoTracking()
            .Where(s => s.DeleteStatus == false && s.SubSectionName != null && s.SubSectionName != "")
            .Select(s => new { s.SubSectionId, Name = s.SubSectionName! })
            .ToListAsync(cancellationToken);

        var extracted = 0;
        foreach (var row in names)
        {
            var lower = row.Name.ToLowerInvariant();
            foreach (var (pattern, category) in ControlledPatterns)
            {
                if (!lower.Contains(pattern, StringComparison.Ordinal))
                    continue;

                var exists = await _context.AiSensationOntologies.AnyAsync(
                    o => o.SubSectionId == row.SubSectionId
                         && o.Pattern == pattern
                         && o.SensationCategory == category,
                    cancellationToken);
                if (exists)
                    continue;

                _context.AiSensationOntologies.Add(new AiSensationOntology
                {
                    Pattern = pattern,
                    SubSectionId = row.SubSectionId,
                    SubSectionName = row.Name,
                    SensationCategory = category,
                    ExtractedAt = DateTime.UtcNow,
                    IsActive = true,
                });
                extracted++;
            }
        }

        if (extracted > 0)
            await _context.SaveChangesAsync(cancellationToken);

        var activeTotal = await _context.AiSensationOntologies.CountAsync(o => o.IsActive, cancellationToken);
        _logger.LogInformation(
            "AISensationOntology rebuild complete. NewlyExtracted={Extracted}, ActiveTotal={ActiveTotal}, ScannedRubrics={Scanned}",
            extracted, activeTotal, names.Count);

        return (extracted, activeTotal);
    }

    public async Task<SensationOntologyMatch?> FindBestMatchAsync(
        string clinicalMeaning,
        string? expression,
        CancellationToken cancellationToken = default)
    {
        var haystack = $"{clinicalMeaning} {expression}".Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(haystack))
            return null;

        var candidates = await _context.AiSensationOntologies
            .AsNoTracking()
            .Where(o => o.IsActive)
            .Take(5000)
            .ToListAsync(cancellationToken);

        SensationOntologyMatch? best = null;
        foreach (var o in candidates)
        {
            var name = (o.SubSectionName ?? string.Empty).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var score = ScoreOverlap(haystack, name, o.Pattern);
            if (score < _options.OntologyMatchMinConfidence)
                continue;

            if (best == null || score > best.Score)
            {
                best = new SensationOntologyMatch
                {
                    OntologyId = o.OntologyId,
                    Pattern = o.Pattern,
                    SubSectionId = o.SubSectionId,
                    SubSectionName = o.SubSectionName,
                    SensationCategory = o.SensationCategory,
                    Score = score,
                };
            }
        }

        return best;
    }

    public async Task GroundMetaphorResolutionsAsync(
        IList<MetaphorResolutionNodeModel> metaphors,
        CancellationToken cancellationToken = default)
    {
        foreach (var meta in metaphors)
        {
            if (!meta.IsMetaphor)
            {
                meta.GroundedInOntology = false;
                continue;
            }

            var match = await FindBestMatchAsync(meta.ClinicalMeaning, meta.Expression, cancellationToken);
            if (match == null)
            {
                meta.GroundedInOntology = false;
                continue;
            }

            // Prefer repertory-grounded clinical wording when ontology match is strong.
            meta.ClinicalMeaning = match.SubSectionName ?? meta.ClinicalMeaning;
            meta.Confidence = Math.Max(meta.Confidence, match.Score);
            meta.GroundedInOntology = true;
            meta.OntologyId = match.OntologyId;
        }
    }

    private static decimal ScoreOverlap(string haystack, string rubricName, string pattern)
    {
        if (!haystack.Contains(pattern, StringComparison.Ordinal)
            && !rubricName.Contains(pattern, StringComparison.Ordinal))
        {
            // Still allow token overlap when clinical meaning paraphrases the rubric.
        }

        var hayTokens = Tokenize(haystack);
        var rubricTokens = Tokenize(rubricName);
        if (hayTokens.Count == 0 || rubricTokens.Count == 0)
            return 0m;

        var overlap = hayTokens.Intersect(rubricTokens, StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = (decimal)overlap / Math.Max(1, hayTokens.Union(rubricTokens, StringComparer.OrdinalIgnoreCase).Count());
        var patternBoost = haystack.Contains(pattern, StringComparison.Ordinal) || rubricName.Contains(pattern, StringComparison.Ordinal)
            ? 0.35m
            : 0m;
        return Math.Min(1m, jaccard + patternBoost);
    }

    private static HashSet<string> Tokenize(string text)
    {
        return Regex.Split(text.ToLowerInvariant(), @"[^a-z0-9]+")
            .Where(t => t.Length >= 3)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
