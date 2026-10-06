using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using System.Net;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// API's for Patient entity
    /// </summary>
    /// 
    /// <summary>
    /// APIs for User entity 
    /// </summary>
    [Route("api/patient")]
    [ApiController]
    [Authorize]
    public class PatientController : BaseAPIController
    {
        IPatientService _patientService;
        private readonly NIGACentrumContext _context;
        private readonly ITokenService _tokenService;

        public PatientController(IPatientService patientService, NIGACentrumContext context, ITokenService tokenService)
        {
            _patientService = patientService;
            _context = context;
            _tokenService = tokenService;
        }

        /// <summary>
        /// To Get all Case entries of a doctor
        /// </summary>
        /// <returns></returns>
        [HttpGet("{UserId}")]
        [ProducesResponseType(typeof(PatientModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> GetCases(long UserId, [FromQuery] ParameterParams parameterParams)
        {
            if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, UserId))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            // Route {UserId} is the clinic owner (Doctor.UserId). Always bind it — query UserId alone was optional and broke search.
            parameterParams.UserId = (int)UserId;

            var patientList = await _patientService.GetCases(parameterParams);
            Response.AddPaginationHeader(patientList.CurrentPage, patientList.PageSize,
                patientList.TotalCount, patientList.TotalPages);
            return Ok(patientList);
        }

       

        /// <summary>
        /// Create new Patient. EntityType = DoctorMobile | PatientMobile may call without a token (create only);
        /// EntityType = Web (or empty) needs a token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Post(PatientModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid request, please verify details");
            }
            try
            {
                var isMobile = PatientEntityTypes.IsMobile(model.EntityType);
                var isAuthenticated = User.Identity?.IsAuthenticated == true;
                var isCreate = model.PatientID == 0;

                if (!isAuthenticated)
                {
                    if (!isMobile)
                        return Unauthorized(new { success = false, message = "Login required." });

                    // Without a token anyone could overwrite any patient by id.
                    if (model.PatientID != 0)
                        return Unauthorized(new { success = false, message = "Login required to update a patient." });
                }
                // REC-04.02 — create patient is own-doctor only. Bind DoctorID from JWT; never trust a client DoctorID.
                else if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
                {
                    var jwtDoctor = DoctorOwnership.GetDoctorId(User);
                    if (jwtDoctor.HasValue)
                    {
                        model.DoctorID = jwtDoctor.Value;
                        var forbid = DoctorOwnership.ForbidIfNotOwner(User, model.DoctorID);
                        if (forbid != null)
                            return forbid;
                    }
                    else if (!isMobile)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { success = false, message = "Access denied for this doctor resource." });
                    }
                }

                if (model.PatientID == 0 && string.IsNullOrWhiteSpace(model.PatientName))
                    return BadRequest(new { success = false, message = "Patient name is required." });

                // REC-04.03 — same POST /api/patient. Reception JWT user id is staff id;
                // the case must be stored under the clinic doctor's UserId.
                if (isAuthenticated
                    && string.Equals(DoctorOwnership.GetRoleName(User), "Reception", StringComparison.OrdinalIgnoreCase))
                {
                    var doctorUserId = DoctorOwnership.GetDoctorUserId(User);
                    if (!doctorUserId.HasValue)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden,
                            new { success = false, message = "Reception must belong to a doctor." });
                    }

                    model.LoggedInUser = doctorUserId.Value;
                    model.UserId = doctorUserId.Value;
                }

                var userModel = await _patientService.SavePatient(model);
                if (
                    !string.IsNullOrEmpty(userModel.Message)
                    && userModel.Message.Contains("Successfully", StringComparison.OrdinalIgnoreCase)
                )
                {
                    await AttachTokenForExistingMobileAsync(userModel);
                    if (string.IsNullOrEmpty(userModel.Token) && isCreate && isMobile)
                        await CreatePatientLoginAsync(userModel);
                    return Ok(userModel);
                }

                var errorMessage = new ErrorResponseModel
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Message = userModel.Message ?? "Failed to save patient",
                };
                return ReturnErrorResponse(errorMessage);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }


        /// <summary>
        /// Get patient 
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetPatientDetails/{PatientID}/{caseId}")]
        [ProducesResponseType(typeof(PatientModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> GetPatientDetails(long PatientID,long caseId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientModelList = _patientService.GetPatientDetails(PatientID, caseId, ref errorResponseModel);

                if (patientModelList != null)
                {
                    if (!await CanAccessPatientAsync((int)PatientID, patientModelList.DoctorID))
                        return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });
                    return Ok(patientModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
        /// Save complaints
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("SaveComplaints")]
        [DoctorOnly]
        public IActionResult SaveComplaints(PatientModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid request, please verify details");
            }
            try
            {
                var errorMessage = new ErrorResponseModel();
                var userModel = _patientService.SaveComplaints(model, ref errorMessage);
                if (userModel != "")
                {
                    return Ok(userModel);
                }
                return ReturnErrorResponse(errorMessage);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }



        /// <summary>
        /// To get GetPatientDetails by patientModel 
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        [HttpGet("GetPatientDetailsById/{patientId}")]
        [ProducesResponseType(typeof(GetPatientDetailsById), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientDetailsById(long patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientModel = _patientService.GetPatientDetailsById(patientId, ref errorResponseModel);

                if (patientModel != null)
                {
                    return Ok(patientModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        /// <summary>
                /// To delete Depatient 
                /// </summary>
                /// <param name=""></param>
                /// <returns></returns>
        [HttpPost]
        [Route("Deletepatient")]
        [ProducesResponseType(typeof(PatientModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult Deletepatient(int patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var newsModel = _patientService.Deletepatient(patientId, ref errorResponseModel);



                if (newsModel != null)
                {
                    return Ok(newsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }


        [HttpPost("SaveCaseDetails")]
        [DoctorOnly]
        [ProducesResponseType(typeof(CaseDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SaveCaseDetails(List<CaseDetailsModel> casedetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModel = _patientService.SaveCaseDetails(casedetailsModel, ref errorResponseModel);

                if (remedyModel != null)
                {
                    return Ok(remedyModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>CLN-16.02 — GET complaints for a patient (classic SaveComplaints is POST).</summary>
        [HttpGet("GetComplaints/{patientId}")]
        [DoctorOnly]
        public async Task<IActionResult> GetComplaints(int patientId)
        {
            var caseRow = await _context.CaseEntryDetails.AsNoTracking()
                .FirstOrDefaultAsync(c => c.PatientId == patientId && c.DeleteStatus == false);
            if (caseRow == null)
                return Ok(new { success = true, data = Array.Empty<object>() });

            if (!await CanAccessPatientAsync(patientId, caseRow.DoctorId))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            var rows = await _context.CaseEntryChiefComplaints.AsNoTracking()
                .Where(c => c.CaseId == caseRow.CaseId)
                .Select(c => new
                {
                    c.CaseChiefComplaintId,
                    c.CaseId,
                    c.ChiefComplaintName,
                    c.CreatedByRole
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        /// <summary>CLN-16.02 — GET case details for a case (classic SaveCaseDetails is POST).</summary>
        [HttpGet("GetCaseDetails/{caseId}")]
        [DoctorOnly]
        public async Task<IActionResult> GetCaseDetails(int caseId)
        {
            var caseRow = await _context.CaseEntryDetails.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CaseId == caseId && c.DeleteStatus == false);
            if (caseRow == null)
                return NotFound(new { success = false, message = "Case not found." });

            if (!await CanAccessPatientAsync(caseRow.PatientId, caseRow.DoctorId))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            var rows = await _context.CaseDetails.AsNoTracking()
                .Where(d => d.CaseId == caseId)
                .Select(d => new CaseDetailsModel
                {
                    CaseDetailId = d.CaseDetailId,
                    CaseId = d.CaseId,
                    SubsectionId = d.SubsectionId,
                    IntensityId = d.IntensityId,
                    RemedyCount = d.RemedyCount
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        /// <summary>CLN-18.01 — clinical case PDF (doctor copy).</summary>
        [HttpGet("ExportCaseToPdf/{patientId}/{caseId}")]
        [DoctorOnly]
        public async Task<IActionResult> ExportCaseToPdf(int patientId, int caseId)
        {
            ErrorResponseModel errorResponseModel = null;
            var patient = _patientService.GetPatientDetails(patientId, caseId, ref errorResponseModel);
            if (patient == null)
                return NotFound(new { success = false, message = errorResponseModel?.Message ?? "Case not found." });
            if (!await CanAccessPatientAsync(patientId, patient.DoctorID))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            var complaints = await _context.CaseEntryChiefComplaints.AsNoTracking()
                .Where(c => c.CaseId == caseId)
                .Select(c => c.ChiefComplaintName)
                .ToListAsync();
            var details = await (
                from d in _context.CaseDetails.AsNoTracking()
                join s in _context.SubSectionMasters.AsNoTracking() on d.SubsectionId equals s.SubSectionId into sj
                from s in sj.DefaultIfEmpty()
                where d.CaseId == caseId
                select (s != null ? s.SubSectionName : ("Subsection " + d.SubsectionId)) + " (intensity " + d.IntensityId + ")"
            ).ToListAsync();
            var notes = await _context.AppointmentHistoryNotes.AsNoTracking()
                .Where(n => n.Appointment != null && n.Appointment.PatientId == patientId && n.DeletedStatus != true)
                .OrderByDescending(n => n.HistoryId)
                .Select(n => n.HistoryNote)
                .Take(20)
                .ToListAsync();

            var lines = new List<string>
            {
                "Patient: " + (patient.PatientName ?? string.Empty),
                "PatientId: " + patient.PatientID + "  CaseId: " + caseId,
                "Mobile: " + (patient.MobileNo ?? string.Empty),
                "Diagnosis: " + (patient.DiagnosisIds ?? string.Empty),
                "Complaints: " + string.Join("; ", complaints.Where(x => !string.IsNullOrWhiteSpace(x)).Select(PlainExportText)),
                "Rubrics:",
            };
            lines.AddRange(details.Select(PlainExportText));
            var remedies = await (
                from remedy in _context.PrescriptionRemedyDetails.AsNoTracking()
                join visit in _context.PatientAppointments.AsNoTracking() on remedy.AppointmentId equals visit.PatientAppId
                join master in _context.RemedyMasters.AsNoTracking() on remedy.RemedyId equals master.RemedyId into masters
                from master in masters.DefaultIfEmpty()
                where visit.PatientId == patientId && remedy.DeletedStatus != true
                orderby remedy.PrescriptionRemedyId
                select (master != null ? master.RemedyName : "Remedy " + remedy.RemedyId)
                    + (string.IsNullOrWhiteSpace(remedy.Dose) ? "" : " - " + remedy.Dose)
            ).ToListAsync();
            lines.Add("Prescription:");
            lines.AddRange(remedies.Select(PlainExportText));
            lines.Add("Notes:");
            lines.AddRange(notes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(PlainExportText)!);

            var bytes = ClinicalCasePdfBuilder.Build("Clinical case - doctor copy", lines);
            return File(bytes, "application/pdf", $"Case_{patientId}_{caseId}.pdf");
        }

        [HttpGet("getAllCases")]
        public async Task<List<PatientModel>> getAllCases([FromQuery] ParameterParams parameterParams)
        {
            var sectionList = await _patientService.getAllCases(parameterParams);
            Response.AddPaginationHeader(sectionList.CurrentPage, sectionList.PageSize,
                    sectionList.TotalCount, sectionList.TotalPages);
            return sectionList;
        }

        [HttpGet("ExportCasesToExcel")]
        [DoctorOnly]
        public async Task<IActionResult> ExportCasesToExcel([FromQuery] ParameterParams parameterParams)
        {
            parameterParams ??= new ParameterParams();
            if (!parameterParams.UserId.HasValue || parameterParams.UserId.Value <= 0)
                parameterParams.UserId = User.GetUserId();
            if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, parameterParams.UserId.Value))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            var errorResponseModel = new ErrorResponseModel();
            var cases = await _patientService.getAllCasesForExport(parameterParams);
            if (cases == null || !cases.Any())
            {
            errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            errorResponseModel.Message = "No cases found to export.";
            return NotFound(errorResponseModel);
            }

            using (var workbook = new ClosedXML.Excel.XLWorkbook())
            {
            var worksheet = workbook.Worksheets.Add("Cases");
            worksheet.Cell(1, 1).Value = "Patient ID";
            worksheet.Cell(1, 2).Value = "Patient Name";
            worksheet.Cell(1, 3).Value = "Mobile No";
            worksheet.Cell(1, 4).Value = "Gender";
            worksheet.Cell(1, 5).Value = "Address";
            worksheet.Cell(1, 6).Value = "Date of Birth";
            worksheet.Cell(1, 7).Value = "Date of First Visit";
            worksheet.Cell(1, 8).Value = "Diagnosis";

            int row = 2;
            foreach (var caseItem in cases)
            {
                worksheet.Cell(row, 1).Value = caseItem.PatientID;
                worksheet.Cell(row, 2).Value = caseItem.PatientName;
                worksheet.Cell(row, 3).Value = caseItem.MobileNo;
                worksheet.Cell(row, 4).Value = caseItem.Gender;
                worksheet.Cell(row, 5).Value = caseItem.Address;
                worksheet.Cell(row, 6).Value = caseItem.DateOfBirth?.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 7).Value = caseItem.DateodFirstVisit?.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 8).Value = caseItem.DiagnosisIds;
                row++;
            }

            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                var fileContent = stream.ToArray();
                return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Cases.xlsx");
            }
            }
        }

        /// <summary>
        /// To get GetPatientBackHostory by patientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        [HttpGet("GetPatientBackHistoryById/{patientId}")]
        [DoctorOnly]
        [ProducesResponseType(typeof(PatientAppointmentModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientBackHostoryById(long patientId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientAppointmentModel = _patientService.GetPatientBackHostoryById(patientId, ref errorResponseModel);

                if (patientAppointmentModel != null)
                {
                    if (!DoctorOwnership.IsAdminPortalUser(User))
                    {
                        var jwtDoctorId = DoctorOwnership.GetDoctorId(User);
                        patientAppointmentModel = patientAppointmentModel
                            .Where(x => jwtDoctorId.HasValue && x.DoctorId == jwtDoctorId.Value)
                            .ToList();
                    }
                    return Ok(patientAppointmentModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Download sample patient import template (Excel or CSV).
        /// </summary>
        [HttpGet("DownloadImportTemplate")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public IActionResult DownloadImportTemplate([FromQuery] string format = "excel")
        {
            var normalizedFormat = (format ?? "excel").Trim().ToLowerInvariant();
            var isCsv = normalizedFormat == "csv";
            var bytes = _patientService.GetPatientImportTemplate(normalizedFormat);
            var contentType = isCsv
                ? "text/csv"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = isCsv ? "Patient_Import_Sample.csv" : "Patient_Import_Sample.xlsx";
            return File(bytes, contentType, fileName);
        }

        /// <summary>
        /// DOC-05.02 — bulk import. ACL: caller userId or reception DoctorUserID must match.
        /// </summary>
        [HttpPost("ImportPatients")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        [ProducesResponseType(typeof(PatientImportResultModel), 200)]
        [ProducesResponseType(typeof(string), 400)]
        public async Task<IActionResult> ImportPatients(IFormFile file, [FromForm] long userId, [FromForm] string? userName)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Import file is required.");
            }

            if (userId <= 0)
            {
                return BadRequest("userId is required.");
            }

            if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, userId))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this doctor resource." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".xlsx" or ".xls" or ".csv"))
            {
                return BadRequest("Only .xlsx and .csv files are supported.");
            }

            try
            {
                var result = await _patientService.ImportPatientsAsync(file, userId, userName ?? "IMPORT");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        private async Task<bool> CanAccessPatientAsync(int patientId, int resourceDoctorId)
        {
            if (DoctorOwnership.EnsureDoctorOwns(User, resourceDoctorId))
                return true;

            var jwtDoctor = DoctorOwnership.GetDoctorId(User);
            if (jwtDoctor.HasValue)
            {
                return await _context.PatientAppointments.AsNoTracking().AnyAsync(a =>
                    a.PatientId == patientId && a.DoctorId == jwtDoctor.Value && a.DeleteStatus != true);
            }

            try
            {
                var userId = (long)User.GetUserId();
                return await _context.PatientUserMaps.AsNoTracking().AnyAsync(m =>
                        m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus)
                    || await _context.PatientFamilyMembers.AsNoTracking().AnyAsync(f =>
                        f.OwnerUserId == userId && f.MemberPatientId == patientId && !f.DeleteStatus);
            }
            catch
            {
                return false;
            }
        }

        private static string PlainExportText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            var text = System.Text.RegularExpressions.Regex.Replace(value, "<[^>]+>", " ");
            text = System.Net.WebUtility.HtmlDecode(text);
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
            var safe = new System.Text.StringBuilder(text.Length);
            foreach (var ch in text)
                safe.Append(ch <= 255 ? ch : ' ');
            return safe.ToString();
        }

        /// <summary>
        /// Token when this mobile is already on another Patient row.
        /// </summary>
        private async Task AttachTokenForExistingMobileAsync(PatientModel model)
        {
            var digits = PhoneNormalizer.Digits(model.MobileNo);
            model.IsUserAlreadyRegistered = false;
            if (digits.Length < 8)
                return;

            var patients = await _context.Patients.AsNoTracking()
                .Where(p => p.DeleteStatus != true && p.MobileNo != null && p.PatientId != model.PatientID)
                .Select(p => new { p.PatientId, p.MobileNo })
                .ToListAsync();
            var existing = patients.FirstOrDefault(p => PhoneNormalizer.EqualsNormalized(p.MobileNo, digits));
            if (existing == null)
                return;

            var mappedUserId = await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.PatientId == existing.PatientId && !m.DeleteStatus)
                .OrderByDescending(m => m.IsPrimary)
                .Select(m => (long?)m.UserId)
                .FirstOrDefaultAsync();

            var user = mappedUserId.HasValue
                ? await _context.UserMasters.FirstOrDefaultAsync(u => u.UserId == mappedUserId.Value && !u.DeleteStatus)
                : null;
            if (user == null)
            {
                var rows = await _context.UserMasters.AsNoTracking()
                    .Where(u => !u.DeleteStatus && u.MobileNo != null && u.IsUserActivated == true)
                    .Select(u => new { u.UserId, u.MobileNo, u.RoleId })
                    .ToListAsync();
                var patientRoleId = await _context.RoleMasters.AsNoTracking()
                    .Where(r => r.RoleName == "Patient" && !r.DeleteStatus)
                    .Select(r => (int?)r.RoleId)
                    .FirstOrDefaultAsync();
                var chosen = rows
                    .Where(u => PhoneNormalizer.EqualsNormalized(u.MobileNo, digits))
                    .OrderByDescending(u => patientRoleId.HasValue && u.RoleId == patientRoleId.Value)
                    .FirstOrDefault();
                if (chosen == null)
                    return;
                user = await _context.UserMasters.FirstOrDefaultAsync(u => u.UserId == chosen.UserId && !u.DeleteStatus);
            }
            if (user == null || user.IsUserActivated != true)
                return;

            var roleName = await _context.RoleMasters.AsNoTracking()
                .Where(r => r.RoleId == user.RoleId && !r.DeleteStatus)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(roleName))
                return;

            int? doctorId = await _context.Doctors.AsNoTracking()
                .Where(d => d.UserId == user.UserId && !d.DeleteStatus)
                .Select(d => (int?)d.DoctorId)
                .FirstOrDefaultAsync();

            model.Token = await _tokenService.CreateToken(user, 7 * 24 * 60, roleName, doctorId);
            model.IsUserAlreadyRegistered = true;
        }

        /// <summary>
        /// Mobile create with no existing login: add a Patient login for this mobile, link it to the new
        /// patient as primary, and return its token. Password is random; the app signs in again by OTP.
        /// </summary>
        private async Task CreatePatientLoginAsync(PatientModel model)
        {
            var digits = PhoneNormalizer.Digits(model.MobileNo);
            if (digits.Length < 8 || model.PatientID <= 0)
                return;

            var patientRoleId = await _context.RoleMasters.AsNoTracking()
                .Where(r => r.RoleName == "Patient" && !r.DeleteStatus)
                .Select(r => (int?)r.RoleId)
                .FirstOrDefaultAsync();
            if (!patientRoleId.HasValue)
                return;

            var userName = digits;
            if (await _context.UserMasters.AnyAsync(u => u.UserName == userName && !u.DeleteStatus))
                userName = $"{digits}_{model.PatientID}";

            var nameParts = (model.PatientName ?? string.Empty).Trim()
                .Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var email = model.Email?.Trim();

            var user = new Homeocentrum.Niga.NewAPI.Domain.Master.UserMaster
            {
                UserName = userName,
                UserPassword = UserPasswordHasher.Hash(Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24))),
                MobileNo = digits,
                EmailId = string.IsNullOrEmpty(email) ? null : (email.Length > 50 ? email[..50] : email),
                FirstName = nameParts.Length > 0 ? nameParts[0] : null,
                LastName = nameParts.Length > 1 ? nameParts[1] : null,
                CountryId = model.CountryId > 0 ? model.CountryId : null,
                StateId = model.StateId > 0 ? model.StateId : null,
                RoleId = patientRoleId.Value,
                UserStatus = true,
                IsUserActivated = true,
                DeleteStatus = false,
                EnteredBy = model.EntityType?.Trim(),
                EnteredDate = DateTime.Now
            };
            _context.UserMasters.Add(user);
            await _context.SaveChangesAsync();

            _context.PatientUserMaps.Add(new Homeocentrum.Niga.NewAPI.Domain.Master.PatientUserMap
            {
                UserId = user.UserId,
                PatientId = model.PatientID,
                IsPrimary = true,
                DeleteStatus = false,
                EnteredBy = user.UserId.ToString(),
                EnteredDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            model.UserId = (int)user.UserId;
            model.Token = await _tokenService.CreateToken(user, 7 * 24 * 60, "Patient", null);
            model.IsUserAlreadyRegistered = false;
        }

    }
}