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
    }
}
