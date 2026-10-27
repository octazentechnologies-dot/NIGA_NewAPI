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
    public interface IPaginationService
    {
        /// <summary>
        /// Method is used for get all subsection by sectionId and query string It used for admin login
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        PaginationResult GetSubSectionBySectionIdAndQueryString(int sectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDrugSystem(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDrugGroup(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAllopathicDrug(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetLanguage(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetQuestionSections(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetQuestionGroupExistance(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel, int? questionGroupId = null, int? questionSectionId = null);
        PaginationResult GetQuestionSubGroup(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel, int? questionSubgroupId = null, int? questionGroupId = null, int? questionSectionId = null);
        PaginationResult GetAuthor(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetRemedies(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetMateriaMedica(int authorId, int remedyId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetMateriaMedicaHead(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetIntensities(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetSections(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDiagnosisSystem(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetBodyParts(int sectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetClinicalQuestionBodyPartList(int questionGroupId, int questionSubgroupId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDiagnosisTherapeuticsDetails(int diagonosisId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDiagnosis(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetPatientLabTests(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetUser(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAllNewsDetails(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAllBlogDetail(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetSubSectionForRubric(int SectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetRepertorizarionRemedyForAccordion(
            int remedyID,
            string RequiredType,
            NigaParameters nigaParameters,
            ref ErrorResponseModel errorResponseModel,
            int[] sectionIds = null,
            int? gradeNo = null);
    }
}
