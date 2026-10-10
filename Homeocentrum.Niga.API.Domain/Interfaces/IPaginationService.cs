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
        PaginationResult GetDrugSystem(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDrugGroup(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAllopathicDrug(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetLanguage(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetQuestionSections(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetQuestionGroupExistance(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetQuestionSubGroup(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAuthor(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetRemedies(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetMateriaMedica(int authorId, int remedyId, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetMateriaMedicaHead(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetIntensities(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetSections(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDiagnosisSystem(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetBodyParts(int sectionId, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetClinicalQuestionBodyPartList(int questionGroupId, int questionSubgroupId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDiagnosisTherapeuticsDetails(int diagonosisId, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetDiagnosis(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetPatientLabTests(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetUser(string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAllNewsDetails(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetAllBlogDetail(NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetSubSectionForRubric(int SectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
        PaginationResult GetRepertorizarionRemedyForAccordion(int SectionId, string queryString, NigaParameters nigaParameters, ref ErrorResponseModel errorResponseModel);
    }
}
