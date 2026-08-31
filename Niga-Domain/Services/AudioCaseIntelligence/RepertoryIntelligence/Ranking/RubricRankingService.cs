using Microsoft.Extensions.Options;
using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Learning;

namespace Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Ranking;

/// <summary>V7: weighted rubric ranking — not cosine similarity alone.</summary>
public interface IRubricRankingService
{
    List<V7RubricCandidate> RankCandidates(
        IReadOnlyList<V7RubricCandidate> candidates,
        V7ExtractedSymptom symptom,
        IReadOnlyDictionary<string, decimal>? doctorWeights = null);

    List<RubricDiscoveryNodeModel> ToDiscoveries(
        IReadOnlyList<V7RubricCandidate> ranked,
        V7ExtractedSymptom symptom);
}

public class RubricRankingService : IRubricRankingService
{
    private readonly RubricIntelligenceOptions _options;

    public RubricRankingService(IOptions<RubricIntelligenceOptions> options)
    {
        _options = options.Value;
    }

    public List<V7RubricCandidate> RankCandidates(
        IReadOnlyList<V7RubricCandidate> candidates,
        V7ExtractedSymptom symptom,
        IReadOnlyDictionary<string, decimal>? doctorWeights = null)
    {
        var maxRaw = candidates.Count > 0 ? candidates.Max(c => c.RawScore) : 100m;
        if (maxRaw <= 0)
        {
            maxRaw = 100m;
        }

        return candidates
            .Select(c =>
            {
                var score = c.RawScore;

                if (_options.EnableDoctorLearningEngine
                    && doctorWeights != null
                    && !string.IsNullOrWhiteSpace(c.MatchedTerm)
                    && doctorWeights.TryGetValue(c.MatchedTerm, out var weight))
                {
                    score += 10m * Math.Min(weight, 1m);
                }

                score += (symptom.Confidence / 100m) * 10m;

                c.NormalizedScore = Math.Round(score / maxRaw * 100m, 2);
                return c;
            })
            .OrderByDescending(c => c.RawScore)
            .ThenByDescending(c => c.EmbeddingSimilarity)
            .ThenByDescending(c => c.NormalizedScore)
            .ToList();
    }

    public List<RubricDiscoveryNodeModel> ToDiscoveries(
        IReadOnlyList<V7RubricCandidate> ranked,
        V7ExtractedSymptom symptom)
    {
        return ranked.Select(c => new RubricDiscoveryNodeModel
        {
            HomeopathicConceptId = symptom.SourceConceptId,
            SubSectionId = c.SubSectionId,
            SubSectionName = c.SubSectionName,
            DiscoveryMethod = RubricDiscoverySources.RepertoryDb,
            MatchReason = $"{c.SearchStrategy}: {c.MatchPath}",
            Confidence = Math.Min(1m, c.NormalizedScore / 100m),
            QualityScore = c.NormalizedScore,
            RubricTier = c.NormalizedScore >= 75m ? "Primary" : c.NormalizedScore >= 55m ? "Secondary" : "Supporting",
        }).ToList();
    }
}
