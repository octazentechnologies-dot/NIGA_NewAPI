using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IDoctorLearningWeightProvider
{
    Task<DoctorLearningWeightsSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task<DoctorLearningSummaryModel> GetSummaryAsync(
        int topN = 10,
        CancellationToken cancellationToken = default);
}
