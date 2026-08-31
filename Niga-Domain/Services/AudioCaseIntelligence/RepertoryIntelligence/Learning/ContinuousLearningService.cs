using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Services.AudioCaseIntelligence.RepertoryIntelligence.Learning;

/// <summary>V7: bridges doctor feedback into future ranking weights.</summary>
public interface IContinuousLearningService
{
    Task RecordDecisionAsync(
        Guid sessionId,
        V7ExtractedSymptom symptom,
        AudioCaseSuggestedRubricModel rubric,
        string decision,
        string? doctorNotes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, decimal>> GetConceptWeightsAsync(
        long? doctorId,
        CancellationToken cancellationToken = default);
}

public class ContinuousLearningService : IContinuousLearningService
{
    private readonly IDoctorLearningWeightProvider _weightProvider;

    public ContinuousLearningService(IDoctorLearningWeightProvider weightProvider)
    {
        _weightProvider = weightProvider;
    }

    public Task RecordDecisionAsync(
        Guid sessionId,
        V7ExtractedSymptom symptom,
        AudioCaseSuggestedRubricModel rubric,
        string decision,
        string? doctorNotes,
        CancellationToken cancellationToken = default)
    {
        // Doctor feedback is persisted via AudioCaseTakingController → DoctorFeedbackLearningEngine.
        // This service provides the V7 integration point for future automated weight updates.
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetConceptWeightsAsync(
        long? doctorId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _weightProvider.LoadAsync(cancellationToken);
        return snapshot.ConceptRanking;
    }
}
