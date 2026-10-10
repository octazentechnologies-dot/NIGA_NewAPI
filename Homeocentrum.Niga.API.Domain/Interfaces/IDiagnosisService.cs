#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    public interface IDiagnosisService
    {
        /// <summary>
        /// Method is used for to get diagnosis by diagnosisId
        /// </summary>
        /// <param name="diagnosisId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        DiagnosisModel GetDiagnosisById(long diagnosisId, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Method is used for get all the Diagnosis
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        List<DiagnosisModel> GetDiagnosis(NigaParameters nigaParameters ,ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to save Diagnosis
        /// </summary>
        /// <param name="diagnosisModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveDiagnosis(DiagnosisModel diagnosisModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate diagnosis.
        /// </summary>
        /// <param name="diagnosisModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteDiagnosis(DiagnosisModel diagnosisModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to deactivate diagnosis.
        /// </summary>
        /// <param name="diagnosisrubricModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeleteDiagnosisRubric(DiagnosisRubricDeleteTabWise diagnosisrubricModel, ref ErrorResponseModel errorResponseModel);
        /// <summary>
        /// Interface is used to get rubric and remedy for search keyword.
        /// </summary>
        /// <param name="keywordID"></param>
        /// <param name="type"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<RubricKeywordModel> GetRubricByKeywordID(int keywordID, string type, ref ErrorResponseModel errorResponseModel);
        DiagnosisSearchResultModel DiagnosisSearch(string searchKeyword, ref ErrorResponseModel errorResponseModel);
        List<DiagnosisKeywordModel> GetDiagnosisKeywordByTab(int diagnosisId, string type, ref ErrorResponseModel errorResponseModel);
        List<DiagnosisDDLModel> GetDiagnosisDDL();
        DiagnosisTherapeuticsModel GetdiagnosisTherapeuticsDetail(int diagnosisID, ref ErrorResponseModel errorResponseModel);
        DiagnosisSearchResultModel DiagnosisSearch(int diagnosisID, ref ErrorResponseModel errorResponseModel);
    }
}
