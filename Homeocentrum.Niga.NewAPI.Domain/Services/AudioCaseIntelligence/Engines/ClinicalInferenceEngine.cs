using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Engines;

public class ClinicalInferenceEngine : IClinicalInferenceEngine
{
    private readonly RubricIntelligenceOptions _options;
    private readonly IEmbeddingSearchEngine _embeddingSearchEngine;

    public ClinicalInferenceEngine(
        IOptions<RubricIntelligenceOptions> options,
        IEmbeddingSearchEngine embeddingSearchEngine)
    {
        _options = options.Value;
        _embeddingSearchEngine = embeddingSearchEngine;
    }

    public async Task<ClinicalInferenceResult> InferAsync(
        IReadOnlyList<ClinicalConceptModel> concepts,
        IReadOnlyList<CausationLinkModel> causationLinks,
        IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics,
        CancellationToken cancellationToken = default)
    {
        var result = new ClinicalInferenceResult();
        if (!_options.EnableClinicalInference || concepts.Count == 0)
        {
            return result;
        }

        var existingIds = existingRubrics
            .Where(r => r.SubSectionId > 0)
            .Select(r => r.SubSectionId)
            .ToHashSet();

        foreach (var concept in concepts.Where(c => c.Confidence >= _options.MinConceptConfidenceForInference))
        {
            if (HasStrongRetrievalMatch(concept, existingRubrics))
            {
                continue;
            }

            var embeddingResult = await _embeddingSearchEngine.SearchAsync(new[] { concept }, cancellationToken);
            var neighbor = embeddingResult.Candidates
                .Where(c => c.CosineScore >= _options.MinEmbeddingNeighborForInference)
                .OrderByDescending(c => c.CosineScore)
                .FirstOrDefault(c => !existingIds.Contains(c.SubSectionId));

            if (neighbor == null)
            {
                continue;
            }

            var reason = BuildReason(concept, neighbor, causationLinks);
            var rubric = new AudioCaseSuggestedRubricModel
            {
                SubSectionId = neighbor.SubSectionId,
                SubSectionName = neighbor.SubSectionName,
                MatchScore = Math.Round(neighbor.CosineScore * 0.9m, 4),
                ConfidenceScore = neighbor.CosineScore,
                SuggestedIntensityNo = 2,
                MatchedFrom = concept.RawStatement,
                MatchSource = "Inference",
                MatchLayer = "Inference",
                RubricTier = "Inference",
                RequiresDoctorReview = true,
                RequiresManualApproval = true,
                IsAiSuggested = false,
                SourceConceptId = concept.ConceptId,
                InferenceReason = reason,
                WhySuggested = reason,
                EngineVersion = "v2",
            };

            result.Rubrics.Add(rubric);
            existingIds.Add(neighbor.SubSectionId);
            result.Logs.Add(new ClinicalInferenceLogModel
            {
                SourceConceptId = concept.ConceptId,
                InferredRubricName = neighbor.SubSectionName,
                SubSectionId = neighbor.SubSectionId,
                Reason = reason,
                SourceSymptom = concept.RawStatement,
                Confidence = neighbor.CosineScore,
            });
        }

        return result;
    }

    private bool HasStrongRetrievalMatch(
        ClinicalConceptModel concept,
        IReadOnlyList<AudioCaseSuggestedRubricModel> existingRubrics)
    {
        return existingRubrics.Any(r =>
            r.SubSectionId > 0
            && ConceptMatchesRubric(concept, r)
            && (r.ConfidenceScore ?? r.MatchScore) >= _options.MinRetrievalScoreToSkipInference);
    }

    private static bool ConceptMatchesRubric(ClinicalConceptModel concept, AudioCaseSuggestedRubricModel rubric)
    {
        var rubricText = $"{rubric.MatchedFrom} {rubric.SubSectionName}".ToLowerInvariant();
        var terms = concept.SearchTerms
            .Concat(new[] { concept.RawStatement, concept.ClinicalMeaning ?? string.Empty, concept.HomeopathicMeaning ?? string.Empty })
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return terms.Any(term =>
            rubricText.Contains(term.ToLowerInvariant(), StringComparison.Ordinal)
            || term.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(token => token.Length > 3 && rubricText.Contains(token.ToLowerInvariant(), StringComparison.Ordinal)));
    }

    private static string BuildReason(
        ClinicalConceptModel concept,
        EmbeddingSearchCandidate neighbor,
        IReadOnlyList<CausationLinkModel> causationLinks)
    {
        var clinical = concept.ClinicalMeaning ?? concept.RawStatement;
        var causation = causationLinks
            .FirstOrDefault(l => l.EffectConceptId == concept.ConceptId || l.CauseConceptId == concept.ConceptId);

        if (causation != null)
        {
            return $"Inferred '{neighbor.SubSectionName}' from concept '{clinical}' in causation chain ({causation.CauseText} → {causation.EffectText}); embedding neighbor score {neighbor.CosineScore:0.00}.";
        }

        return $"Inferred '{neighbor.SubSectionName}' from high-confidence concept '{clinical}' with embedding neighbor score {neighbor.CosineScore:0.00}; no retrieval match ≥ threshold.";
    }
}
