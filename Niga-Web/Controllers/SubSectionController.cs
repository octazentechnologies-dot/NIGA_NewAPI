using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace NIGA.Centrum.API.Controllers
{
    /// <summary>
    /// APIs for SubSection entity 
    /// </summary>
    [Route("api/subsection")]
    [ApiController]
   // //[Authorize]
    public class SubSectionController : ControllerBase
    {
        private readonly ISubSectionRepository _subSectionService;
        private readonly IMapper _mapper;

        public SubSectionController(ISubSectionRepository subSectionService, IMapper mapper)
        {
            _subSectionService = subSectionService;
            _mapper = mapper;
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
        public async Task<object> AddNewSection(AddSubSectionModel subSection)
        {
            try
            {
                var Section = _mapper.Map<SubSectionMaster>(subSection);
                _subSectionService.SaveSubSection(Section);
                if (await _subSectionService.SaveAllAsync())
                {
                    if (subSection.SubLanguageDetail != null)
                    {
                        foreach (var languageDetail in subSection.SubLanguageDetail)
                        {
                            var language = _mapper.Map<SubSectionLanguageDetail>(languageDetail);
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
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
            }
        }


        [HttpPost("UpdateSectionDetails")]
        public async Task<object> UpdateSectionDetails(AddSubSectionModel updatesubsection)
        {
            try
            {
                var data = await _subSectionService.GetSubSectionById(updatesubsection.SubSectionId);
                _mapper.Map(updatesubsection, data);
                _subSectionService.UpdateSubSection(data);
                if (await _subSectionService.SaveAllAsync())
                {
                    if (updatesubsection.SubLanguageDetail != null)
                    {
                        foreach (var languageDetail in updatesubsection.SubLanguageDetail)
                        {
                            var languageDetails = await _subSectionService.GetSubLanguageById(languageDetail.SubSectionLanguageId);
                            _mapper.Map(languageDetail, languageDetails);
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
                                _mapper.Map(rubric, referencerubricDetails);
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return Ok(result);

            }
        }

        [HttpPost("DeleteSubSectionDetails/{Id}")]
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return BadRequest(result);

            }

        }

        [HttpPost("DeleteReferenceRubricDetails/{Id}")]
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return BadRequest(result);

            }

        }

        [HttpPost("DeleteSubLanguageDetails/{Id}")]
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return BadRequest(result);

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
                var subsectionModelList = _subSectionService.GetSubSectionsByDate(userId, ref errorResponseModel);

                if (subsectionModelList != null)
                {
                    return Ok(subsectionModelList);
                }
                return BadRequest(null);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost("ImportFromExcel")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
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
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("ExportSubSectionsToExcel/{sectionId}")]
        public async Task<IActionResult> ExportSubSectionsToExcel(int sectionId)
        {
            var fileContent = await _subSectionService.ExportSubSectionsToExcel(sectionId);
            var fileName = $"SubSections_Section_{sectionId}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(fileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpPost("UpdateSubSectionsFromExcel")]
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}