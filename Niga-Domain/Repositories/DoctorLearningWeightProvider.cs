using Microsoft.EntityFrameworkCore;
using Niga_Domain.Configuration;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Services.AudioCaseIntelligence.Learning;

namespace Niga_Domain.Repositories;

public class DoctorLearningWeightProvider : IDoctorLearningWeightProvider
{
    private readonly NIGACentrumContext _context;
    private readonly RubricIntelligenceOptions _options;

    public DoctorLearningWeightProvider(
        NIGACentrumContext context,
        Microsoft.Extensions.Options.IOptions<RubricIntelligenceOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<DoctorLearningWeightsSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableDoctorLearningEngine)
            return DoctorLearningWeightsSnapshot.Empty;

        var rows = await _context.AiCaseLearnings.AsNoTracking()
            .Select(x => new { x.LearningType, x.FromConcept, x.ToRubricSubSectionId, x.WeightDelta })
            .ToListAsync(cancellationToken);

        var snapshot = new DoctorLearningWeightsSnapshot();
        var max = _options.DoctorLearningMaxAccumulatedWeight;

        foreach (var group in rows.GroupBy(x => x.LearningType, StringComparer.OrdinalIgnoreCase))
        {
            switch (group.Key)
            {
                case DoctorLearningTypes.ConceptRubricMapping:
                    foreach (var item in group.Where(x => x.ToRubricSubSectionId.HasValue)
                        .GroupBy(x => (x.FromConcept, x.ToRubricSubSectionId!.Value)))
                    {
                        var weight = DoctorLearningScoring.CapWeight(
                            item.Sum(x => x.WeightDelta),
                            max);
                        snapshot.ConceptRubricMapping[(item.Key.FromConcept, item.Key.Value)] = weight;
                    }
                    break;

                case DoctorLearningTypes.ConceptRanking:
                case DoctorLearningTypes.ConfidenceCalibration:
                    foreach (var item in group.GroupBy(x => x.FromConcept, StringComparer.OrdinalIgnoreCase))
                    {
                        var existing = snapshot.ConceptRanking.GetValueOrDefault(item.Key);
                        var weight = DoctorLearningScoring.CapWeight(
                            existing + item.Sum(x => x.WeightDelta),
                            max);
                        snapshot.ConceptRanking[item.Key] = weight;
                    }
                    break;

                case DoctorLearningTypes.ClinicalRelevance:
                    foreach (var item in group.GroupBy(x => x.FromConcept, StringComparer.OrdinalIgnoreCase))
                    {
                        var weight = DoctorLearningScoring.CapWeight(
                            item.Sum(x => x.WeightDelta),
                            max);
                        snapshot.ClinicalRelevance[item.Key] = weight;
                    }
                    break;
            }
        }

        var acceptanceStats = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .Where(x => x.SubSectionId.HasValue)
            .GroupBy(x => x.SubSectionId!.Value)
            .Select(g => new
            {
                SubSectionId = g.Key,
                Accepted = g.Count(x => x.FeedbackType == "Accepted"),
                Total = g.Count(),
            })
            .ToListAsync(cancellationToken);

        snapshot.RubricAcceptanceRates = acceptanceStats.ToDictionary(
            x => x.SubSectionId,
            x => x.Total == 0 ? 0.50m : Math.Round((decimal)x.Accepted / x.Total, 4));

        return snapshot;
    }

    public async Task<DoctorLearningSummaryModel> GetSummaryAsync(
        int topN = 10,
        CancellationToken cancellationToken = default)
    {
        var feedbackStats = await _context.AudioCaseRubricFeedbacks.AsNoTracking()
            .GroupBy(x => x.FeedbackType)
            .Select(g => new { FeedbackType = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var totalFeedback = feedbackStats.Sum(x => x.Count);
        var accepted = feedbackStats.FirstOrDefault(x => x.FeedbackType == "Accepted")?.Count ?? 0;

        var learningRows = await _context.AiCaseLearnings.AsNoTracking()
            .Select(x => new { x.LearningType, x.FromConcept, x.ToRubricSubSectionId, x.WeightDelta })
            .ToListAsync(cancellationToken);

        var topConcepts = learningRows
            .Where(x => x.LearningType is DoctorLearningTypes.ConceptRanking or DoctorLearningTypes.ClinicalRelevance)
            .GroupBy(x => new { x.FromConcept, x.LearningType })
            .Select(g => new DoctorLearningTopConceptModel
            {
                ConceptName = g.Key.FromConcept,
                LearningType = g.Key.LearningType,
                AccumulatedWeight = Math.Round(g.Sum(x => x.WeightDelta), 3),
            })
            .OrderByDescending(x => Math.Abs(x.AccumulatedWeight))
            .Take(topN)
            .ToList();

        var topMappings = learningRows
            .Where(x => x.LearningType == DoctorLearningTypes.ConceptRubricMapping && x.ToRubricSubSectionId.HasValue)
            .GroupBy(x => new { x.FromConcept, SubSectionId = x.ToRubricSubSectionId!.Value })
            .Select(g => new DoctorLearningTopMappingModel
            {
                ConceptName = g.Key.FromConcept,
                SubSectionId = g.Key.SubSectionId,
                AccumulatedWeight = Math.Round(g.Sum(x => x.WeightDelta), 3),
            })
            .OrderByDescending(x => x.AccumulatedWeight)
            .Take(topN)
            .ToList();

        return new DoctorLearningSummaryModel
        {
            EngineVersion = "v9",
            TotalLearningSignals = learningRows.Count,
            TotalFeedbackCount = totalFeedback,
            AcceptedCount = accepted,
            RejectedCount = feedbackStats.FirstOrDefault(x => x.FeedbackType == "Rejected")?.Count ?? 0,
            CorrectedCount = feedbackStats.FirstOrDefault(x => x.FeedbackType == "Corrected")?.Count ?? 0,
            DistinctConceptsLearned = learningRows.Select(x => x.FromConcept).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            DistinctRubricMappingsLearned = learningRows
                .Where(x => x.ToRubricSubSectionId.HasValue)
                .Select(x => x.ToRubricSubSectionId!.Value)
                .Distinct()
                .Count(),
            AverageAcceptanceRate = totalFeedback == 0 ? null : Math.Round((decimal)accepted / totalFeedback, 4),
            TopBoostedConcepts = topConcepts,
            TopConceptRubricMappings = topMappings,
            GeneratedAtUtc = DateTime.UtcNow,
        };
    }
}
