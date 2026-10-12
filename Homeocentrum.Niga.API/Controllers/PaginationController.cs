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
        public IActionResult GetDrugSystem([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDrugSystem(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetDrugGroup([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDrugGroup(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetLanguage( [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetLanguage(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetQuestionGroupExistance(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetQuestionGroupExistance(queryString, nigaParameters, ref errorResponseModel);

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
        public IActionResult GetQuestionSubGroup(string queryString, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetQuestionSubGroup(queryString, nigaParameters, ref errorResponseModel);

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
        public IActionResult GetMateriaMedica(int authorId,int remedyId,[FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetMateriaMedica(authorId, remedyId, nigaParameters, ref errorResponseModel);

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
        public IActionResult GetMateriaMedicaHead([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetMateriaMedicaHead(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetIntensities([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetIntensities(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetSections([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetSections(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetDiagnosisSystem([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDiagnosisSystem(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetBodyParts(int sectionId,[FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetBodyParts(sectionId,nigaParameters, ref errorResponseModel);

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
        public IActionResult GetDiagnosisTherapeuticsDetails(int diagonosisId,[FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetDiagnosisTherapeuticsDetails(diagonosisId,nigaParameters, ref errorResponseModel);

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
        public IActionResult GetPatientLabTests([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetPatientLabTests(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetAllNewsDetails([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetAllNewsDetails(nigaParameters, ref errorResponseModel);

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
        public IActionResult GetAllBlogDetail([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _paginationService.GetAllBlogDetail(nigaParameters, ref errorResponseModel);

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
