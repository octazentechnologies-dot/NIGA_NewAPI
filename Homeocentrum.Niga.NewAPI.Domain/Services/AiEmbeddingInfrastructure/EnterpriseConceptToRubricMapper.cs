using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AiEmbeddingInfrastructure;

public class EnterpriseConceptToRubricMapper
{
    private readonly NIGACentrumContext _context;
    private readonly AiEmbeddingInfrastructureOptions _options;
    private readonly ILogger<EnterpriseConceptToRubricMapper> _logger;

    public EnterpriseConceptToRubricMapper(
        NIGACentrumContext context,
        IOptions<AiEmbeddingInfrastructureOptions> options,
        ILogger<EnterpriseConceptToRubricMapper> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<EnterpriseSemanticRubricMatchModel>> MapAsync(
        IReadOnlyList<float> queryVector,
        IReadOnlyList<EnterpriseSemanticConceptMatchModel> concepts,
        IReadOnlyList<AiEnterpriseRubricEmbeddingCacheEntry> rubricEntries,
        bool includeValidation,
        CancellationToken cancellationToken = default)
    {
        if (concepts.Count == 0 || rubricEntries.Count == 0)
        {
            return new List<EnterpriseSemanticRubricMatchModel>();
        }

        var rubricById = rubricEntries
            .GroupBy(x => x.RubricId)
            .ToDictionary(g => g.Key, g => g.First());

        var results = new Dictionary<int, EnterpriseSemanticRubricMatchModel>();
        var topRubricsPerConcept = Math.Max(1, _options.SemanticSearchTopRubricsPerConcept);
        var semanticTake = Math.Min(_options.SemanticSearchMaxRubrics, topRubricsPerConcept * 3);

        foreach (var concept in concepts)
        {
            foreach (var rubricId in concept.LinkedRubricIds)
            {
                if (!rubricById.TryGetValue(rubricId, out var rubricEntry))
                {
                    continue;
                }

                AddLinkedRubric(results, concept, rubricEntry, queryVector, includeValidation);
            }
        }

        var semanticMatches = EnterpriseEmbeddingVectorSearch.TopRubricMatches(
            queryVector,
            rubricEntries,
            semanticTake,
            _options.MinRubricCosineScore);

        foreach (var match in semanticMatches)
        {
            if (IsSemanticDocumentTooThin(match.Entry.SourceText))
            {
                continue;
            }

            var concept = concepts
                .OrderByDescending(c => c.SimilarityScore)
                .FirstOrDefault();

            if (concept == null)
            {
                continue;
            }

            var combined = Math.Round((concept.SimilarityScore * 0.55m) + (match.Score * 0.45m), 4);
            if (results.TryGetValue(match.Entry.RubricId, out var existing) && combined <= existing.CombinedScore)
            {
                continue;
            }

            var parsedRubric = ConceptSemanticDocumentParser.Parse(match.Entry.SourceText);
            results[match.Entry.RubricId] = new EnterpriseSemanticRubricMatchModel
            {
                SubSectionId = match.Entry.RubricId,
                SubSectionName = ResolveRubricName(match.Entry),
                MappedFromConceptKey = concept.ConceptKey,
                ConceptSimilarityScore = concept.SimilarityScore,
                RubricSimilarityScore = match.Score,
                CombinedScore = combined,
                MatchMethod = "SemanticRubricEmbedding",
                Validation = includeValidation
                    ? EnterpriseSemanticRubricValidator.Validate(concept, parsedRubric, match.Score, _options)
                    : null,
            };
        }

        var missingNameIds = results.Values
            .Where(x => x.SubSectionName.StartsWith("Rubric ", StringComparison.Ordinal))
            .Select(x => x.SubSectionId)
            .Distinct()
            .ToList();

        if (missingNameIds.Count > 0)
        {
            var names = await LoadRubricNamesAsync(missingNameIds, cancellationToken);
            foreach (var rubric in results.Values)
            {
                if (names.TryGetValue(rubric.SubSectionId, out var name) && !string.IsNullOrWhiteSpace(name))
                {
                    rubric.SubSectionName = name;
                }
            }
        }

        return results.Values
            .OrderByDescending(x => x.CombinedScore)
            .Take(_options.SemanticSearchMaxRubrics)
            .Select((item, index) =>
            {
                item.Rank = index + 1;
                return item;
            })
            .ToList();
    }

    private void AddLinkedRubric(
        Dictionary<int, EnterpriseSemanticRubricMatchModel> results,
        EnterpriseSemanticConceptMatchModel concept,
        AiEnterpriseRubricEmbeddingCacheEntry rubricEntry,
        IReadOnlyList<float> queryVector,
        bool includeValidation)
    {
        var linkedScore = Math.Round(concept.SimilarityScore * 0.85m, 4);
        if (results.TryGetValue(rubricEntry.RubricId, out var existing) && existing.CombinedScore >= linkedScore)
        {
            return;
        }

        var rubricMatches = EnterpriseEmbeddingVectorSearch.TopRubricMatches(
            queryVector,
            new[] { rubricEntry },
            1,
            0m);
        var rubricScore = rubricMatches.Count > 0 ? rubricMatches[0].Score : linkedScore;

        var parsedRubric = ConceptSemanticDocumentParser.Parse(rubricEntry.SourceText);
        results[rubricEntry.RubricId] = new EnterpriseSemanticRubricMatchModel
        {
            SubSectionId = rubricEntry.RubricId,
            SubSectionName = ResolveRubricName(rubricEntry),
            MappedFromConceptKey = concept.ConceptKey,
            ConceptSimilarityScore = concept.SimilarityScore,
            RubricSimilarityScore = rubricScore,
            CombinedScore = Math.Round((concept.SimilarityScore * 0.65m) + (rubricScore * 0.35m), 4),
            MatchMethod = "LinkedConceptRubric",
            Validation = includeValidation
                ? EnterpriseSemanticRubricValidator.Validate(concept, parsedRubric, rubricScore, _options)
                : null,
        };
    }

    private static string ResolveRubricName(AiEnterpriseRubricEmbeddingCacheEntry entry)
    {
        var parsed = ConceptSemanticDocumentParser.Parse(entry.SourceText);
        if (!string.IsNullOrWhiteSpace(parsed.HomeopathicConcept))
        {
            return parsed.HomeopathicConcept!;
        }

        if (!string.IsNullOrWhiteSpace(parsed.ClinicalConcept))
        {
            return parsed.ClinicalConcept!;
        }

        return $"Rubric {entry.RubricId}";
    }

    private static bool IsSemanticDocumentTooThin(string rubricSourceText)
    {
        var parsed = ConceptSemanticDocumentParser.Parse(rubricSourceText);
        return string.IsNullOrWhiteSpace(parsed.ClinicalConcept)
            && string.IsNullOrWhiteSpace(parsed.HomeopathicConcept)
            && parsed.Meanings.Count == 0
            && parsed.KnownSynonyms.Count == 0;
    }

    private async Task<Dictionary<int, string>> LoadRubricNamesAsync(
        IEnumerable<int> rubricIds,
        CancellationToken cancellationToken)
    {
        var ids = rubricIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, string>();
        }

        return await _context.SubSectionMasters.AsNoTracking()
            .Where(x => ids.Contains(x.SubSectionId) && !x.DeleteStatus)
            .ToDictionaryAsync(x => x.SubSectionId, x => x.SubSectionName ?? string.Empty, cancellationToken);
    }
}

