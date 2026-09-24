using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Learning;

public static class DoctorLearningScoring
{
    public static decimal ApplyConceptRankingBoost(
        decimal baseScore,
        string conceptName,
        IReadOnlyDictionary<string, decimal> conceptRankingWeights,
        RubricIntelligenceOptions options)
    {
        if (!options.EnableDoctorLearningEngine
            || string.IsNullOrWhiteSpace(conceptName)
            || conceptRankingWeights.Count == 0)
        {
            return baseScore;
        }

        if (!TryGetWeight(conceptRankingWeights, conceptName, out var weight))
            return baseScore;

        var capped = CapWeight(weight, options.DoctorLearningMaxAccumulatedWeight);
        return Math.Max(0m, baseScore * (1m + capped * 0.12m));
    }

    public static decimal ApplyClinicalRelevanceBoost(
        decimal baseScore,
        string? clinicalConceptName,
        string? homeopathicConceptName,
        IReadOnlyDictionary<string, decimal> clinicalRelevanceWeights,
        RubricIntelligenceOptions options)
    {
        if (!options.EnableDoctorLearningEngine || clinicalRelevanceWeights.Count == 0)
            return baseScore;

        var weight = 0m;
        if (!string.IsNullOrWhiteSpace(clinicalConceptName)
            && TryGetWeight(clinicalRelevanceWeights, clinicalConceptName, out var clinicalWeight))
        {
            weight = clinicalWeight;
        }
        else if (!string.IsNullOrWhiteSpace(homeopathicConceptName)
            && TryGetWeight(clinicalRelevanceWeights, homeopathicConceptName, out var homeoWeight))
        {
            weight = homeoWeight;
        }
        else
        {
            return baseScore;
        }

        var capped = CapWeight(weight, options.DoctorLearningMaxAccumulatedWeight);
        return Math.Clamp(baseScore + (capped * 0.08m), 0m, 1m);
    }

    public static decimal ApplyConfidenceCalibration(
        decimal confidence,
        string conceptName,
        IReadOnlyDictionary<string, decimal> conceptRankingWeights,
        RubricIntelligenceOptions options)
    {
        if (!options.EnableDoctorLearningEngine
            || string.IsNullOrWhiteSpace(conceptName)
            || conceptRankingWeights.Count == 0)
        {
            return confidence;
        }

        if (!TryGetWeight(conceptRankingWeights, conceptName, out var weight))
            return confidence;

        var capped = CapWeight(weight, options.DoctorLearningMaxAccumulatedWeight);
        return Math.Clamp(confidence + (capped * 0.05m), 0m, 0.99m);
    }

    public static decimal ApplyDoctorAcceptanceBoost(
        decimal doctorAcceptance,
        string conceptName,
        int subSectionId,
        DoctorLearningWeightsSnapshot learned,
        RubricIntelligenceOptions options)
    {
        if (!options.EnableDoctorLearningEngine)
            return doctorAcceptance;

        var boosted = doctorAcceptance;

        if (learned.ConceptRubricMapping.TryGetValue((conceptName, subSectionId), out var mappingWeight))
        {
            var capped = CapWeight(mappingWeight, options.DoctorLearningMaxAccumulatedWeight);
            boosted = Math.Clamp(boosted + (capped * 0.05m), 0m, 1m);
        }

        if (learned.RubricAcceptanceRates.TryGetValue(subSectionId, out var rubricRate))
            boosted = Math.Clamp((boosted * 0.7m) + (rubricRate * 0.3m), 0m, 1m);

        return boosted;
    }

    public static decimal CapWeight(decimal weight, decimal maxAccumulated) =>
        Math.Clamp(weight, -maxAccumulated, maxAccumulated);

    private static bool TryGetWeight(
        IReadOnlyDictionary<string, decimal> weights,
        string conceptName,
        out decimal weight)
    {
        if (weights.TryGetValue(conceptName, out weight))
            return true;

        var match = weights.FirstOrDefault(kvp =>
            conceptName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)
            || kvp.Key.Contains(conceptName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(match.Key))
        {
            weight = 0m;
            return false;
        }

        weight = match.Value;
        return true;
    }
}
