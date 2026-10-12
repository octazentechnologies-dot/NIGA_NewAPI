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
    [Route("api/clinicalquestions")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    [OldApiContract]
    public class ClinicalQuestionsController : BaseAPIController
    {
        IClinicalQuestionsService _clinicalquestionsService;

        /// <summary>
        /// Used to initialize controller and inject clinical questions
        /// </summary>
        /// <param name="clinicalquestionsService"></param>
        public ClinicalQuestionsController(IClinicalQuestionsService clinicalquestionsService)
        {
            _clinicalquestionsService = clinicalquestionsService;
        }

        /// <summary>
        /// To get clinical questions by Clinical Questions ID 
        /// </summary>
        /// <param name="questionsId"></param>
        /// <returns></returns>
        [HttpGet("{questionsId}")]
        [ProducesResponseType(typeof(ClinicalQuestionsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClinicalQuestionsById(long questionsId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _clinicalquestionsService.GetClinicalQuestionsById(questionsId, ref errorResponseModel);

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
        /// To get all clinical questions
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetClinicalQuestionBodyPartDataById")]
        [ProducesResponseType(typeof(ClinicalQueKeywordModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClinicalQuestionBodyPartDataById(int questionId, int QBType)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModelList = _clinicalquestionsService.GetClinicalQuestionBodyPartDataById(questionId, QBType, ref errorResponseModel);

                if (clinicalquestionModelList != null)
                {
                    return Ok(clinicalquestionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new clinical questions 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("AddEditClinicalQuestionsBodyPart")]
        [ProducesResponseType(typeof(ClinicalQuestionsBodyPartModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult AddEditClinicalQuestionsBodyPart(ClinicalQuestionsBodyPartModel clinicalQuestionsBodyPart)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _clinicalquestionsService.AddEditClinicalQuestionsBodyPart(clinicalQuestionsBodyPart, ref errorResponseModel);

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
        /// To delete clinical questions / Body part and rubric
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteQuestionBodyPartData")]
        [ProducesResponseType(typeof(ClinicalQuestionsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteQuestionBodyPartData(int questionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionmodel = _clinicalquestionsService.DeleteClinicalQuestionBodyPart(questionId, 0, ref errorResponseModel);

                if (clinicalquestionmodel != null)
                {
                    return Ok(clinicalquestionmodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        //Doctor Side

        /// <summary>
        /// To add new clinical questions 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetClinicalQuestionsKeyWordBodyPart")]
        [ProducesResponseType(typeof(List<QuestionKeyWordBodyPartOutputModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClinicalQuestionsKeyWordBodyPart(QuestionKeyWordBodyPartInputModel inputModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModel = _clinicalquestionsService.GetClinicalQuestionsKeyWordBodyPart(inputModel, ref errorResponseModel);

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
        /// To get all clinical questions
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetClinicalRubricData")]
        [ProducesResponseType(typeof(List<QuestionKeyWordBodyPartRubricOutputModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClinicalRubricData(QuestionKeyWordBodyPartRubricInputModel rubricInputModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clinicalquestionModelList = _clinicalquestionsService.GetClinicalRubricData(rubricInputModel, ref errorResponseModel);

                if (clinicalquestionModelList != null)
                {
                    return Ok(clinicalquestionModelList);
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
