using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using API.Extensions;
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
    /// APIs for RemedyGrade entity 
    /// </summary>
    [Route("api/remedyGrade")]
    [ApiController]
   // //[Authorize]
    public class RemedyGradeController : BaseAPIController
    {
        private readonly IRemedyGradeRepository _remedyGradeService;
        public RemedyGradeController(IRemedyGradeRepository remedyGradeService)
        {
            _remedyGradeService = remedyGradeService;
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
                return SafeError.Capture(ex, HttpContext);
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
                var RemedyGrade = RemedyGradeMaster;
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
                 return SafeError.Capture(ex, HttpContext);
            }
        }


        [HttpPost("UpdateRemedyGradeDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateRemedyGradeDetails(RemedyGradeMaster updateRemedyGradeDto)
        {
            try
            {
                var data = await _remedyGradeService.GetRemedyGradeById(updateRemedyGradeDto.GradeId);
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
                return Ok(SafeError.Capture(ex, HttpContext));

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
                return this.ServerError(ex);

            }

        }


      

        // [HttpGet("GetRemedyGradeDD")]
        // public async Task<List<RemedyGradeMaster>> GetRemedyGradeDD(string? Search)
        // {
        //     return await _remedyGradeService.GetRemedyGradeDD(Search);
        // }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To add new Remedy Grade 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(RemedyGradeModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SaveRemedyGrade([FromServices] IRemedyGradeService remedygradeService, RemedyGradeModel remedyGradeModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedygradeModel = remedygradeService.SaveRemedyGrade(remedyGradeModel, ref errorResponseModel);

                if (remedygradeModel != null)
                {
                    return Ok(remedygradeModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete Remedy Grade 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteRemedyGrade")]
        [ProducesResponseType(typeof(RemedyGradeModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteRemedyGrade([FromServices] IRemedyGradeService remedygradeService, RemedyGradeModel remedyGradeModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var remedygradeModel = remedygradeService.DeleteRemedyGrade(remedyGradeModel, ref errorResponseModel);

                if (remedygradeModel != null)
                {
                    return Ok(remedygradeModel);
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