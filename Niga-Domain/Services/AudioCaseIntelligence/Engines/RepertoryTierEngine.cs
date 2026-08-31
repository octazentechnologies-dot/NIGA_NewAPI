using Niga_Domain.Configuration;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.Engines;

public class RepertoryTierEngine : IRepertoryTierEngine
{
    private readonly IRepertoryMappingRepository _mappingRepository;
    private readonly IRubricIntelligenceSettingsService _settings;

    public RepertoryTierEngine(
        IRepertoryMappingRepository mappingRepository,
        IRubricIntelligenceSettingsService settings)
    {
        _mappingRepository = mappingRepository;
        _settings = settings;
    }

    public async Task<RepertoryTierEnrichmentResult> EnrichAsync(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.EnableRepertoryMapping || rubrics.Count == 0)
        {
            return new RepertoryTierEnrichmentResult { Rubrics = rubrics.ToList() };
        }

        var subSectionIds = rubrics.Where(r => r.SubSectionId > 0).Select(r => r.SubSectionId);
        var mapsBySubSection = await _mappingRepository.GetMapsForSubSectionsAsync(subSectionIds, cancellationToken);

        var enriched = rubrics.Select(rubric => EnrichRubric(rubric, mapsBySubSection)).ToList();
        var mappedCount = enriched.Count(r => r.RepertorySources.Count > 0);

        return new RepertoryTierEnrichmentResult
        {
            Rubrics = enriched,
            MappedRubricCount = mappedCount,
        };
    }

    public static AudioCaseSuggestedRubricModel EnrichRubric(
        AudioCaseSuggestedRubricModel rubric,
        IReadOnlyDictionary<int, List<RepertoryMapModel>> mapsBySubSection)
    {
        if (rubric.SubSectionId <= 0
            || !mapsBySubSection.TryGetValue(rubric.SubSectionId, out var maps)
            || maps.Count == 0)
        {
            return rubric;
        }

        var sources = maps.Select(m => m.SourceCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var primarySource = maps.FirstOrDefault(m => m.IsPrimarySource)?.SourceCode
            ?? maps.OrderBy(m => m.PriorityOrder).First().SourceCode;

        var score = rubric.ConfidenceScore ?? rubric.MatchScore;
        var tier = ResolveTierWithRepertory(rubric.RubricTier, maps, score);

        var explainability = rubric.Explainability == null
            ? null
            : new RubricExplainabilityModel
            {
                PatientStatement = rubric.Explainability.PatientStatement,
                ClinicalMeaning = rubric.Explainability.ClinicalMeaning,
                HomeopathicMeaning = rubric.Explainability.HomeopathicMeaning,
                WhySuggested = rubric.Explainability.WhySuggested,
                MatchLayer = rubric.Explainability.MatchLayer,
                ConfidenceScore = rubric.Explainability.ConfidenceScore,
                RubricTier = tier,
                ReviewRequired = rubric.Explainability.ReviewRequired,
                CausationChain = rubric.Explainability.CausationChain,
                SourceConceptId = rubric.Explainability.SourceConceptId,
            };

        return new AudioCaseSuggestedRubricModel
        {
            SubSectionId = rubric.SubSectionId,
            SubSectionName = rubric.SubSectionName,
            SectionId = rubric.SectionId,
            MatchScore = rubric.MatchScore,
            SuggestedIntensityNo = rubric.SuggestedIntensityNo,
            MatchedFrom = rubric.MatchedFrom,
            RemedyCountForSort = rubric.RemedyCountForSort,
            IsAiSuggested = rubric.IsAiSuggested,
            MatchSource = rubric.MatchSource,
            ConfidenceScore = rubric.ConfidenceScore,
            WhySuggested = rubric.WhySuggested,
            EngineVersion = rubric.EngineVersion,
            RequiresManualApproval = rubric.RequiresManualApproval,
            HomeopathicWeight = rubric.HomeopathicWeight,
            MatchLayer = rubric.MatchLayer,
            RubricTier = tier,
            RequiresDoctorReview = rubric.RequiresDoctorReview || tier == "Inference",
            SourceConceptId = rubric.SourceConceptId,
            InferenceReason = rubric.InferenceReason,
            Explainability = explainability,
            RepertorySources = sources,
            PrimaryRepertorySource = primarySource,
        };
    }

    public static string ResolveTierWithRepertory(
        string? existingTier,
        IReadOnlyList<RepertoryMapModel> maps,
        decimal score)
    {
        if (string.Equals(existingTier, "Inference", StringComparison.OrdinalIgnoreCase))
            return "Inference";

        var hasKent = maps.Any(m => string.Equals(m.SourceCode, "KENT", StringComparison.OrdinalIgnoreCase));
        var hasComplete = maps.Any(m => string.Equals(m.SourceCode, "COMPLETE", StringComparison.OrdinalIgnoreCase));

        if (hasKent && score >= 0.85m)
            return "Primary";

        if ((hasKent || hasComplete) && score >= 0.70m)
            return "Secondary";

        if (hasKent || hasComplete)
            return "Confirmatory";

        if (score >= 0.85m)
            return "Primary";

        if (score >= 0.70m)
            return "Secondary";

        return existingTier ?? "Confirmatory";
    }
}
