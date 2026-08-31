using Niga_Domain.Configuration;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IRepertoryMappingRepository
{
    Task<Dictionary<int, List<RepertoryMapModel>>> GetMapsForSubSectionsAsync(
        IEnumerable<int> subSectionIds,
        CancellationToken cancellationToken = default);

    Task<RepertoryMappingStatusModel> GetMappingStatusAsync(CancellationToken cancellationToken = default);
}

public interface IRepertoryTierEngine
{
    Task<RepertoryTierEnrichmentResult> EnrichAsync(
        IReadOnlyList<AudioCaseSuggestedRubricModel> rubrics,
        CancellationToken cancellationToken = default);
}

public interface IRubricIntelligenceSettingsService
{
    bool IsV2Active { get; }

    bool RollbackToV1Only { get; }

    bool RequiresManualApproval { get; }

    bool EnableRepertoryMapping { get; }

    bool IsV3Active { get; }

    RubricIntelligenceOptions GetBaseOptions();

    RubricIntelligenceConfigModel GetConfig();

    void ApplyRuntimeUpdate(RubricIntelligenceConfigUpdateModel update, int adminUserId);

    void ClearRuntimeOverrides();

    Task<RubricIntelligenceRolloutStatusModel> GetRolloutStatusAsync(CancellationToken cancellationToken = default);
}
