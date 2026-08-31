using Microsoft.Extensions.Logging;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AiEmbeddingInfrastructure;
using Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Niga_Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 5: embedding search using enterprise semantic search infrastructure.</summary>
public sealed class EmbeddingSearchModule : IEciEmbeddingSearchModule
{
    private readonly IAiEnterpriseSemanticSearchService _semanticSearch;
    private readonly ILogger<EmbeddingSearchModule> _logger;

    public EmbeddingSearchModule(
        IAiEnterpriseSemanticSearchService semanticSearch,
        ILogger<EmbeddingSearchModule> logger)
    {
        _semanticSearch = semanticSearch;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        IReadOnlyList<string> terms,
        CancellationToken cancellationToken = default)
    {
        var query = terms.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))
            ?? symptom.Symptom.Symptom
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(query))
        {
            return new List<EciCandidateRubric>();
        }

        var result = await _semanticSearch.SearchAsync(new EnterpriseSemanticSearchRequest
        {
            ClinicalConcept = query,
            TopConcepts = 20,
            IncludeRubricMapping = true,
            IncludeValidation = false,
        }, cancellationToken);

        if (!result.Success || result.Rubrics.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        var map = new Dictionary<int, EciCandidateRubric>();
        foreach (var rubric in result.Rubrics.Take(20))
        {
            if (rubric.SubSectionId <= 0 || string.IsNullOrWhiteSpace(rubric.SubSectionName))
            {
                continue;
            }

            if (!map.TryGetValue(rubric.SubSectionId, out var candidate))
            {
                candidate = new EciCandidateRubric
                {
                    SubSectionId = rubric.SubSectionId,
                    SubSectionName = rubric.SubSectionName,
                };
                map[rubric.SubSectionId] = candidate;
            }

            candidate.EmbeddingScore = Math.Max(candidate.EmbeddingScore, rubric.CombinedScore);
            candidate.Provenance.Add(new EciCandidateProvenance
            {
                Source = EciCandidateSources.Embedding,
                MatchPath = $"Embedding: '{query}' (v={result.VersionCode})",
                Confidence = rubric.CombinedScore,
                EmbeddingScore = rubric.CombinedScore,
            });
        }

        return map.Values.ToList();
    }
}

