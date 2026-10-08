using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for prescription rubric and remedy details.
    /// </summary>
    [Route("api/Prescription")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class PrescriptionController : BaseAPIController
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly ILogger<PrescriptionController> _logger;
        private readonly NIGACentrumContext _context;

        public PrescriptionController(
            IPrescriptionService prescriptionService,
            ILogger<PrescriptionController> logger,
            NIGACentrumContext context)
        {
            _prescriptionService = prescriptionService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get paginated prescription rubric and remedy details by appointment id.
        /// </summary>
        /// <param name="request">AppointmentId (required), PageNumber, PageSize</param>
        /// <returns>Rubric and remedy details with joined names and pagination metadata</returns>
        /// <remarks>
        /// Example: GET api/Prescription/GetPrescriptionDetailsByAppointmentId?AppointmentId=101&amp;pageNumber=1&amp;pageSize=10
        /// </remarks>
        [HttpGet("GetPrescriptionDetailsByAppointmentId")]
        [ProducesResponseType(typeof(PrescriptionDetailsPaginatedApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> GetPrescriptionDetailsByAppointmentId(
            [FromQuery] GetPrescriptionDetailsByAppointmentIdRequest request)
        {
            try
            {
                if (request.AppointmentId <= 0)
                {
                    ModelState.AddModelError(nameof(request.AppointmentId), "AppointmentId is required");
                }

                if (!ModelState.IsValid)
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(GetValidationMessage());
                }

                if (!TryValidatePagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.Failure(validationError!);
                }

                _logger.LogInformation(
                    "GetPrescriptionDetailsByAppointmentId requested. AppointmentId={AppointmentId}, PageNumber={PageNumber}, PageSize={PageSize}",
                    request.AppointmentId,
                    request.PageNumber,
                    request.PageSize);

                if (!await _prescriptionService.AppointmentExistsAsync(request.AppointmentId))
                {
                    return ThreeDBodyPartApiResponseHelper.Failure("Appointment not found");
                }

                var appointment = await _context.PatientAppointments.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.PatientAppId == request.AppointmentId && a.DeleteStatus == false);
                var forbid = DoctorOwnership.ForbidIfNotOwner(User, appointment?.DoctorId);
                if (forbid != null)
                    return forbid;

                var result = await _prescriptionService.GetPrescriptionDetailsByAppointmentIdAsync(request);

                var hasRecords = result.RubricTotalRecords > 0 || result.RemedyTotalRecords > 0;

                _logger.LogInformation(
                    "GetPrescriptionDetailsByAppointmentId completed. AppointmentId={AppointmentId}, RubricTotalRecords={RubricTotalRecords}, RemedyTotalRecords={RemedyTotalRecords}",
                    request.AppointmentId,
                    result.RubricTotalRecords,
                    result.RemedyTotalRecords);

                return ThreeDBodyPartApiResponseHelper.PrescriptionPaginatedSuccess(
                    result,
                    hasRecords
                        ? "Prescription details retrieved successfully."
                        : "No prescription records found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetPrescriptionDetailsByAppointmentId failed for AppointmentId={AppointmentId}",
                    request.AppointmentId);
                return ThreeDBodyPartApiResponseHelper.Error(ex);
            }
        }

        private static bool TryValidatePagination(
            GetPrescriptionDetailsByAppointmentIdRequest request,
            out string? errorMessage)
        {
            if (request.PageNumber < 1)
            {
                errorMessage = "PageNumber must be greater than 0";
                return false;
            }

            if (request.PageSize < 1)
            {
                errorMessage = "PageSize must be greater than 0";
                return false;
            }

            if (request.PageSize > PaginationRequestModel.MaxPageSize)
            {
                errorMessage = $"PageSize cannot exceed {PaginationRequestModel.MaxPageSize}";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private string GetValidationMessage()
        {
            return string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To get diagnosis by Diagnosis ID 
        /// </summary>
        /// <param name="diagnosisId"></param>
        /// <returns></returns>
        [HttpPost("SavePrescriptionDetail")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult SavePrescriptionDetail([FromServices] IPrescriptionService prescriptionService, [FromServices] IPatientAppointmentService patientAppointmentService, PrescriptionDetailModel prescriptionDetail)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var deny = ForbidAppointmentIfNotOwner(patientAppointmentService, prescriptionDetail?.AppointmentId ?? 0);
                if (deny != null)
                    return deny;

                var diagnosisModel = prescriptionService.SavePrescriptionDetail(prescriptionDetail, ref errorResponseModel);

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
        /// To get Prescription Remedy by rubric ID 
        /// </summary>
        /// <param name="diagnosisId"></param>
        /// <returns></returns>
        [HttpPost("GetPrescriptionRemedy")]
        [ProducesResponseType(typeof(List<PrescriptionRemedyViewModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetPrescriptionRemedy([FromServices] IPrescriptionService prescriptionService, List<int?> rubricList)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisModel = prescriptionService.GetPrescriptionRemedy(rubricList, ref errorResponseModel);

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

        private IActionResult ForbidAppointmentIfNotOwner(IPatientAppointmentService patientAppointmentService, int appointmentId)
        {
            if (appointmentId <= 0)
                return BadRequest("AppointmentId is required.");

            ErrorResponseModel lookupError = null;
            var appointment = patientAppointmentService.GetPatientAppById(appointmentId, ref lookupError);
            if (appointment == null)
                return ReturnErrorResponse(lookupError);

            return DoctorOwnership.ForbidIfNotOwner(User, appointment.DoctorId);
        }

#nullable restore
        #endregion
    }
}
