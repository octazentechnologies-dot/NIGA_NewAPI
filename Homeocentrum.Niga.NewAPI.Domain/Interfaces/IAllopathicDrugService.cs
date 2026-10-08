using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
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

        #region Old API compatible overloads
#nullable disable
        /// <summary>
        /// Method is used for to get allopathicDrug by allopathicDrugId
        /// </summary>
        /// <param name="allopathicDrugId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        AllopathicDrugModel GetAllopathicDrugById(long allopathicDrugId, ref ErrorResponseModel errorResponseModel);
        List<AllopathicDrugModel> GetAllopathicDrug(ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }
}