public static class EnterpriseSemanticRubricValidator
{
    public static EnterpriseSemanticRubricValidationModel Validate(
        EnterpriseSemanticConceptMatchModel concept,
        ParsedConceptSemanticDocument rubricDocument,
        decimal rubricSimilarityScore,
        AiEmbeddingInfrastructureOptions options)
    {
        var issues = new List<string>();
        var passed = new List<string>();
        var score = rubricSimilarityScore;

        if (rubricSimilarityScore >= options.MinRubricCosineScore)
            passed.Add("SemanticRubricCosine");
        else
            issues.Add($"Rubric semantic cosine {rubricSimilarityScore:0.00} is below threshold {options.MinRubricCosineScore:0.00}.");

        if (ConceptFieldMatches(concept, rubricDocument.ClinicalConcept))
        {
            passed.Add("ClinicalConceptAlignment");
            score += 0.05m;
        }

        if (ConceptFieldMatches(concept, rubricDocument.HomeopathicConcept))
        {
            passed.Add("HomeopathicConceptAlignment");
            score += 0.05m;
        }

        if (concept.MatchedSynonyms.Any(s => rubricDocument.KnownSynonyms.Any(r =>
                AudioCaseAiProcessor.ComputeTextSimilarity(s, r) >= 0.45m)))
        {
            passed.Add("SynonymOverlap");
            score += 0.03m;
        }

        if (string.IsNullOrWhiteSpace(rubricDocument.ClinicalConcept)
            && string.IsNullOrWhiteSpace(rubricDocument.HomeopathicConcept)
            && string.IsNullOrWhiteSpace(rubricDocument.Meanings.FirstOrDefault()))
        {
            issues.Add("Rubric semantic document lacks concept-level meaning fields.");
            score -= 0.10m;
        }

        score = Math.Clamp(score, 0m, 1m);
        var isValid = issues.Count == 0 && score >= options.MinRubricValidationScore;

        return new EnterpriseSemanticRubricValidationModel
        {
            IsValid = isValid,
            ValidationScore = Math.Round(score, 4),
            Issues = issues,
            PassedChecks = passed,
        };
    }

    private static bool ConceptFieldMatches(EnterpriseSemanticConceptMatchModel concept, string? rubricField)
    {
        if (string.IsNullOrWhiteSpace(rubricField))
            return false;

        return AudioCaseAiProcessor.ComputeTextSimilarity(concept.ConceptKey, rubricField) >= 0.40m
            || (!string.IsNullOrWhiteSpace(concept.MatchedClinicalMeaning)
                && AudioCaseAiProcessor.ComputeTextSimilarity(concept.MatchedClinicalMeaning, rubricField) >= 0.40m)
            || (!string.IsNullOrWhiteSpace(concept.MatchedHomeopathicMeaning)
                && AudioCaseAiProcessor.ComputeTextSimilarity(concept.MatchedHomeopathicMeaning, rubricField) >= 0.40m);
    }
}
