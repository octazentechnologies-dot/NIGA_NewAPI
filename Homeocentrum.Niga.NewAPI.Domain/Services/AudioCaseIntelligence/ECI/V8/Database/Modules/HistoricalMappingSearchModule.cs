using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 8: historical mapping search using doctor learning signals (deterministic).</summary>
public sealed class HistoricalMappingSearchModule : IEciHistoricalMappingSearchModule
{
    private readonly IDoctorLearningWeightProvider _weightProvider;
    private readonly ILogger<HistoricalMappingSearchModule> _logger;

    public HistoricalMappingSearchModule(
        IDoctorLearningWeightProvider weightProvider,
        ILogger<HistoricalMappingSearchModule> logger)
    {
        _weightProvider = weightProvider;
        _logger = logger;
    }

    public async Task<List<EciCandidateRubric>> SearchAsync(
        EciValidatedSymptom symptom,
        CancellationToken cancellationToken = default)
    {
        var text = symptom.Symptom.Symptom?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<EciCandidateRubric>();
        }

        var snapshot = await _weightProvider.LoadAsync(cancellationToken);
        if (snapshot.ConceptRubricMapping.Count == 0)
        {
            return new List<EciCandidateRubric>();
        }

        // Deterministic: only match by exact concept key.
        var matches = snapshot.ConceptRubricMapping
            .Where(kvp => string.Equals(kvp.Key.Concept, text, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(kvp => kvp.Value)
            .Take(30)
            .ToList();

        var list = new List<EciCandidateRubric>();
        foreach (var match in matches)
        {
            list.Add(new EciCandidateRubric
            {
                SubSectionId = match.Key.RubricId,
                SubSectionName = string.Empty, // resolved later by merger/loader
                HistoricalScore = Math.Clamp(match.Value, 0m, 1m),
                Provenance = new List<EciCandidateProvenance>
                {
                    new()
                    {
                        Source = EciCandidateSources.Historical,
                        MatchPath = $"Historical mapping: '{text}' → rubricId={match.Key.RubricId}",
                        Confidence = Math.Clamp(0.50m + match.Value * 0.40m, 0.50m, 0.95m),
                    },
                },
            });
        }

        return list;
    }
}

