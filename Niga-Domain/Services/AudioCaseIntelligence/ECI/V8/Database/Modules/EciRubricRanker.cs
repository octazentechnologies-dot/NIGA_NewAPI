using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 10: deterministic scoring engine (configurable weights).</summary>
public sealed class EciRubricRanker : IEciRubricRanker
{
    private readonly RubricIntelligenceOptions _options;
    private readonly ILogger<EciRubricRanker> _logger;

    public EciRubricRanker(IOptions<RubricIntelligenceOptions> options, ILogger<EciRubricRanker> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public List<EciCandidateRubric> Rank(
        EciValidatedSymptom symptom,
        IReadOnlyList<EciCandidateRubric> candidates,
        string cleanTranscript)
    {
        var weights = _options.EciV8RankingWeights;

        foreach (var c in candidates)
        {
            // Clinical match: derived from provenance strength and presence of exact signals.
            c.ClinicalMatchScore = ComputeClinicalMatch(c);

            // Evidence: symptom already has evidence span; score high when present.
            c.EvidenceScore = string.IsNullOrWhiteSpace(symptom.Symptom.Evidence) ? 0m : 1m;

            // SQL exact: already set in ExactSql module.
            // Ontology/Embedding/Hierarchy/Historical: already set in modules.
            c.PenaltyScore = 0m;

            c.FinalScore =
                (c.ClinicalMatchScore * weights.ClinicalMatch)
                + (c.EvidenceScore * weights.Evidence)
                + (c.OntologyScore * weights.Ontology)
                + (c.EmbeddingScore * weights.Embedding)
                + (c.HierarchyScore * weights.Hierarchy)
                + (c.SqlExactScore * weights.SqlExact)
                + (c.HistoricalScore * weights.Historical)
                + (c.ExpertRulesScore * weights.ExpertRules)
                + c.PenaltyScore;

            c.FinalScore = Math.Round(c.FinalScore, 3);
        }

        return candidates
            .OrderByDescending(c => c.FinalScore)
            .ThenByDescending(c => c.SqlExactScore)
            .ThenByDescending(c => c.EmbeddingScore)
            .ThenBy(c => c.SubSectionName?.Length ?? int.MaxValue)
            .ToList();
    }

    private static decimal ComputeClinicalMatch(EciCandidateRubric c)
    {
        if (c.Provenance.Any(p => p.Source == EciCandidateSources.ExactSql))
        {
            return 1.0m;
        }

        var best = c.Provenance.Count == 0 ? 0m : c.Provenance.Max(p => p.Confidence);
        return Math.Clamp(best, 0m, 1m);
    }
}

