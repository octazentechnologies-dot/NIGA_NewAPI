using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using API.Extensions;
using API.Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;
namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for Section entity 
    /// </summary>
    [Route("api/section")]
    [ApiController]
   // //[Authorize]
    public class SectionController : BaseAPIController
    {
        private readonly ISectionRepository _sectionService;
        public SectionController(ISectionRepository sectionService)
        {
            _sectionService = sectionService;
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
                return SafeError.Capture(ex, HttpContext);
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
                var Section = SectionMasterDto.ToSectionMaster();
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
                 return SafeError.Capture(ex, HttpContext);
            }
        }


        [HttpPost("UpdateSectionDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateSectionDetails(SectionMasterDto updateSectionDto)
        {
            try
            {
                var data = await _sectionService.GetSectionById(updateSectionDto.SectionId);
                updateSectionDto.CopyTo(data);
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
                return Ok(SafeError.Capture(ex, HttpContext));

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
                return this.ServerError(ex);

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
                return this.ServerError(ex);
            }
        }

        [HttpGet("GetSectionDD")]
        public async Task<List<SectionMasterDto>> GetSectionDD(string? Search)
        {
            return await _sectionService.GetSectionDD(Search);
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To add new Section 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(SectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SaveSection([FromServices] ISectionService sectionService, SectionModel sectionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var sectionmodel = sectionService.SaveSection(sectionModel, ref errorResponseModel);

                if (sectionmodel != null)
                {
                    return Ok(sectionmodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete Section 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteSection")]
        [ProducesResponseType(typeof(SectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteSection([FromServices] ISectionService sectionService, SectionModel sectionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var sectionmodel = sectionService.DeleteSection(sectionModel, ref errorResponseModel);

                if (sectionmodel != null)
                {
                    return Ok(sectionmodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

#nullable restore
        #endregion
    }
}