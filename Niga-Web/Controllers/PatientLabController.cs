using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Business.Interface;
using Niga_Domain.API.Controllers;
using Niga_Domain.Business.Interface;
using Niga_Domain.DTOs;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// PatientLabController
    /// </summary>
    [Route("api/PatientLab")]
    [ApiController]
    [Authorize]
    public class PatientLabController : BaseAPIController
    {
        IPatientLabOrderServices _patientLabOrderServices;
        IPatientLabEntryServices _patientLabEntryServices;
        ILabTestMasterServices _labTestMasterServices;
        /// <summary>
        /// Used to initialize controller and inject master service
        /// </summary>
        /// <param name="patientLabOrderServices"></param>
        /// <param name="patientLabEntryServices"></param>
        /// <param name="labTestMasterServices"></param>
        public PatientLabController(IPatientLabOrderServices patientLabOrderServices, IPatientLabEntryServices patientLabEntryServices,ILabTestMasterServices labTestMasterServices)
        {
            _patientLabOrderServices = patientLabOrderServices;
            _patientLabEntryServices = patientLabEntryServices;
            _labTestMasterServices = labTestMasterServices;
        }

        /// <summary>
        /// To get all lab orders
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetAllLabTests")]
        [ProducesResponseType(typeof(LabTestMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllLabTests()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var labTestMasterModel = _labTestMasterServices.GetLabTests(ref errorResponseModel);

                if (labTestMasterModel != null)
                {
                    return Ok(labTestMasterModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        /// To get all lab orders
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetPatientLabOrder")]
        [ProducesResponseType(typeof(PatientLabOrderModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabOrder()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientLabOrderModel = _patientLabOrderServices.GetAllPatinetLabOrder(ref errorResponseModel);

                if (patientLabOrderModel != null)
                {
                    return Ok(patientLabOrderModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }



        /// <summary>
        /// To get all lab orders
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetPatientLabOrder/{patientId}")]
        [ProducesResponseType(typeof(PatientLabOrderModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabOrder(int patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientLabOrderModel = _patientLabOrderServices.GetPatinetLabOrder(patientId, ref errorResponseModel);

                if (patientLabOrderModel != null)
                {
                    return Ok(patientLabOrderModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all lab entries
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetPatientLabEntry")]
        [ProducesResponseType(typeof(PatientLabEntryModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabEntry()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientLabEntryModel = _patientLabEntryServices.GetAllPatientLabEntry(ref errorResponseModel);

                if (patientLabEntryModel != null)
                {
                    return Ok(patientLabEntryModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all lab entries
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetPatientLabEntry/{patientId}")]
        [ProducesResponseType(typeof(PatientLabEntryModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabEntry(int patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientLabEntryModel = _patientLabEntryServices.GetPatientLabEntry(patientId,ref errorResponseModel);

                if (patientLabEntryModel != null)
                {
                    return Ok(patientLabEntryModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all save lab order
        /// </summary>
        /// <param name="patientLabOrderModel"></param>
        /// <returns></returns>
        [HttpPost("SavePatientLabOrder")]
        [ProducesResponseType(typeof(PatientLabOrderModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SavePatientLabOrder(PatientLabOrderModel patientLabOrderModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var response = _patientLabOrderServices.SavePatinetLabOrder(patientLabOrderModel);

                if (response != null)
                {
                    return Ok(patientLabOrderModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get all save lab entries
        /// </summary>
        /// <param name="patientLabEntryModel"></param>
        /// <returns></returns>
        [HttpPost("SavePatientLabEntry")]
        [ProducesResponseType(typeof(PatientLabOrderModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SavePatientLabEntry(PatientLabEntryModel patientLabEntryModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var response = _patientLabEntryServices.SavePatientLabEntry(patientLabEntryModel);

                if (response != null)
                {
                    return Ok(patientLabEntryModel);
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
