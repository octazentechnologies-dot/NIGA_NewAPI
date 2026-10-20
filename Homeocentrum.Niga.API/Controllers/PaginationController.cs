#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Errors;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interface;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Compatibility;

namespace Homeocentrum.Niga.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [OldApiContract]
    public class PaginationController : BaseAPIController
    {
        IPaginationService _paginationService;

        /// <summary>
        /// Used to initialize controller and inject Pagination
        /// </summary>
        /// <param name="paginationService"></param>
        public PaginationController(IPaginationService paginationService)
        {
            _paginationService = paginationService;
        }

        /// <summary>
        /// To get clinical questions by Clinical Questions ID 
        /// </summary>
        /// <param name="questionsId"></param>
        /// <returns></returns>
        [HttpGet("GetSubSectionBySectionIdAndQueryString")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubSectionBySectionIdAndQueryString(int sectionId, string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetSubSectionBySectionIdAndQueryString(sectionId, queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all drug system data  
        /// </summary>
        /// <param name="questionsId"></param>
        /// <returns></returns>
        [HttpGet("GetDrugSystem")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDrugSystem(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDrugSystem(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all drug group data  
        /// </summary>
        /// <param name="questionsId"></param>
        /// <returns></returns>
        [HttpGet("GetDrugGroup")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDrugGroup(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDrugGroup(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all drug group data  
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns></returns>
        [HttpGet("GetAllopathicDrug")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllopathicDrug(string queryString,[FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetAllopathicDrug(queryString,nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all drug group data  
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetLanguage")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetLanguage(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetLanguage(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all drug group data  
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionSections")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetQuestionSections(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetQuestionSections(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question group existance  
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionGroupExistance")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetQuestionGroupExistance(string queryString, [FromQuery] NigaParameters nigaParameters, int? questionGroupId = null, int? questionSectionId = null)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetQuestionGroupExistance(queryString, nigaParameters, ref errorResponseModel, questionGroupId, questionSectionId);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionSubGroup")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetQuestionSubGroup(string queryString, [FromQuery] NigaParameters nigaParameters, int? questionSubgroupId = null, int? questionGroupId = null, int? questionSectionId = null)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetQuestionSubGroup(queryString, nigaParameters, ref errorResponseModel, questionSubgroupId, questionGroupId, questionSectionId);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAuthor")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAuthor(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetAuthor(queryString,nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// <param name="queryString"></param>
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetRemedies")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRemedies(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetRemedies(queryString,nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
         /// </summary>
        /// <returns></returns>
        [HttpGet("GetMateriaMedica")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedica(int authorId,int remedyId, string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetMateriaMedica(authorId, remedyId, queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetMateriaMedicaHead")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetMateriaMedicaHead(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetMateriaMedicaHead(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetIntensities")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetIntensities(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetIntensities(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetSections")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSections(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetSections(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all question sub group
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetDiagnosisSystem")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosisSystem(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDiagnosisSystem(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all Bosy part
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetBodyParts")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetBodyParts(int sectionId, string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetBodyParts(sectionId, queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all clinincal question and body part
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetClinicalQuestionBodyPart")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClinicalQuestionBodyPart(int questionGroupId, int questionSubgroupId, string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetClinicalQuestionBodyPartList(questionGroupId, questionSubgroupId, queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all diagnosis therapeutic details
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetDiagnosisTherapeuticsDetails")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosisTherapeuticsDetails(int diagonosisId, string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDiagnosisTherapeuticsDetails(diagonosisId, queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all diagnosis
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetDiagnosis")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosis(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDiagnosis(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all patient lab test list
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetPatientLabTests")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabTests(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetPatientLabTests(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all User
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetUser")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetUser(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetUser( queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all NewsDetail
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAllNewsDetails")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllNewsDetails(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetAllNewsDetails(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all blog details
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAllBlogDetail")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllBlogDetail(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetAllBlogDetail(queryString, nigaParameters, ref errorResponseModel);

                if (clinicalquestionModel != null)
                {
                    return Ok(clinicalquestionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all blog details
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetSubSectionForRubric")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubSectionForRubric(int sectionId,string queryString,[FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subSctionList = _paginationService.GetSubSectionForRubric(sectionId,queryString,nigaParameters, ref errorResponseModel);

                if (subSctionList != null)
                {
                    return Ok(subSctionList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all blog details
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetRepertorizarionRemedyForAccordion")]
        [ProducesResponseType(typeof(PaginationResult), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRepertorizarionRemedyForAccordion(int remedyID, string RequiredType, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subSctionList = _paginationService.GetRepertorizarionRemedyForAccordion(remedyID, RequiredType, nigaParameters, ref errorResponseModel);

                if (subSctionList != null)
                {
                    return Ok(subSctionList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}
