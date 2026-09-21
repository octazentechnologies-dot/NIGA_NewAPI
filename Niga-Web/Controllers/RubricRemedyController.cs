using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Implementation;
using Niga_Domain.Interface;
using Niga_Domain.Interfaces;

using Niga_Domain.Authorization;
namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for bodypart entity 
    /// </summary>
    [Route("api/RubricRemedy")]
    [ApiController]
   // [Authorize]
    public class RubricRemedyController : BaseAPIController
    {
        private readonly IRubricRemedyDetailsService _iRubricRemedyDetailsService;
        private readonly IRubricRemedyImportQueue _importQueue;

        /// <summary>
        /// RubricRemedyController contructor
        /// </summary>
        public RubricRemedyController(
            IRubricRemedyDetailsService iRubricRemedyDetailsService,
            IRubricRemedyImportQueue importQueue)
        {
            _iRubricRemedyDetailsService = iRubricRemedyDetailsService;
            _importQueue = importQueue;
        }


        /// <summary>
        /// To add new BodyPart 
        /// </summary>
        /// <param name="rubricRemedyDetailsModel"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(RubricRemedyDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveRemedyDetails(List<RubricRemedyDetailsModel> rubricRemedyDetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModel = _iRubricRemedyDetailsService.SaveRubricRemedyDetails(rubricRemedyDetailsModel, ref errorResponseModel);

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


        /// <summary>
        /// To add new BodyPart 
        /// </summary>
        /// <param name="RemedyId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRubricRemedyDetails/{RemedyId}")]
        [ProducesResponseType(typeof(RubricRemedyDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRubricRemedyDetails(long RemedyId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
               // _iRubricRemedyDetailsService.GetRubricList(SectionId,ref errorResponseModel);
                var remedyDetailsModel = _iRubricRemedyDetailsService.GetRubricRemedyDetails(RemedyId, ref errorResponseModel);

                if (remedyDetailsModel != null)
                {
                    return Ok(remedyDetailsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To add new BodyPart 
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRemedyCounts/{subSectionId}")]
        [ProducesResponseType(typeof(RemedyCountsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRemedyCounts(int subSectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyDetailsModel = _iRubricRemedyDetailsService.GetRemedyCounts(subSectionId, ref errorResponseModel);

                if (remedyDetailsModel != null)
                {
                    return Ok(remedyDetailsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// To add new BodyPart 
        /// </summary>
        /// <param name="RemedyId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRubricList/")]
        [ProducesResponseType(typeof(RubricRemedyDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRubricList(int SectionId, [FromQuery] NigaParameters nigaParameters)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyDetailsModel = _iRubricRemedyDetailsService.GetRubricList(SectionId,nigaParameters, ref errorResponseModel);

                if (remedyDetailsModel != null)
                {
                    return Ok(remedyDetailsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get Grade details
        /// </summary>
        /// <param name="RemedyId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetGradeDetails/{subSectionId:int}")]
        [ProducesResponseType(typeof(List<GradeRemediesModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetGradeDetails(int subSectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var gradeDetails = _iRubricRemedyDetailsService.GetGradeRemedies(subSectionId, ref errorResponseModel);

                if (gradeDetails != null)
                {
                    return Ok(gradeDetails);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        ///  Get details to edit rubric remedies
        /// </summary>
        /// <param name="subSectionId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRemedyDetailsToEdit/{subSectionId}/{grade}")]
        [ProducesResponseType(typeof(RubricRemedyDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRemedyDetailsToEdit(int subSectionId, int grade)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var rubricRemedyDetails = _iRubricRemedyDetailsService.GetRemedyDetailsToEdit(subSectionId, grade, ref errorResponseModel);

                if (rubricRemedyDetails != null)
                {
                    return Ok(rubricRemedyDetails);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }




        /// <summary>
        /// To get all subsections
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubSections/{sectionId}")]
        [ProducesResponseType(typeof(RubricRemedyViewModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubSections(int sectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModelList = _iRubricRemedyDetailsService.GetSubSections(sectionId, ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // Action created by Vikas More

        // <summary>
        // To get rubric remedy by subSectionId & gradeId 
        // </summary>
        // <param name = "remedyId" ></ param >
        // <param name = "remedyId" ></ param >
        // < returns ></ returns >
        [HttpGet("GetRubricRemedyBySectionIdGreadId/{subSectionId}/{gradeId}")]
        [ProducesResponseType(typeof(RubricRemedyDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRubricRemedyBySectionIdGreadId(int subSectionId, int gradeId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModel = _iRubricRemedyDetailsService.GetRubricRemedyBySectionGread(subSectionId, gradeId, ref errorResponseModel);

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

        /// <summary>
        /// To add new BodyPart 
        /// </summary>
        /// <param name="rubricRemedyDetailsModel"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("SaveUpdateRubricRemedy")]
        [ProducesResponseType(typeof(RubricRemedyDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveUpdateRubricRemedy(RubricRemedyDetailModel rubricRemedyDetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                // int userId = 0;
                // if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                // {
                //     if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                //     {
                //         userId = Convert.ToInt32(((System.Security.Claims.ClaimsIdentity)User.Identity).FindFirst(System.Security.Claims.ClaimTypes.Name).Value);
                //     }
                // }

                int userId = User.GetUserId();

                var remedyModel = _iRubricRemedyDetailsService.SaveUpdateRubricRemedy(rubricRemedyDetailsModel, userId, ref errorResponseModel);

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


        /// <summary>
        /// To delete RemedyRubricAuthorDetails & RubricRemedyDetails 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteRubricRemedyAuthor")]
        [ProducesResponseType(typeof(RubricRemedyDeleteModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteRubricRemedyAuthor(RubricRemedyDeleteModel rubricRemedyDeleteModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosismodel = _iRubricRemedyDetailsService.DeleteRubricRemedyAuthor(rubricRemedyDeleteModel, ref errorResponseModel);

                if (diagnosismodel != null)
                {
                    return Ok(diagnosismodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        // <summary>
        // To update IsSmall rubric
        // </summary>
        // <param name = "remedyId" ></ param >
        // <param name = "remedyId" ></ param >
        // < returns ></ returns >
        [HttpGet("UpdateIsSmallRubric/{rubricRemedyId}/{isSmallRubric}")]
        [ProducesResponseType(typeof(RubricRemedyDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult UpdateIsSmallRubric(int rubricRemedyId, bool isSmallRubric)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModel = _iRubricRemedyDetailsService.UpdateIsSmallRubric(rubricRemedyId, isSmallRubric, ref errorResponseModel);

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

        // <summary>
        // To update IsConformation Rubric
        // </summary>
        // <param name = "rubricRemedyId" ></ param >
        // <param name = "isConformationRubric" ></ param >
        // < returns ></ returns >
        [HttpGet("UpdateIsConfirmationRubric/{rubricRemedyId}/{isConformationRubric}")]
        [ProducesResponseType(typeof(RubricRemedyDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult UpdateIsConfirmationRubric(int rubricRemedyId, bool isConformationRubric)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModel = _iRubricRemedyDetailsService.UpdateIsConfirmationRubric(rubricRemedyId, isConformationRubric, ref errorResponseModel);

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


        /// <summary>
        /// Get Grade details
        /// </summary>
        /// <param name="RemedyId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetGradeDetailsWithoutGrade/{subSectionId:int}")]
        [ProducesResponseType(typeof(List<GradeRemediesModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetGradeDetailsWithoutGrade(int subSectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var gradeDetails = _iRubricRemedyDetailsService.GetGradeRemedies1(subSectionId, ref errorResponseModel);

                if (gradeDetails != null)
                {
                    return Ok(gradeDetails);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Get Grade details
        /// </summary>
        /// <param name="RemedyId"></param>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRubricDetails/{subSectionId:int}")]
        [ProducesResponseType(typeof(RubricDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRubricDetails(int subSectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var gradeDetails = _iRubricRemedyDetailsService.GetRubricDetails(subSectionId, ref errorResponseModel);

                if (gradeDetails != null)
                {
                    return Ok(gradeDetails);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Start background import of Rubric Remedy data from Excel/CSV.
        /// Returns immediately with a jobId to avoid Cloudflare 524 timeouts.
        /// </summary>
        [HttpPost("ImportFromExcel")]
        [ProducesResponseType(typeof(ImportJobStartResult), 202)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [RequestSizeLimit(100_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ImportRubricRemedyFromExcel(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("File not selected or empty");

                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (ext != ".xlsx" && ext != ".csv")
                    return BadRequest("Please upload a valid file (.xlsx or .csv)");

                var start = await _importQueue.EnqueueAsync(file, cancellationToken);
                return Accepted(start);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        /// <summary>
        /// Poll background import job status/progress/result.
        /// </summary>
        [HttpGet("ImportFromExcel/Status/{jobId}")]
        [ProducesResponseType(typeof(ImportJobStatusModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult GetImportFromExcelStatus(string jobId)
        {
            var status = _importQueue.GetStatus(jobId);
            if (status == null)
                return NotFound("Import job not found.");
            return Ok(status);
        }

        /// <summary>
        /// Export all rubric remedy data for a section to Excel.
        /// </summary>
        [HttpGet("ExportRubricsToExcel/{sectionId:int}")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ExportRubricsToExcel(int sectionId)
        {
            try
            {
                if (sectionId <= 0)
                {
                    return BadRequest("Please provide a valid sectionId.");
                }

                var exportResult = await _iRubricRemedyDetailsService.ExportRubricsToExcelAsync(sectionId);
                var safeSectionName = string.Join("_", (exportResult.SectionName ?? $"Section_{sectionId}")
                    .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))
                    .Trim();
                if (string.IsNullOrWhiteSpace(safeSectionName))
                {
                    safeSectionName = $"Section_{sectionId}";
                }

                var fileName = $"Rubrics_{safeSectionName}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
                return File(
                    exportResult.FileBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }

}