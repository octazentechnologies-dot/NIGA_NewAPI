using Niga_Domain.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Niga_Domain.Helpers;

namespace Niga_Domain.Business.Interface
{
    /// <summary>
    /// Interface used for AllopathicDrug related operations
    /// </summary>
    public interface IAllopathicDrugService
    {
        AllopathicDrugModel GetAllopathicDrugById(long allopathicDrugId);
        Task<PagedList<AdverseReactionModel>> GetAllAdverseReactionsAsync(ParameterParams parameterParams);
        Task<PagedList<OtherSideEffectModel>> GetAllOtherSideEffectsAsync(ParameterParams parameterParams);
        Task<PagedList<SeriousSideEffectModel>> GetAllSeriousSideEffectsAsync(ParameterParams parameterParams);
        Task<PagedList<AllopathicDrugModel>> GetAllopathicDrugsAsync(ParameterParams parameterParams);
        Task<AllopathicDrugModel> GetAllopathicDrugByNameAsync(string allopathicDrugName);
        Task<List<AllopathicDrugDDModel>> GetAllopathicDrugDropdownAsync(string? search = null);
        Task<string> SaveAllopathicDrug(AllopathicDrugModel allopathicDrugModel);
        Task<string> DeleteAllopathicDrug(long allopathicDrugId);
        Task<bool> SaveAllAsync();
        AllopathicDrugModel GetAllopathicDrugByID(
            int allopathicDrugId,
            ref ErrorResponseModel errorResponseModel
        );
    }
}
