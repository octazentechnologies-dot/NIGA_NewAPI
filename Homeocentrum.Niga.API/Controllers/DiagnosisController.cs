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
    [Route("api/diagnosis")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    [OldApiContract]
    public class DiagnosisController : BaseAPIController
    {
        IDiagnosisService _diagnosisService;

        /// <summary>
        /// Used to initialize controller and inject diagnosis service
        /// </summary>
        /// <param name="diagnosisService"></param>
        public DiagnosisController(IDiagnosisService diagnosisService)
        {
            _diagnosisService = diagnosisService;
        }

        /// <summary>
        /// To get diagnosis by Diagnosis ID 
        /// </summary>
        /// <param name="diagnosisId"></param>
        /// <returns></returns>
        [HttpGet("{diagnosisId}")]
        [ProducesResponseType(typeof(DiagnosisModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosisById(long diagnosisId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisModel = _diagnosisService.GetDiagnosisById(diagnosisId, ref errorResponseModel);

                if (diagnosisModel != null)
                {
                    return Ok(diagnosisModel);
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
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetDiagnosis")]
        [ProducesResponseType(typeof(DiagnosisModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosis([FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisModelList = _diagnosisService.GetDiagnosis(nigaParameters,ref errorResponseModel);

                if (diagnosisModelList != null)
                {
                    return Ok(diagnosisModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DiagnosisModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveDiagnosis(DiagnosisModel diagnosismodel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisModel = _diagnosisService.SaveDiagnosis(diagnosismodel, ref errorResponseModel);

                if (diagnosisModel != null)
                {
                    return Ok(diagnosisModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteDiagnosis")]
        [ProducesResponseType(typeof(DiagnosisModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteDiagnosis(DiagnosisModel diagnosisModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosismodel = _diagnosisService.DeleteDiagnosis(diagnosisModel, ref errorResponseModel);

                if (diagnosismodel != null)
                {
                    return Ok(diagnosismodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteDiagnosisRubric")]
        [ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteDiagnosisRubric(DiagnosisRubricDeleteTabWise diagnosisrubricModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisrubric = _diagnosisService.DeleteDiagnosisRubric(diagnosisrubricModel, ref errorResponseModel);

                if (diagnosisrubric != null)
                {
                    return Ok(diagnosisrubric);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        //[HttpPost]
        //[Route("DiagnosisSearch")]
        //[ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        //[ProducesResponseType(typeof(string), 404)]
        //[ProducesResponseType(typeof(string), 400)]
        //[ProducesResponseType(typeof(string), 500)]
        //public IActionResult DiagnosisSearch(string keyword)
        //{
        //    ErrorResponseModel errorResponseModel = null;
        //    try
        //    {
        //        var diagnosisrubric = _diagnosisService.DiagnosisSearch(keyword, ref errorResponseModel);

        //        if (diagnosisrubric != null)
        //        {
        //            return Ok(diagnosisrubric);
        //        }
        //        return ReturnErrorResponse(errorResponseModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex);
        //    }
        //}

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DiagnosisSearch")]
        [ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult DiagnosisSearch(int diagnosisID)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisrubric = _diagnosisService.DiagnosisSearch(diagnosisID, ref errorResponseModel);

                if (diagnosisrubric != null)
                {
                    return Ok(diagnosisrubric);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetDiagnosisKeywordByTab")]
        [ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosisKeywordByTab(int diagnosisId, string tabType)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisrubric = _diagnosisService.GetDiagnosisKeywordByTab(diagnosisId, tabType, ref errorResponseModel);

                if (diagnosisrubric != null)
                {
                    return Ok(diagnosisrubric);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetRubricByKeywordID")]
        [ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRubricByKeywordID(int keywordID, string tabType)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisrubric = _diagnosisService.GetRubricByKeywordID(keywordID, tabType, ref errorResponseModel);

                if (diagnosisrubric != null)
                {
                    return Ok(diagnosisrubric);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetDiagnosisForClinicalPattern")]
        [ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDiagnosisDDL()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisrubric = _diagnosisService.GetDiagnosisDDL();

                if (diagnosisrubric != null)
                {
                    return Ok(diagnosisrubric);
                }
                else
                {
                    errorResponseModel.Message = "Not data found";
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }

        /// <summary>
        /// To delete diagnosis 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("GetThrepoticByDiagonisID")]
        [ProducesResponseType(typeof(DiagnosisRubricDeleteTabWise), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetThrepoticByDiagonisID(int diagnosisId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisrubric = _diagnosisService.GetdiagnosisTherapeuticsDetail(diagnosisId, ref errorResponseModel);

                if (diagnosisrubric != null)
                {
                    return Ok(diagnosisrubric);
                }
                else
                {
                    errorResponseModel.Message = "No data found";
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex);
            }
        }
    }
}
