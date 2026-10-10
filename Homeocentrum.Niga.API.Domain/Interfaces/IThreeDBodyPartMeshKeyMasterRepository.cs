using System.Collections.Generic;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Interfaces
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
