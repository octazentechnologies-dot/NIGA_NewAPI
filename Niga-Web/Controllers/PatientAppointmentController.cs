using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.API.Controllers;
using Niga_Domain.API.Helpers;
using Niga_Domain.Authorization;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Security;
using Niga_Domain.Extensions;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for Patient Appointment entity
    /// </summary>
    [Route("api/PatientApp")]
    [ApiController]
    [Authorize]
    public class PatientAppointmentController : BaseAPIController
    {
        private readonly IPatientAppointmentService _PatientAppointmentService;
        private readonly ILogger<PatientAppointmentController> _logger;
        private readonly NIGACentrumContext _context;

        public PatientAppointmentController(
            IPatientAppointmentService PatientAppointmentService,
            ILogger<PatientAppointmentController> logger,
            NIGACentrumContext context)
        {
            _PatientAppointmentService = PatientAppointmentService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Get Patient Appointment by ID
        /// </summary>
        [HttpGet("{PatientAppId}")]
        [ProducesResponseType(typeof(PatientAppointmentModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientAppById(long PatientAppId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var PatientAppModel = _PatientAppointmentService.GetPatientAppById(PatientAppId, ref errorResponseModel);
                if (PatientAppModel != null)
                {
                    return Ok(PatientAppModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (System.Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get paginated appointment list for a patient (latest appointments first).
        /// </summary>
        /// <param name="request">PatientId (required), PageNumber, PageSize, optional SortBy and SortDirection</param>
        /// <returns>Paginated list with success, message, pageNumber, pageSize, totalRecords, totalPages, and resultObject</returns>
        /// <remarks>
        /// Example: GET api/PatientAppointment/GetAppointmentListByPatientId?PatientId=101&amp;pageNumber=1&amp;pageSize=10
        /// </remarks>
        [HttpGet("/api/PatientAppointment/GetAppointmentListByPatientId")]
        [ProducesResponseType(typeof(PaginatedApiResponse<PatientAppointmentListItemModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(PaginatedApiFailureResponse), StatusCodes.Status500InternalServerError)]
        public async Task<object> GetAppointmentListByPatientId(
            [FromQuery] GetAppointmentListByPatientIdRequest request)
        {
            try
            {
                if (request.PatientId <= 0)
                {
                    ModelState.AddModelError(nameof(request.PatientId), "PatientId is required");
                }

                if (!ModelState.IsValid)
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(GetValidationMessage());
                }

                if (!TryValidateAppointmentListPagination(request, out var validationError))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure(validationError!);
                }

                _logger.LogInformation(
                    "GetAppointmentListByPatientId requested. PatientId={PatientId}, PageNumber={PageNumber}, PageSize={PageSize}",
                    request.PatientId,
                    request.PageNumber,
                    request.PageSize);

                if (!await _PatientAppointmentService.PatientExistsAsync(request.PatientId))
                {
                    return ThreeDBodyPartApiResponseHelper.PaginatedFailure("Patient not found");
                }

                var result = await _PatientAppointmentService.GetAppointmentListByPatientIdAsync(request);

                _logger.LogInformation(
                    "GetAppointmentListByPatientId completed. PatientId={PatientId}, TotalRecords={TotalRecords}",
                    request.PatientId,
                    result.TotalRecords);

                return ThreeDBodyPartApiResponseHelper.PaginatedSuccess(
                    result,
                    result.TotalRecords == 0
                        ? "No appointment records found."
                        : "Appointment list retrieved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetAppointmentListByPatientId failed for PatientId={PatientId}",
                    request.PatientId);
                return ThreeDBodyPartApiResponseHelper.PaginatedError(ex.Message);
            }
        }

        /// <summary>
        /// Add or update Patient Appointment
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SavePatientApp(PatientAppointmentModel PatientAppointmentModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                // SEC-05.01 — bind DoctorId from JWT when present; forbid cross-doctor mutate
                var jwtDoctorId = DoctorOwnership.GetDoctorId(User);
                if (jwtDoctorId.HasValue && (PatientAppointmentModel.DoctorId <= 0))
                    PatientAppointmentModel.DoctorId = jwtDoctorId.Value;

                var forbid = DoctorOwnership.ForbidIfNotOwner(User, PatientAppointmentModel.DoctorId);
                if (forbid != null)
                    return forbid;

                // CON-01.02 / CON-02.02 — patient/caregiver booking authorisation
                var roleName = AdminAuthorizationPolicies.GetRoleName(User) ?? string.Empty;
                var isClinicStaff =
                    DoctorOwnership.IsAdminPortalUser(User)
                    || roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase)
                    || roleName.Equals("Reception", StringComparison.OrdinalIgnoreCase)
                    || jwtDoctorId.HasValue;
                if (!isClinicStaff && PatientAppointmentModel.PatientId > 0)
                {
                    var userId = (long)User.GetUserId();
                    var pid = PatientAppointmentModel.PatientId;
                    var allowed =
                        _context.PatientUserMaps.Any(m =>
                            m.UserId == userId && m.PatientId == pid && !m.DeleteStatus)
                        || _context.PatientFamilyMembers.Any(f =>
                            f.OwnerUserId == userId && f.MemberPatientId == pid && !f.DeleteStatus)
                        || _context.CaregiverAuthorizations.Any(c =>
                            c.CaregiverUserId == userId
                            && c.PatientId == pid
                            && !c.DeleteStatus
                            && c.RevokedAt == null);
                    if (!allowed)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { success = false, message = "Not authorised to book as this patient." });
                    }
                }

                var result = _PatientAppointmentService.SavePatientApp(PatientAppointmentModel, ref errorResponseModel);
                if (!string.IsNullOrEmpty(result))
                {
                    return Ok(result);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (System.Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get all Patient cases for a user
        /// </summary>
        [HttpGet("GetCasesByUser/{userId}")]
        [ProducesResponseType(typeof(List<PatientModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetCasesByUser(long userId)
        {
            ErrorResponseModel errorResponseModel = new ErrorResponseModel();
            try
            {
                var PatientModelList = _PatientAppointmentService.GetCasesByUser(userId, ref errorResponseModel);
                if (PatientModelList != null)
                {
                    return Ok(PatientModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (System.Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get appointments for a doctor user on a specific date.
        /// </summary>
        /// <remarks>
        /// Example: GET /api/PatientAppointment/GetAppointmentsByDate?UserId=101&amp;AppointmentDate=2025-06-11
        /// </remarks>
        [HttpGet("/api/PatientAppointment/GetAppointmentsByDate")]
        [ProducesResponseType(typeof(List<PatientAppointmentModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAppointmentsByDate([FromQuery] GetAppointmentsByDateRequest request)
        {
            try
            {
                if (request.UserId <= 0)
                {
                    return BadRequest("UserId is required");
                }

                if (request.AppointmentDate == default)
                {
                    return BadRequest("AppointmentDate is required");
                }

                // SEC-05.01 — non-admin must only query own user id
                if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, request.UserId))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Access denied for this doctor resource." });
                }

                var appointments = await _PatientAppointmentService.GetAppointmentsByDateAsync(request);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "GetAppointmentsByDate failed for UserId={UserId}, AppointmentDate={AppointmentDate}",
                    request.UserId,
                    request.AppointmentDate);
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Update appointment date/time for a particular appointment.
        /// </summary>
        [HttpPost("/api/PatientAppointment/UpdateAppointmentTime")]
        [ProducesResponseType(typeof(PatientAppointmentModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult UpdateAppointmentTime([FromBody] UpdateAppointmentTimeModel model)
        {
            ErrorResponseModel errorResponseModel = null;

            try
            {
                if (model == null || model.PatientAppId <= 0)
                {
                    return BadRequest("Invalid request data");
                }

                var result = _PatientAppointmentService.UpdateAppointmentTime(model, ref errorResponseModel);

                if (result != null)
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get daily appointment schedule for a doctor on a specific date.
        /// </summary>
        [HttpGet("/api/PatientAppointment/GetDailySchedule")]
        [ProducesResponseType(typeof(DoctorDailyScheduleModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDailySchedule([FromQuery] GetDoctorDailyScheduleRequest request)
        {
            try
            {
                if (request.DoctorId <= 0)
                {
                    return BadRequest("DoctorId is required");
                }

                var forbid = DoctorOwnership.ForbidIfNotOwner(User, request.DoctorId);
                if (forbid != null)
                    return forbid;

                if (request.ScheduleDate == default)
                {
                    return BadRequest("ScheduleDate is required");
                }

                var schedule = await _PatientAppointmentService.GetDailyScheduleAsync(request);
                if (schedule == null)
                {
                    return NotFound("Daily schedule not found for this date.");
                }

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Save locked daily appointment schedule for a doctor.
        /// </summary>
        [HttpPost("/api/PatientAppointment/SaveDailySchedule")]
        [ProducesResponseType(typeof(DoctorDailyScheduleModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SaveDailySchedule([FromBody] SaveDoctorDailyScheduleRequest request)
        {
            try
            {
                if (request == null || request.DoctorId <= 0)
                {
                    return BadRequest("DoctorId is required");
                }

                var forbid = DoctorOwnership.ForbidIfNotOwner(User, request.DoctorId);
                if (forbid != null)
                    return forbid;

                if (request.ScheduleDate == default)
                {
                    return BadRequest("ScheduleDate is required");
                }

                if (request.CreatedByUserId <= 0)
                {
                    return BadRequest("CreatedByUserId is required");
                }

                var (result, errorResponseModel) = await _PatientAppointmentService.SaveDailyScheduleAsync(request);
                if (result != null)
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get generated appointment slots for a doctor on a specific date.
        /// </summary>
        [HttpGet("/api/PatientAppointment/GetAppointmentSlots")]
        [ProducesResponseType(typeof(AppointmentSlotsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAppointmentSlots([FromQuery] GetAppointmentSlotsRequest request)
        {
            try
            {
                if (request.DoctorId <= 0)
                {
                    return BadRequest("DoctorId is required");
                }

                if (request.AppointmentDate == default)
                {
                    return BadRequest("AppointmentDate is required");
                }

                var slots = await _PatientAppointmentService.GetAppointmentSlotsAsync(request);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Update appointment status by appointment id
        /// </summary>
        [HttpPost("UpdateAppointmentStatus")]
        [ProducesResponseType(typeof(PatientAppointmentModel), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult UpdateAppointmentStatus(
            [FromBody] UpdateAppointmentStatusModel model)
        {
            ErrorResponseModel errorResponseModel = null;

            try
            {
                if (model == null || model.PatientAppId <= 0 || string.IsNullOrEmpty(model.Status))
                {
                    return BadRequest("Invalid request data");
                }

                var result = _PatientAppointmentService
                    .UpdateAppointmentStatus(model, ref errorResponseModel);

                if (result != null)
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private bool TryValidateAppointmentListPagination(
            GetAppointmentListByPatientIdRequest request,
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
    }
}
