using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Microsoft.AspNetCore.Http;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
    /// <summary>
    /// Interface used for remedy related operations
    /// </summary>
   public interface IRemedyService
    {
       
        RemedyCommonUncommonModel GetCommonUnCommonRemedyBySection(long subSectionId, ref ErrorResponseModel errorResponseModel);  
        Task<RemedyMaster> GetRemedyById(long remedyId);
        Task<PagedList<RemedyModel>> GetAllRemedys(ParameterParams parameterParams);
        void SaveRemedy(RemedyMaster remedy);
        void UpdateRemedy(RemedyMaster remedy);
        void DeleteRemedy(RemedyMaster remedy);
        Task<RemedyModel> GetRemedyDetailsById(long remedyId);
        Task<bool> SaveAllAsync();
        Task<RemedyImportModel> ImportRemediesFromExcel(IFormFile file);

        #region Old API compatible methods
#nullable disable
        /// <summary>
        /// Method is used for get all the Remedies
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<SearchRemedyModel> GetRemedies(string search,ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion

        #region Old API compatible overloads
#nullable disable
        /// <summary>
        /// Method is used for to get remedy by remedyId
        /// </summary>
        /// <param name="remedyId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        RemedyModel GetRemedyById(long remedyId, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save Remedie
        /// </summary>
        /// <param name="remedyModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveRemedy(RemedyModel remedyModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate Remedie.
        /// </summary>
        /// <param name="remedyModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteRemedy(RemedyModel remedyModel, ref ErrorResponseModel errorResponseModel);
#nullable restore
        #endregion
    }
}
