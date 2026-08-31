using System.Collections.Generic;
using System.Threading.Tasks;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Interfaces
{
    /// <summary>
    /// Interface for ThreeDBodyPartMeshKeyMaster operations
    /// </summary>
    public interface IThreeDBodyPartMeshKeyMasterRepository
    {
        Task<ThreeDBodyPartMeshKeyMaster?> GetMeshKeyEntityByIdAsync(int meshKeyId);

        Task<ThreeDBodyPartMeshKeyMasterModel?> GetMeshKeyDetailsByIdAsync(int meshKeyId);

        Task<PaginatedResult<ThreeDBodyPartMeshKeyMasterModel>> GetAllMeshKeysAsync(PaginationRequestModel request);

        Task<bool> IsDuplicateMeshKeyNameAsync(string meshKeyName, int? excludeMeshKeyId = null);

        void SaveMeshKey(ThreeDBodyPartMeshKeyMaster meshKey);

        void UpdateMeshKey(ThreeDBodyPartMeshKeyMaster meshKey);

        void DeleteMeshKey(ThreeDBodyPartMeshKeyMaster meshKey);

        Task<bool> SaveAllAsync();
    }
}
