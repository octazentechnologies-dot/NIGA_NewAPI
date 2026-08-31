using API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interface;
using System.Net;

namespace Niga_Domain.API.Controllers
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

        /// <summary>
        /// Used to initialize controller and inject patient Service
        /// </summary>
        /// <param name="patientService"></param>
        public PatientController(IPatientService patientService)
        {
            _patientService = patientService;
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
        public async Task<List<PatientModel>> GetCases([FromQuery] ParameterParams parameterParams)
        {
           
                var patientList = await _patientService.GetCases(parameterParams);
                Response.AddPaginationHeader(patientList.CurrentPage, patientList.PageSize,
                    patientList.TotalCount, patientList.TotalPages);
                return patientList;
        }

       

        /// <summary>
        /// Create new Patient
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post(PatientModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid request, please verify details");
            }
            try
            {
                var userModel = await _patientService.SavePatient(model);
                if (
                    !string.IsNullOrEmpty(userModel.Message)
                    && userModel.Message.Contains("Successfully", StringComparison.OrdinalIgnoreCase)
                )
                {
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
        public IActionResult GetPatientDetails(long PatientID,long caseId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var patientModelList = _patientService.GetPatientDetails(PatientID, caseId, ref errorResponseModel);

                if (patientModelList != null)
                {
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


  [HttpGet("getAllCases")]
        public async Task<List<PatientModel>> getAllCases([FromQuery] ParameterParams parameterParams)
        {
            var sectionList = await _patientService.getAllCases(parameterParams);
            Response.AddPaginationHeader(sectionList.CurrentPage, sectionList.PageSize,
                    sectionList.TotalCount, sectionList.TotalPages);
            return sectionList;
        }

        [HttpGet("ExportCasesToExcel")]
        public async Task<IActionResult> ExportCasesToExcel([FromQuery] ParameterParams parameterParams)
        {
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
        /// Bulk import patients from Excel or CSV file.
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

    }
}