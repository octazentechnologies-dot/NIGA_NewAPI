using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

using Homeocentrum.Niga.NewAPI.Domain.Authorization;
namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for Section entity 
    /// </summary>
    [Route("api/section")]
    [ApiController]
   // //[Authorize]
    public class SectionController : ControllerBase
    {
        private readonly ISectionRepository _sectionService;
        private readonly IMapper _mapper;

        public SectionController(ISectionRepository sectionService, IMapper mapper)
        {
            _sectionService = sectionService;
            _mapper = mapper;
        }

        /// <summary>
        /// To get section by Section ID 
        /// </summary>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        [HttpGet("GetSectionDetailsById/{sectionId}")]
        public async Task<object> GetSectionById(long sectionId)
        {
            try
            {
                var sectionModel = await _sectionService.GetSectionDetailsById(sectionId);

                if (sectionModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = sectionModel
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
        /// To get all Sections
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetSectionList")]
        public async Task<List<SectionList>> ShowSectionList([FromQuery] ParameterParams parameterParams)
        {
            var sectionList = await _sectionService.getAllSections(parameterParams);
            Response.AddPaginationHeader(sectionList.CurrentPage, sectionList.PageSize,
                    sectionList.TotalCount, sectionList.TotalPages);
            return sectionList;
        }

        [HttpPost("AddSection")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewSection(SectionMasterDto SectionMasterDto)
        {
            try
            {
                var Section = _mapper.Map<SectionMaster>(SectionMasterDto);
                _sectionService.SaveSection(Section);
                if (await _sectionService.SaveAllAsync())
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


        [HttpPost("UpdateSectionDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateSectionDetails(SectionMasterDto updateSectionDto)
        {
            try
            {
                var data = await _sectionService.GetSectionById(updateSectionDto.SectionId);
                _mapper.Map(updateSectionDto, data);
                _sectionService.UpdateSection(data);
                 if (await _sectionService.SaveAllAsync())
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

        [HttpPost("DeleteSectionDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteSectionDetails(int Id)
        {
            var data = await _sectionService.GetSectionById(Id);
            try
            {
                _sectionService.DeleteSection(data);
                if (await _sectionService.SaveAllAsync())
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


        [HttpGet]
        [Route("GetAllRemedyByFilter")]
        [ProducesResponseType(typeof(SectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult getAllRemedyByFilter(string search, int SectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var sectionModel = _sectionService.getAllRemedyByFilter(search, SectionId, ref errorResponseModel);

                if (sectionModel != null)
                {
                    return Ok(sectionModel);
                }
                return BadRequest(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("GetSectionDD")]
        public async Task<List<SectionMasterDto>> GetSectionDD(string? Search)
        {
            return await _sectionService.GetSectionDD(Search);
        }

    }
}