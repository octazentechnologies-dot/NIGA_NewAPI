using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using API.Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;

using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Compatibility;
namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>
    /// APIs for SubSection entity 
    /// </summary>
    [Route("api/subsection")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class SubSectionController : BaseAPIController
    {
        private readonly ISubSectionRepository _subSectionService;

        public SubSectionController(ISubSectionRepository subSectionService)
        {
            _subSectionService = subSectionService;
        }

        // /// <summary>
        // /// To get section by Section ID 
        // /// </summary>
        // /// <param name="sectionId"></param>
        // /// <returns></returns>
        // [HttpGet("GetSectionDetailsById/{sectionId}")]
        // public object GetSectionById(long sectionId)
        // {
        //     try
        //     {
        //         var sectionModel = _subSectionService.GetSectionDetailsById(sectionId);

        //         if (sectionModel != null)
        //         {
        //             return new
        //             {
        //                 Status = 200,
        //                 Data = sectionModel
        //             };
        //         }
        //         else
        //         {
        //             return new
        //             {
        //                 Status = 401,
        //                 Data = "No Data Found"
        //             };
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         return new
        //         {
        //             Status = 500,
        //             Message = ex.Message
        //         };
        //     }
        // }

        /// <summary>
        /// To get all Sections
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetSubSectionList")]
        public async Task<List<SubSectionList>> GetSubSectionList([FromQuery] ParameterParams parameterParams)
        {
            var sectionList = await _subSectionService.GetSubSectionList(parameterParams);
            Response.AddPaginationHeader(sectionList.CurrentPage, sectionList.PageSize,
                    sectionList.TotalCount, sectionList.TotalPages);
            return sectionList;
        }
        [HttpGet("GetSubSectionById")]
        public async Task<AddSubSectionModel> GetSubSectionById(long SubsectionId)
        {
            var subsection = await _subSectionService.GetSubSectionById(SubsectionId);
            if(subsection==null){
                return null;
            }
            return subsection;
        }
        [HttpPost("AddSubSection")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewSection(AddSubSectionModel subSection)
        {
            try
            {
                var Section = subSection.ToSubSectionMaster();
                _subSectionService.SaveSubSection(Section);
                if (await _subSectionService.SaveAllAsync())
                {
                    if (subSection.SubLanguageDetail != null)
                    {
                        foreach (var languageDetail in subSection.SubLanguageDetail)
                        {
                            var language = languageDetail.ToSubSectionLanguageDetail();
                            _subSectionService.SaveSubsectionlanguage(language);
                        }
                        await _subSectionService.SaveAllAsync();
                    }

                    if (subSection.Referencerubric != null)
                    {
                        foreach (var rubric in subSection.Referencerubric)
                        {
                            foreach (var refSubSectionId in rubric.RefSubSectionId)
                            {
                                var refSubSection = new ReferenceRubricDetail
                                {
                                    SubSectionId = rubric.SubSectionId,
                                    RefSubSectionId = refSubSectionId,
                                };
                                _subSectionService.SaveReferenceRubric(refSubSection);
                            }
                        }
                        await _subSectionService.SaveAllAsync();
                    }
                    return new
                    {
                        Status = 200,
                        Meassage = "Data Added Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Meassage = "Failed To Add Data"
                    };
                }

            }
            catch (Exception ex)
            {
                return SafeError.Capture(ex, HttpContext);
            }
        }


        [HttpPost("UpdateSectionDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateSectionDetails(AddSubSectionModel updatesubsection)
        {
            try
            {
                var data = await _subSectionService.GetSubSectionById(updatesubsection.SubSectionId);
                updatesubsection.CopyTo(data);
                _subSectionService.UpdateSubSection(data);
                if (await _subSectionService.SaveAllAsync())
                {
                    if (updatesubsection.SubLanguageDetail != null)
                    {
                        foreach (var languageDetail in updatesubsection.SubLanguageDetail)
                        {
                            var languageDetails = await _subSectionService.GetSubLanguageById(languageDetail.SubSectionLanguageId);
                            languageDetail.CopyTo(languageDetails);
                            _subSectionService.UpdateSubsectionlanguage(languageDetails);
                        }
                        await _subSectionService.SaveAllAsync();
                    }

                    if (updatesubsection.Referencerubric != null)
                    {
                        foreach (var rubric in updatesubsection.Referencerubric)
                        {
                            foreach (var refSubSectionId in rubric.RefSubSectionId)
                            {
                                var referencerubricDetails = await _subSectionService.GetReferenceRubricById((int)refSubSectionId);
                                rubric.CopyTo(referencerubricDetails);
                                _subSectionService.UpdateReferenceRubric(referencerubricDetails);
                            }
                        }
                        await _subSectionService.SaveAllAsync();
                    }
                    return new
                    {
                        Status = 200,
                        Meassage = "Data Updated Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Meassage = "Failed To Update Data"
                    };
                }
            }
            catch (Exception ex)
            {
                return Ok(SafeError.Capture(ex, HttpContext));

            }
        }

        [HttpPost("DeleteSubSectionDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteSectionDetails(int Id)
        {
            var data = await _subSectionService.GetSubSectionById(Id);
            try
            {
                _subSectionService.DeleteSubSection(data);
                if (await _subSectionService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Meassage = "Data Deleted Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Meassage = "Failed To Deleted Data"
                    };
                }

            }
            catch (Exception ex)
            {
                return this.ServerError(ex);

            }

        }

        [HttpPost("DeleteReferenceRubricDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteReferenceRubricDetails(int Id)
        {
            var data = await _subSectionService.GetReferenceRubricById(Id);
            try
            {
                _subSectionService.DeleterubricDetails(data);
                if (await _subSectionService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Meassage = "Data Deleted Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Meassage = "Failed To Deleted Data"
                    };
                }

            }
            catch (Exception ex)
            {
                return this.ServerError(ex);

            }

        }

        [HttpPost("DeleteSubLanguageDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteSubLanguageDetails(int Id)
        {
            var data = await _subSectionService.GetSubLanguageById(Id);
            try
            {
                _subSectionService.DeleteLanguageDetails(data);
                if (await _subSectionService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Meassage = "Data Deleted Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Meassage = "Failed To Deleted Data"
                    };
                }

            }
            catch (Exception ex)
            {
                return this.ServerError(ex);

            }

        }
        [HttpGet("GetSubSectionsByDate/{userId}")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetSubSectionsByDate(int userId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                if (!DoctorOwnership.EnsureCallerIsUserOrAdmin(User, userId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied." });

                var subsectionModelList = _subSectionService.GetSubSectionsByDate(userId, ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return BadRequest(null);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// Search rubrics in SubSection master by keyword (contains match on subsection/section name).
        /// </summary>
        [HttpGet("SearchRubricsByKeyword")]
        [ProducesResponseType(typeof(RubricKeywordSearchPagedResponse), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public async Task<IActionResult> SearchRubricsByKeyword(
            [FromQuery] string keyword,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] List<int> sectionIds = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return BadRequest("Keyword is required.");
                }

                var result = await _subSectionService.SearchRubricsByKeywordAsync(keyword, pageNumber, pageSize, sectionIds);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpPost("ImportFromExcel")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ImportFromExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("Please upload a valid Excel file.");

                if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Only .xlsx files are supported.");

                var result = await _subSectionService.ImportSubSectionsFromExcel(file);

                return Ok(new
                {
                    Success = result.Success,
                    Message = result.Message,
                    TotalRows = result.TotalRows,
                    SuccessRows = result.SuccessRows,
                    FailedRows = result.FailedRows
                });
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("ExportSubSectionsToExcel/{sectionId}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ExportSubSectionsToExcel(int sectionId)
        {
            var fileContent = await _subSectionService.ExportSubSectionsToExcel(sectionId);
            var fileName = $"SubSections_Section_{sectionId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost("UpdateSubSectionsFromExcel")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> UpdateSubSectionsFromExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Please upload a valid Excel file.");
            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .xlsx files are supported.");
            var result = await _subSectionService.UpdateSubSectionsFromExcel(file);
            return Ok(new
            {
                Success = result.Success,
                Message = result.Message,
                TotalRows = result.TotalRows,
                SuccessRows = result.SuccessRows,
                FailedRows = result.FailedRows
            });
        }

        [HttpPost("ImportSubSectionsFromExcel")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ImportSubSectionsFromExcelForPC(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Invalid Excel file");

            var result = await _subSectionService.ImportSubSectionsFromExcelPC(file);

            return Ok(new
            {
                result.Success,
                result.Message,
                result.TotalRows,
                result.SuccessRows,
                result.FailedRows
            });
        }

        [HttpGet("DownloadReferenceRubricsTemplate")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DownloadReferenceRubricsTemplate([FromQuery] string format = "excel")
        {
            var normalizedFormat = (format ?? "excel").Trim().ToLowerInvariant();
            var isCsv = normalizedFormat == "csv";
            var bytes = _subSectionService.GetReferenceRubricsImportTemplate(normalizedFormat);
            var contentType = isCsv
                ? "text/csv"
                : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var fileName = isCsv
                ? "ReferenceRubrics_Import_Sample.csv"
                : "ReferenceRubrics_Import_Sample.xlsx";
            return File(bytes, contentType, fileName);
        }

        [HttpPost("ImportReferenceRubrics")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        [ProducesResponseType(typeof(ReferenceRubricImportResultModel), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ImportReferenceRubrics(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Import file is required.");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension is not (".xlsx" or ".xls" or ".csv"))
            {
                return BadRequest("Only .xlsx and .csv files are supported.");
            }

            try
            {
                var result = await _subSectionService.ImportReferenceRubricsAsync(file);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To get subsection by SubSection ID 
        /// </summary>
        /// <param name="subsectionId"></param>
        /// <returns></returns>
        [HttpGet("{subsectionId}")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetSubSectionById([FromServices] ISubSectionService subsectionService, long subsectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModel = subsectionService.GetSubSectionById(subsectionId, ref errorResponseModel);

                if (subsectionModel != null)
                {
                    return Ok(subsectionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new SubSection 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SaveSubSection([FromServices] ISubSectionService subsectionService, List<SubSectionModel> subSectionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModel = subsectionService.SaveSubSection(subSectionModel, ref errorResponseModel);

                if (subsectionModel != null)
                {
                    return Ok(subsectionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete SubSection 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteSubSection")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteSubSection([FromServices] ISubSectionService subsectionService, SubSectionModel subSectionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModel = subsectionService.DeleteSubSection(subSectionModel, ref errorResponseModel);

                if (subsectionModel != null)
                {
                    return Ok(subsectionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all subsections
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetSubSections")]
        [ProducesResponseType(typeof(SubSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetSubSections([FromServices] ISubSectionService subsectionService)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var subsectionModelList = subsectionService.GetSubSections(ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete author 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteSubSectionLanguageDetails")]
        [ProducesResponseType(typeof(SubSectionLanguageDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteSubSectionLanguageDetails([FromServices] ISubSectionService subsectionService, SubSectionLanguageDetailsModel subSectionLanguageDetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var SubSectionLanguageDetailsModel = subsectionService.DeleteSubSectionLanguageDetails(subSectionLanguageDetailsModel, ref errorResponseModel);

                if (SubSectionLanguageDetailsModel != null)
                {
                    return Ok(SubSectionLanguageDetailsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete author 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteReferenceRubricDetails")]
        [ProducesResponseType(typeof(ReferenceRubricDetailsModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteReferenceRubricDetails([FromServices] ISubSectionService subsectionService, ReferenceRubricDetailsModel referenceRubricDetailsModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var ReferenceRubricDetailsModel = subsectionService.DeleteReferenceRubricDetails(referenceRubricDetailsModel, ref errorResponseModel);

                if (ReferenceRubricDetailsModel != null)
                {
                    return Ok(ReferenceRubricDetailsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// Get subsection with its children and child count
        /// </summary>
        [HttpGet("GetSubSectionWithChildrenCount/{subsectionId}")]
        [ProducesResponseType(typeof(List<SubSectionLevelModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetSubSectionWithChildrenCount([FromServices] ISubSectionService subsectionService, long subsectionId)
        {
            ErrorResponseModel errorResponseModel = null;

            try
            {
                var result = subsectionService
                    .GetSubSectionWithChildrenCount(subsectionId, ref errorResponseModel);

                if (result != null && result.Count > 0)
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// Get main parent subsections with child count by section id
        /// </summary>
        [HttpGet("GetMainParentSubSectionsWithChildCount/{sectionId}")]
        [ProducesResponseType(typeof(List<SubSectionLevelModel>), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetMainParentSubSectionsWithChildCount([FromServices] ISubSectionService subsectionService, long sectionId)
        {
            ErrorResponseModel errorResponseModel = null;

            try
            {
                var result = subsectionService
                    .GetMainParentSubSectionsWithChildCount(sectionId, ref errorResponseModel);

                if (result != null && result.Count > 0)
                {
                    return Ok(result);
                }

                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To update MainParentSubsection against subsectionId
        /// </summary>
        /// <param name="subsectionId"></param>
        /// <param name="mainParentSubsection"></param>
        /// <param name="changedBy"></param>
        /// <returns></returns>
        [HttpPost("UpdateMainParentSubsection/{subsectionId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult UpdateMainParentSubsection([FromServices] ISubSectionService subsectionService, long subsectionId, [FromQuery] bool mainParentSubsection, [FromQuery] string changedBy)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var result = subsectionService.UpdateMainParentSubsection(subsectionId, mainParentSubsection, changedBy, ref errorResponseModel);

                if (result != null && !string.IsNullOrEmpty(result))
                {
                    return Ok(result);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("search")]
        [OldApiContract]
        public async Task<IActionResult> Search([FromServices] ISubSectionService subsectionService, [FromQuery] string query,
       [FromQuery] int top = 20)
        {
            var result = await subsectionService.SearchAsync(query, top);
            return Ok(result);
        }

        /// <summary>
        /// Search subsections within a section (autocomplete + tree filter).
        /// </summary>
        [HttpGet("SearchBySection")]
        [ProducesResponseType(typeof(List<SubSectionSearchResultModel>), 200)]
        [OldApiContract]
        public async Task<IActionResult> SearchBySection([FromServices] ISubSectionService subsectionService, [FromQuery] long sectionId,
            [FromQuery] string query,
            [FromQuery] int top = 20)
        {
            if (sectionId <= 0)
            {
                return BadRequest("sectionId is required");
            }

            var result = await subsectionService.SearchBySectionAsync(sectionId, query, top);
            return Ok(result);
        }

        /// <summary>
        /// Global subsection search across all sections (autocomplete + tree).
        /// </summary>
        [HttpGet("SearchGlobal")]
        [ProducesResponseType(typeof(List<SubSectionSearchResultModel>), 200)]
        [OldApiContract]
        public async Task<IActionResult> SearchGlobal([FromServices] ISubSectionService subsectionService, [FromQuery] string query,
            [FromQuery] int top = 20)
        {
            try
            {
                var result = await subsectionService.SearchGlobalAsync(query, top);
                return Ok(result ?? new List<SubSectionSearchResultModel>());
            }
            catch (Exception)
            {
                return Ok(new List<SubSectionSearchResultModel>());
            }
        }

        /// <summary>
        /// Paginated global subsection search for tree results (all matches, level-wise).
        /// </summary>
        [HttpGet("SearchGlobalPaged")]
        [ProducesResponseType(typeof(SubSectionSearchPagedResultModel), 200)]
        [OldApiContract]
        public async Task<IActionResult> SearchGlobalPaged([FromServices] ISubSectionService subsectionService, [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 40)
        {
            try
            {
                var result = await subsectionService.SearchGlobalPagedAsync(query, pageNumber, pageSize);
                return Ok(result ?? new SubSectionSearchPagedResultModel());
            }
            catch (Exception)
            {
                return Ok(new SubSectionSearchPagedResultModel());
            }
        }

        /// <summary>
        /// Paginated section-scoped subsection search for tree results (all matches, level-wise).
        /// </summary>
        [HttpGet("SearchBySectionPaged")]
        [ProducesResponseType(typeof(SubSectionSearchPagedResultModel), 200)]
        [OldApiContract]
        public async Task<IActionResult> SearchBySectionPaged([FromServices] ISubSectionService subsectionService, [FromQuery] long sectionId,
            [FromQuery] string query,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 40)
        {
            if (sectionId <= 0)
            {
                return BadRequest("sectionId is required");
            }

            try
            {
                var result = await subsectionService.SearchBySectionPagedAsync(sectionId, query, pageNumber, pageSize);
                return Ok(result ?? new SubSectionSearchPagedResultModel());
            }
            catch (Exception)
            {
                return Ok(new SubSectionSearchPagedResultModel());
            }
        }

#nullable restore
        #endregion
    }
}