using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Security;


namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for Clinical Question entity 
    /// </summary>
    [Route("api/clipboardRubrics")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class ClipboardRubricsController : BaseAPIController
    {
        IClipboardRubricsService _clipboardRubricsService;
        /// <summary>
        /// Used to initialize controller and inject clinical questions
        /// </summary>
        /// <param name="clinicalquestionsService"></param>
        public ClipboardRubricsController(IClipboardRubricsService clipboardRubricsService)
        {
            _clipboardRubricsService = clipboardRubricsService;
        }

        /// <summary>
        /// To get all Clipboard Rubrics
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(ClipboardRubricsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClipboardRubrics()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsModelList = _clipboardRubricsService.GetClipboardRubrics(ref errorResponseModel);

                if (clipboardRubricsModelList != null)
                {
                    return Ok(clipboardRubricsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To add new clipboard Rubrics 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(ClipboardRubricsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SaveClipboardRubrics(List<ClipboardRubricsModel>  clipboardRubricsModels)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsModel = _clipboardRubricsService.SaveClipboardRubrics(clipboardRubricsModels, ref errorResponseModel);

                if (clipboardRubricsModel != null)
                {
                    return Ok(clipboardRubricsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all Clipboard Rubrics by patientid
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet]
        [Route("GetClipboardRubricsPatientId/{PatientId}")]
        [ProducesResponseType(typeof(ClipboardRubricsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetClipboardRubricsPatientId(int PatientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsModelList = _clipboardRubricsService.GetClipboardRubricsPatientId(PatientId, ref errorResponseModel);

                if (clipboardRubricsModelList != null)
                {
                    return Ok(clipboardRubricsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
        /// <summary>
        /// To delete clipboard Rubrics
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteClipboardRubrics")]
        [ProducesResponseType(typeof(ClipboardRubricsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult DeleteClipboardRubrics(ClipboardRubricsModel clipboardRubricsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsmodel = _clipboardRubricsService.DeleteClipboardRubrics(clipboardRubricsModel, ref errorResponseModel);

                if (clipboardRubricsmodel != null)
                {
                    return Ok(clipboardRubricsmodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        [HttpPost]
        [Route("GetRubricsDetailsBySubsectionId")]
        [ProducesResponseType(typeof(ClipboardRubricsModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRubricsDetailsBySubsectionId(List<ClipboardRubricsModel1> lstIntensity)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsModelList = _clipboardRubricsService.GetRubricsDetailsBySubsectionId( lstIntensity, ref errorResponseModel);

                if (clipboardRubricsModelList != null)
                {
                    return Ok(clipboardRubricsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        [HttpPost]
        [Route("GetCommanUnCommanRubricsDetailsBySubsectionId")]
        [ProducesResponseType(typeof(ClipboardRubricsModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetCommanUnCommanRubricsDetailsBySubsectionId(List<ClipboardRubricsModel1> lstIntensity)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsModelList = _clipboardRubricsService.GetCommanUnCommanRubricsDetailsBySubsectionId(lstIntensity, ref errorResponseModel);

                if (clipboardRubricsModelList != null)
                {
                    return Ok(clipboardRubricsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("GetRepertorizarionRemedy")]
        [ProducesResponseType(typeof(RepertorizarionRemedyModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRepertorizarionRemedy(RepertorizarionRemedyInputModel inputModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var repertorizarionRemedyList = _clipboardRubricsService.GetRepertorizarionRemedy(inputModel, ref errorResponseModel);

                if (repertorizarionRemedyList != null)
                {
                    return Ok(repertorizarionRemedyList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        //[HttpPost]
        //[Route("GetCommanUnCommanRubricsDetails")]
        //[ProducesResponseType(typeof(ClipboardRubricsModel1), 200)]
        //[ProducesResponseType(typeof(string), 404)]
        //[ProducesResponseType(typeof(string), 400)]
        //[ProducesResponseType(typeof(string), 500)]
        //public IActionResult GetCommanUnCommanRubricsDetails(ClipboardRUbricModel clipboardRUbricModel)
        //{
        //    ErrorResponseModel errorResponseModel = null;
        //    try
        //    {
        //        var clipboardRubricsModelList=new ClipboardRemedyModel();
        //        if (clipboardRUbricModel.WithEliminateRubric.Count > 0)
        //        {
        //            clipboardRubricsModelList = _clipboardRubricsService.GetCommanUnCommanRubricsDetailsByElemation(clipboardRUbricModel, ref errorResponseModel);
        //        }
        //        else
        //        {
        //            //var clipboardRubricsModelList = _clipboardRubricsService.GetCommanUnCommanRubricsDetailsBySubsectionId(lstIntensity, ref errorResponseModel);
        //            clipboardRubricsModelList = new ClipboardRemedyModel();
        //        }


        //        if (clipboardRubricsModelList != null)
        //        {
        //            return Ok(clipboardRubricsModelList);
        //        }
        //        return ReturnErrorResponse(errorResponseModel);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
        //    }
        //}


        [HttpPost]
        [Route("GetCommanUnCommanRubricsDetails")]
        [ProducesResponseType(typeof(ClipboardRubricsModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetCommanUnCommanRubricsDetails(List<ClipboardRubricsModel1> lstIntensity)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                //var clipboardRubricsModelList= _clipboardRubricsService.GetCommanUnCommanRubricsDetailsBySubsectionId1(lstIntensity, ref errorResponseModel);
                var clipboardRubricsModelList = _clipboardRubricsService.GetCommanUnCommanRubricsDetailsBySubsectionIdFinal(lstIntensity, ref errorResponseModel);

                if (clipboardRubricsModelList != null)
                {
                    return Ok(clipboardRubricsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("GetEliminationData")]
        [ProducesResponseType(typeof(ClipboardRubricsModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetEliminationData(ClipboardRUbricModel clipboardRUbricModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var clipboardRubricsModelList = _clipboardRubricsService.GetCommanUnCommanEliminationData(clipboardRUbricModel, ref errorResponseModel);

                if (clipboardRubricsModelList != null)
                {
                    return Ok(clipboardRubricsModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}
