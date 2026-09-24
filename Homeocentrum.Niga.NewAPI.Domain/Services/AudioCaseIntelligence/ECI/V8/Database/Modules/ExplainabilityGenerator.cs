using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Models;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.ECI.V8.Database.Modules;

/// <summary>Module 12: explainability generator (deterministic, provenance-based).</summary>
public sealed class ExplainabilityGenerator : IEciExplainabilityGenerator
{
    public void ApplyExplainability(
        AudioCaseSuggestedRubricModel rubric,
        EciValidatedSymptom symptom,
        EciCandidateRubric candidate)
    {
        var top = candidate.Provenance
            .OrderByDescending(p => p.Confidence)
            .FirstOrDefault();

        var chain = new List<string>
        {
            $"Symptom: {symptom.Symptom.Symptom}",
            $"Evidence: \"{Truncate(symptom.Symptom.Evidence, 90)}\"",
        };

        if (top != null)
        {
            chain.Add($"Match: {top.Source} ({top.Confidence:0.00})");
            chain.Add($"Path: {top.MatchPath}");
        }

        chain.Add($"Score: {candidate.FinalScore:0.###}");

        rubric.SelectionReason = string.Join(" → ", chain);
        rubric.WhySuggested = rubric.SelectionReason;
        rubric.MatchedFrom = symptom.Symptom.Evidence;
    }

    private static string Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? "—" :
        value.Length <= max ? value : value[..max] + "...";
}

