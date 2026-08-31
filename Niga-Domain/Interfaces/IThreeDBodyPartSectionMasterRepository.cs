using System.Collections.Generic;
using System.Threading.Tasks;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
{
    /// <summary>
    /// Interface for ThreeDBodyPartSectionMaster operations
    /// </summary>
    public interface IThreeDBodyPartSectionMasterRepository
    {
        Task<ThreeDBodyPartSectionMaster?> GetSectionEntityByIdAsync(int sectionMasterId);

        Task<ThreeDBodyPartSectionMasterModel?> GetSectionDetailsByIdAsync(int sectionMasterId);

        Task<PaginatedResult<ThreeDBodyPartSectionMasterListItem>> GetAllSectionsAsync(PaginationRequestModel request);

        Task<PaginatedResult<ThreeDBodyPartSectionMasterListItem>> GetSectionsByMeshKeyIdAsync(
            int meshKeyId,
            PaginationRequestModel request);

        Task<bool> IsDuplicateMappingAsync(int meshKeyId, int sectionId, int? excludeSectionMasterId = null);

        Task<bool> MeshKeyExistsAsync(int meshKeyId);

        Task<bool> SectionExistsAsync(int sectionId);

        Task<List<ThreeDBodyPartMeshKeyDropdownModel>> GetActiveMeshKeysDropdownAsync();

        Task<List<SectionMasterDropdownModel>> GetActiveSectionsDropdownAsync();

        void SaveSection(ThreeDBodyPartSectionMaster section);

        void UpdateSection(ThreeDBodyPartSectionMaster section);

        void DeleteSection(ThreeDBodyPartSectionMaster section);

        Task<bool> SaveAllAsync();
    }
}
