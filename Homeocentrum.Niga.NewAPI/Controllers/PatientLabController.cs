using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Controllers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// PatientLabController
    /// </summary>
    [Route("api/PatientLab")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class PatientLabController : BaseAPIController
    {
        IPatientLabOrderServices _patientLabOrderServices;
        IPatientLabEntryServices _patientLabEntryServices;
        ILabTestMasterServices _labTestMasterServices;
        private readonly IPatientAccessGuard _patientAccess;
        private readonly Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext _context;
        /// <summary>
        /// Used to initialize controller and inject master service
        /// </summary>
        /// <param name="patientLabOrderServices"></param>
        /// <param name="patientLabEntryServices"></param>
        /// <param name="labTestMasterServices"></param>
        public PatientLabController(IPatientLabOrderServices patientLabOrderServices, IPatientLabEntryServices patientLabEntryServices,ILabTestMasterServices labTestMasterServices,
            IPatientAccessGuard patientAccess, Homeocentrum.Niga.NewAPI.Domain.Data.NIGACentrumContext context)
        {
            _patientLabOrderServices = patientLabOrderServices;
            _patientLabEntryServices = patientLabEntryServices;
            _labTestMasterServices = labTestMasterServices;
            _patientAccess = patientAccess;
            _context = context;
        }

        private ObjectResult PatientForbidden()
            => StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

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
                return this.ServerError(ex);
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
        public async Task<IActionResult> GetPatientLabOrder()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var visible = await _patientAccess.VisiblePatientIdsAsync(User);
                var patientLabOrderModel = _patientLabOrderServices.GetAllPatinetLabOrder(ref errorResponseModel);

                if (patientLabOrderModel != null)
                {
                    return Ok(visible == null ? patientLabOrderModel : patientLabOrderModel.Where(x => visible.Contains(x.PatientId)).ToList());
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
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
        public async Task<IActionResult> GetPatientLabOrder(int patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            if (!await _patientAccess.CanAccessPatientAsync(User, patientId))
                return PatientForbidden();
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
                return this.ServerError(ex);
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
        public async Task<IActionResult> GetPatientLabEntry()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var visible = await _patientAccess.VisiblePatientIdsAsync(User);
                var patientLabEntryModel = _patientLabEntryServices.GetAllPatientLabEntry(ref errorResponseModel);

                if (patientLabEntryModel != null)
                {
                    return Ok(visible == null ? patientLabEntryModel : patientLabEntryModel.Where(x => visible.Contains(x.PatientId)).ToList());
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
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
        public async Task<IActionResult> GetPatientLabEntry(int patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            if (!await _patientAccess.CanAccessPatientAsync(User, patientId))
                return PatientForbidden();
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
                return this.ServerError(ex);
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
        [OldApiContract]
        public async Task<IActionResult> SavePatientLabOrder(PatientLabOrderModel patientLabOrderModel)
        {
            ErrorResponseModel errorResponseModel = null;
            if (patientLabOrderModel == null || !await _patientAccess.CanAccessPatientAsync(User, patientLabOrderModel.PatientId))
                return PatientForbidden();
            if (patientLabOrderModel.PatientOrderedTestId != 0)
            {
                var existingPatientId = await _context.PatientLabOrders.AsNoTracking()
                    .Where(x => x.PatientOrderedTestId == patientLabOrderModel.PatientOrderedTestId)
                    .Select(x => (int?)x.PatientId)
                    .FirstOrDefaultAsync();
                if (existingPatientId.HasValue && !await _patientAccess.CanAccessPatientAsync(User, existingPatientId.Value))
                    return PatientForbidden();
            }
            try
            {
                var jwtUserId = User.GetUserId();
                if (jwtUserId > 0)
                    patientLabOrderModel.UserId = jwtUserId;

                var response = await _patientLabOrderServices.SavePatinetLabOrder(patientLabOrderModel);

                if (response == "Not found")
                    return NotFound(response);
                if (response != null)
                {
                    return Ok(patientLabOrderModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
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
        [OldApiContract]
        public async Task<IActionResult> SavePatientLabEntry(PatientLabEntryModel patientLabEntryModel)
        {
            ErrorResponseModel errorResponseModel = null;
            if (patientLabEntryModel == null || !await _patientAccess.CanAccessPatientAsync(User, patientLabEntryModel.PatientId))
                return PatientForbidden();
            if (patientLabEntryModel.PatientLabId != 0)
            {
                var existingPatientId = await _context.PatientLabEntries.AsNoTracking()
                    .Where(x => x.PatientLabId == patientLabEntryModel.PatientLabId)
                    .Select(x => (int?)x.PatientId)
                    .FirstOrDefaultAsync();
                if (existingPatientId.HasValue && !await _patientAccess.CanAccessPatientAsync(User, existingPatientId.Value))
                    return PatientForbidden();
            }
            try
            {
                var jwtUserId = User.GetUserId();
                if (jwtUserId > 0)
                    patientLabEntryModel.EnteredBy = jwtUserId;

                var response = await _patientLabEntryServices.SavePatientLabEntry(patientLabEntryModel);

                if (response == "Not found")
                    return NotFound(response);
                if (response != null)
                {
                    return Ok(patientLabEntryModel);
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
