using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interface;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

using Niga_Domain.Authorization;
namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for Remedy entity 
    /// </summary>
    [Route("api/remedy")]
    [ApiController]
   // //[Authorize]
    public class RemedyController : ControllerBase
    {
        private readonly IRemedyService _remedyService;
        private readonly IMapper _mapper;

        public RemedyController(IRemedyService remedyService, IMapper mapper)
        {
            _remedyService = remedyService;
            _mapper = mapper;
        }

        /// <summary>
        /// To get remedy by Remedy ID 
        /// </summary>
        /// <param name="remedyId"></param>
        /// <returns></returns>
        [HttpGet("GetRemedyDetailsById/{remedyId}")]
        public async Task<object> GetRemedyById(long remedyId)
        {
            try
            {
                var remedyModel = await _remedyService.GetRemedyDetailsById(remedyId);

                if (remedyModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = remedyModel
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Data = "No Data Found"
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

        /// <summary>
        /// To get all Remedys
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetRemedyList")]
        public async Task<List<RemedyModel>> ShowRemedyList([FromQuery] ParameterParams parameterParams)
        {
            var remedyList = await _remedyService.GetAllRemedys(parameterParams);
            Response.AddPaginationHeader(remedyList.CurrentPage, remedyList.PageSize,
                    remedyList.TotalCount, remedyList.TotalPages);
            return remedyList;
        }

        [HttpPost("AddRemedy")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewRemedy(RemedyMaster RemedyMaster)
        {
            try
            {
                var Remedy = _mapper.Map<RemedyMaster>(RemedyMaster);
                _remedyService.SaveRemedy(Remedy);
                if (await _remedyService.SaveAllAsync())
                {
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


        [HttpPost("UpdateRemedyDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateRemedyDetails(RemedyMaster updateRemedyDto)
        {
            try
            {
                var data = await _remedyService.GetRemedyById(updateRemedyDto.RemedyId);
                _mapper.Map(updateRemedyDto, data);
                _remedyService.UpdateRemedy(data);
                 if (await _remedyService.SaveAllAsync())
                {
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

        [HttpPost("DeleteRemedyDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteRemedyDetails(int Id)
        {
            var data = await _remedyService.GetRemedyById(Id);
            try
            {
                _remedyService.DeleteRemedy(data);
                if (await _remedyService.SaveAllAsync())
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


      

        [HttpGet("GetRemedyDD")]
        public IActionResult GetCommonUnCommonRemedyBySection(long subSectionId)
        {
             ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedyModelList = _remedyService.GetCommonUnCommonRemedyBySection(subSectionId, ref errorResponseModel);

                if (remedyModelList != null)
                {
                    return Ok(remedyModelList);
                }
                return BadRequest("Data Not Found");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }

        }

        /// <summary>
        /// Import remedies from Excel file
        /// </summary>
        /// <param name="file">Excel file containing remedy data</param>
        /// <returns>Import results</returns>
        [HttpPost("import")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> ImportRemedies(IFormFile file)
        {
            try
            {
                var result = await _remedyService.ImportRemediesFromExcel(file);
                
                if (result.FailureCount == 0)
                {
                    return Ok(new
                    {
                        Status = 200,
                        Message = $"Successfully imported {result.SuccessCount} remedies",
                        Data = result
                    });
                }
                
                return BadRequest(new
                {
                    Status = 400,
                    Message = $"Import completed with {result.FailureCount} errors",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Status = 500,
                    Message = "Import failed",
                    Error = ex.Message
                });
            }
        }
    }
}