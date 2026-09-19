using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Enums;
using Niga_Domain.Helpers;
using Niga_Domain.Interface;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for doctor dashboard entity 
    /// </summary>
    [Route("api/doctorDashBoard")]
    [ApiController]
    public class DoctorDashBoardController : BaseAPIController
    {
        IDoctorDashBoardService _doctorDashBoardService;
        /// <summary>
        /// Used to initialize controller and inject doctor dashboard service
        /// </summary>
        /// <param name="doctorDashBoardService"></param>
        public DoctorDashBoardController(IDoctorDashBoardService doctorDashBoardService)
        {
            _doctorDashBoardService = doctorDashBoardService;
        }

        /// <summary>
        /// To get patient appointment by appointmentDate
        /// </summary>
        /// <param name="appointmentDate"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("GetCountApp")]
        [ProducesResponseType(typeof(DoctorDashBoardModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientAppCount(DoctorDashBoardModel patientAppmodel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, patientAppmodel.UserId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });
                var patientAppModel = _doctorDashBoardService.GetPatientAppCount(patientAppmodel.UserId, patientAppmodel.AppointmentDate, ref errorResponseModel);

                if (patientAppModel != null)
                {
                    return Ok(patientAppModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To get patient appointment by user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(PatientAppointmentModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]

         public async Task<List<PatientAppointmentModel>> GetPatientAppUser([FromQuery] ParameterParams parameterParams)
        {
            
                var patientList = await _doctorDashBoardService.GetPatientAppUserDate(parameterParams);
                Response.AddPaginationHeader(patientList.CurrentPage, patientList.PageSize,
                    patientList.TotalCount, patientList.TotalPages);
                return patientList;
           
        }   

        /// <summary>
        /// Get dropdown values for PatientsStatus enum
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetPatientsStatusDropdown")]
        [ProducesResponseType(typeof(IEnumerable<KeyValuePair<int, string>>), 200)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientsStatusDropdown()
        {
            try
            {
                var enumValues = Enum.GetValues(typeof(PatientsStatus))
                    .Cast<PatientsStatus>()
                    .Select(e => new KeyValuePair<int, string>((int)e, e.GetDisplayName()))
                    .ToList();

                return Ok(enumValues);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get patient statistics grouped by their status
        /// </summary>
        /// <returns>Dictionary of status counts</returns>
        [Authorize]
        [HttpGet("GetPatientStats")]
        [ProducesResponseType(typeof(Dictionary<string, double>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public object GetPatientStats(long userId, DateTime? fromDate , DateTime? toDate)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var stats = _doctorDashBoardService.GetPatientStatusStats(userId, fromDate, toDate, errorResponseModel);
                if (stats != null)
                {
                    return stats;
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get patient stats for dashboard pie and bar charts.
        /// </summary>
        /// <param name="userId">Logged-in doctor user id.</param>
        /// <param name="period">ALL, 1M, 3M, or 6M.</param>
        /// <param name="fromDate">Optional custom start date when period is ALL.</param>
        /// <param name="toDate">Optional custom end date when period is ALL.</param>
        [Authorize]
        [HttpGet("GetPatientStatsCharts")]
        [ProducesResponseType(typeof(PatientStatsChartsResponseModel), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> GetPatientStatsCharts(
            long userId,
            string period = "ALL",
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest("userId is required.");
                }

                if ((fromDate.HasValue && !toDate.HasValue) || (!fromDate.HasValue && toDate.HasValue))
                {
                    return BadRequest("Both fromDate and toDate are required for a custom date range.");
                }

                var stats = await _doctorDashBoardService.GetPatientStatsCharts(userId, period, fromDate, toDate);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Export patient case details for Today or All scope in PDF, Excel, or CSV format.
        /// </summary>
        [HttpGet("ExportPatients")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 404)]
        public async Task<IActionResult> ExportPatients(
            [FromQuery] long userId,
            [FromQuery] string scope = "all",
            [FromQuery] string format = "excel",
            [FromQuery] DateTime? date = null)
        {
            if (userId <= 0)
            {
                return BadRequest("userId is required.");
            }

            if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, userId))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            var normalizedScope = (scope ?? "all").Trim().ToLowerInvariant();
            var normalizedFormat = (format ?? "excel").Trim().ToLowerInvariant();

            if (normalizedScope is not ("today" or "all"))
            {
                return BadRequest("scope must be 'today' or 'all'.");
            }

            if (normalizedFormat is not ("pdf" or "excel" or "csv"))
            {
                return BadRequest("format must be 'pdf', 'excel', or 'csv'.");
            }

            if (normalizedScope == "today" && !date.HasValue)
            {
                return BadRequest("date is required when scope is 'today'.");
            }

            try
            {
                var rows = await _doctorDashBoardService.GetPatientsForExport(userId, normalizedScope, date);
                if (rows == null || rows.Count == 0)
                {
                    return NotFound("No patient data found to export.");
                }

                var includeAppointmentColumns = normalizedScope == "today";
                var fileBytes = normalizedFormat switch
                {
                    "csv" => PatientExportFileBuilder.BuildCsv(rows, includeAppointmentColumns),
                    "pdf" => PatientExportFileBuilder.BuildPdf(
                        rows,
                        includeAppointmentColumns,
                        normalizedScope == "today"
                            ? $"Today's Patients ({date:yyyy-MM-dd})"
                            : "All Patients"),
                    _ => PatientExportFileBuilder.BuildExcel(rows, includeAppointmentColumns),
                };

                var extension = PatientExportFileBuilder.GetFileExtension(normalizedFormat);
                var dateSuffix = normalizedScope == "today" && date.HasValue
                    ? $"_{date:yyyy-MM-dd}"
                    : string.Empty;
                var fileName = $"Patients_{normalizedScope}{dateSuffix}.{extension}";

                return File(fileBytes, PatientExportFileBuilder.GetContentType(normalizedFormat), fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

    }
}