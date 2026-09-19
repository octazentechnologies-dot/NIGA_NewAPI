using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.API.Helpers;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Interfaces;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for prescription rubric and remedy details.
    /// </summary>
    [Route("api/Prescription")]
    [ApiController]
    [Authorize]
    public class PrescriptionController : ControllerBase
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
                return ThreeDBodyPartApiResponseHelper.Error(ex.Message);
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
    }
}
