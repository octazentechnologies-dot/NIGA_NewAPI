using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IDoctorLearningWeightProvider
{
    Task<DoctorLearningWeightsSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task<DoctorLearningSummaryModel> GetSummaryAsync(
        int topN = 10,
        CancellationToken cancellationToken = default);
}
