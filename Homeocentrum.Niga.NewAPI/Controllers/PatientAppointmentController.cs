using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Controllers;
using Homeocentrum.Niga.NewAPI.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;

namespace Homeocentrum.Niga.NewAPI.Controllers
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
                    var forbid = DoctorOwnership.ForbidIfNotOwner(User, PatientAppModel.DoctorId);
                    if (forbid != null)
                        return forbid;
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
        [DoctorOnly]
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

                var ownerDoctorId = await _context.CaseEntryDetails.AsNoTracking()
                    .Where(c => c.PatientId == request.PatientId && c.DeleteStatus == false)
                    .Select(c => (int?)c.DoctorId)
                    .FirstOrDefaultAsync()
                    ?? await _context.PatientAppointments.AsNoTracking()
                        .Where(a => a.PatientId == request.PatientId && a.DeleteStatus != true)
                        .Select(a => (int?)a.DoctorId)
                        .FirstOrDefaultAsync();
                var deny = DoctorOwnership.ForbidIfNotOwner(User, ownerDoctorId);
                if (deny != null)
                    return deny;

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

                // CON-01.02 — a patient books only their own record. Doctor ownership applies to clinic staff.
                var roleName = AdminAuthorizationPolicies.GetRoleName(User) ?? string.Empty;
                var isPatientSide = roleName.Equals("Patient", StringComparison.OrdinalIgnoreCase)
                    || roleName.Equals("Caregiver", StringComparison.OrdinalIgnoreCase);
                if (!isPatientSide)
                {
                    var forbid = DoctorOwnership.ForbidIfNotOwner(User, PatientAppointmentModel.DoctorId);
                    if (forbid != null)
                        return forbid;
                }
                var isClinicStaff =
                    DoctorOwnership.IsAdminPortalUser(User)
                    || roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase)
                    || roleName.Equals("Reception", StringComparison.OrdinalIgnoreCase)
                    || jwtDoctorId.HasValue;

                // REC-13.02 — reception/doctor SPA must not write PaymentStatus=PAID (Account/webhook Phase 6).
                if (isClinicStaff
                    && S3AppointmentRules.IsPaid(PatientAppointmentModel?.PaymentStatus))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "PaymentStatus=PAID cannot be set by the client. Account / webhook is the source of truth (Phase 6)."
                    });
                }

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
                if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, userId))
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Access denied for this doctor resource." });

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
        /// REC-10.02 — Reception may update time only for appointments of their JWT DoctorID (own doctor).
        /// Same endpoint as the treating doctor (REC-10.03).
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

                // REC-10.02 — ACL own doctor (GuardAppointment → JWT DoctorID must match row.DoctorId)
                var timeGuard = GuardAppointment(model.PatientAppId, allowPatient: false);
                if (timeGuard != null)
                    return timeGuard;

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

                var scheduleRole = DoctorOwnership.GetRoleName(User);
                if (!string.IsNullOrWhiteSpace(scheduleRole)
                    && scheduleRole.Equals("Reception", StringComparison.OrdinalIgnoreCase))
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        new { success = false, message = "Reception schedule is read-only." });
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
        /// APT-08.03 — clinic slot grid. This is the public-booking engine.
        /// Public GET /api/Public/Doctors/{id}/Slots and POST .../Bookings call
        /// IPatientAppointmentService.GetAppointmentSlotsAsync. Do not add a second slot calculator.
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

                var forbid = DoctorOwnership.ForbidIfNotOwner(User, request.DoctorId);
                if (forbid != null)
                    return forbid;

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
        /// Update appointment status by appointment id.
        /// REC-09.02 — Reception may update status only for appointments of their JWT DoctorID (own doctor).
        /// Same endpoint as the treating doctor (REC-09.03).
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

                // REC-09.02 — ACL own doctor (GuardAppointment → JWT DoctorID must match row.DoctorId)
                var statusGuard = GuardAppointment(model.PatientAppId, allowPatient: false);
                if (statusGuard != null)
                    return statusGuard;

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

        /// <summary>
        /// APT / PAT-21.02 — formal reschedule for clinic or owning patient (JWT).
        /// Patient app: pick a new slot via GET /api/Public/Doctors/{id}/Slots, then POST here.
        /// REC-06.01 — Reception is authorised for this clinic's appointments only (JWT DoctorID).
        /// Does not invent local paid state; paymentStatus comes from the saved appointment.
        /// </summary>
        [HttpPost("/api/PatientAppointment/RescheduleAppointment")]
        public async Task<IActionResult> RescheduleAppointment([FromBody] RescheduleAppointmentRequest request)
        {
            if (request == null || request.PatientAppId <= 0)
                return BadRequest(new { success = false, message = "PatientAppId is required." });
            var guard = GuardAppointment(request.PatientAppId, allowPatient: true);
            if (guard != null)
                return guard;
            var result = await _PatientAppointmentService.RescheduleAppointmentAsync(
                request, User.GetUserId(), DoctorOwnership.GetRoleName(User));
            return StatusCode(result.StatusCode, result);
        }

        /// <summary>
        /// REC-06.01 — Reception may cancel for this clinic's appointments only (JWT DoctorID).
        /// </summary>
        [HttpPost("/api/PatientAppointment/CancelAppointment")]
        public async Task<IActionResult> CancelAppointment([FromBody] CancelAppointmentRequest request)
        {
            if (request == null || request.PatientAppId <= 0)
                return BadRequest(new { success = false, message = "PatientAppId is required." });
            var guard = GuardAppointment(request.PatientAppId, allowPatient: true);
            if (guard != null)
                return guard;
            var result = await _PatientAppointmentService.CancelAppointmentAsync(
                request, User.GetUserId(), DoctorOwnership.GetRoleName(User));
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("/api/PatientAppointment/ChangeLog/{patientAppId:int}")]
        public async Task<IActionResult> ChangeLog(int patientAppId)
        {
            var guard = GuardAppointment(patientAppId, allowPatient: true);
            if (guard != null)
                return guard;
            var rows = await _PatientAppointmentService.GetChangeLogAsync(patientAppId);
            return Ok(new { success = true, data = rows });
        }

        [HttpPatch("/api/PatientAppointment/{patientAppId:int}/VisitType")]
        public async Task<IActionResult> PatchVisitType(int patientAppId, [FromBody] PatchVisitTypeRequest request)
        {
            var guard = GuardAppointment(patientAppId, allowPatient: false);
            if (guard != null)
                return guard;
            var result = await _PatientAppointmentService.PatchVisitTypeAsync(
                patientAppId, request?.VisitType, request?.ConsultMode);
            return StatusCode(result.StatusCode, result);
        }

        [HttpGet("/api/PatientAppointment/Queue")]
        public async Task<IActionResult> Queue()
        {
            var doctorId = DoctorOwnership.GetDoctorId(User);
            if (!doctorId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Doctor context is required." });
            var rows = await _PatientAppointmentService.GetQueueAsync(doctorId.Value);
            return Ok(new { success = true, data = rows });
        }

        /// <summary>
        /// REC-08.02 — Call next waiting patient for this clinic: sets CalledAt and board Status.
        /// Uses JWT DoctorID (doctor or reception of that clinic).
        /// </summary>
        [HttpPost("/api/PatientAppointment/CallNext")]
        public async Task<IActionResult> CallNext()
        {
            var doctorId = DoctorOwnership.GetDoctorId(User);
            if (!doctorId.HasValue)
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Doctor context is required." });
            var result = await _PatientAppointmentService.CallNextAsync(doctorId.Value);
            return StatusCode(result.StatusCode, result);
        }

        private IActionResult? GuardAppointment(long patientAppId, bool allowPatient)
        {
            var row = _context.PatientAppointments.AsNoTracking()
                .Where(a => a.PatientAppId == patientAppId && a.DeleteStatus != true)
                .Select(a => new { a.DoctorId, a.PatientId })
                .FirstOrDefault();
            if (row == null)
                return NotFound(new { success = false, message = "Appointment not found" });

            // REC-06.01 / REC-09.02 / REC-10.02 — Doctor or Reception with matching JWT DoctorID may act on this clinic only.
            // Reception must never change status / time / reschedule / cancel another doctor's appointments.
            if (DoctorOwnership.EnsureDoctorOwns(User, row.DoctorId))
                return null;

            if (allowPatient)
            {
                var role = DoctorOwnership.GetRoleName(User) ?? string.Empty;
                if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                {
                    var userId = (long)User.GetUserId();
                    var owns = _context.PatientUserMaps.Any(m =>
                        m.UserId == userId && m.PatientId == row.PatientId && !m.DeleteStatus);
                    if (owns)
                        return null;
                }
            }

            return DoctorOwnership.ForbidIfNotOwner(User, row.DoctorId);
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
