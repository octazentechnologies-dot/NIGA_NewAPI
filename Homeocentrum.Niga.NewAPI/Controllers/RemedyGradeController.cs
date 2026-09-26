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
    /// APIs for RemedyGrade entity 
    /// </summary>
    [Route("api/remedyGrade")]
    [ApiController]
   // //[Authorize]
    public class RemedyGradeController : ControllerBase
    {
        private readonly IRemedyGradeRepository _remedyGradeService;
        private readonly IMapper _mapper;

        public RemedyGradeController(IRemedyGradeRepository remedyGradeService, IMapper mapper)
        {
            _remedyGradeService = remedyGradeService;
            _mapper = mapper;
        }

        /// <summary>
        /// To get remedyGrade by RemedyGrade ID 
        /// </summary>
        /// <param name="remedyGradeId"></param>
        /// <returns></returns>
        [HttpGet("GetRemedyGradeDetailsById/{remedyGradeId}")]
        public async Task<object> GetRemedyGradeById(long remedyGradeId)
        {
            try
            {
                var remedyGradeModel = await _remedyGradeService.GetRemedyGradeDetailsById(remedyGradeId);

                if (remedyGradeModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = remedyGradeModel
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
        /// To get all RemedyGrades
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetRemedyGradeList")]
        public async Task<List<RemedyGradeModel>> ShowRemedyGradeList([FromQuery] ParameterParams parameterParams)
        {
            var remedyGradeList = await _remedyGradeService.GetAllRemedyGrades(parameterParams);
            Response.AddPaginationHeader(remedyGradeList.CurrentPage, remedyGradeList.PageSize,
                    remedyGradeList.TotalCount, remedyGradeList.TotalPages);
            return remedyGradeList;
        }

        [HttpPost("AddRemedyGrade")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewRemedyGrade(RemedyGradeMaster RemedyGradeMaster)
        {
            try
            {
                var RemedyGrade = _mapper.Map<RemedyGradeMaster>(RemedyGradeMaster);
                _remedyGradeService.SaveRemedyGrade(RemedyGrade);
                if (await _remedyGradeService.SaveAllAsync())
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


        [HttpPost("UpdateRemedyGradeDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateRemedyGradeDetails(RemedyGradeMaster updateRemedyGradeDto)
        {
            try
            {
                var data = await _remedyGradeService.GetRemedyGradeById(updateRemedyGradeDto.GradeId);
                _mapper.Map(updateRemedyGradeDto, data);
                _remedyGradeService.UpdateRemedyGrade(data);
                 if (await _remedyGradeService.SaveAllAsync())
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

        [HttpPost("DeleteRemedyGradeDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteRemedyGradeDetails(int Id)
        {
            var data = await _remedyGradeService.GetRemedyGradeById(Id);
            try
            {
                _remedyGradeService.DeleteRemedyGrade(data);
                if (await _remedyGradeService.SaveAllAsync())
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


      

        // [HttpGet("GetRemedyGradeDD")]
        // public async Task<List<RemedyGradeMaster>> GetRemedyGradeDD(string? Search)
        // {
        //     return await _remedyGradeService.GetRemedyGradeDD(Search);
        // }

    }
}