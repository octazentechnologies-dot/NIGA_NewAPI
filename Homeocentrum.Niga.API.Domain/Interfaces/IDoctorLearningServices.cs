using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interfaces;

public interface IDoctorLearningWeightProvider
{
    Task<DoctorLearningWeightsSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task<DoctorLearningSummaryModel> GetSummaryAsync(
        int topN = 10,
        CancellationToken cancellationToken = default);
}
