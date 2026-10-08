using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;
using API.Mapper;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Interface;

using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;
namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for Question Group entity 
    /// </summary>
    [Route("api/questionGroup")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class QuestionGroupController : BaseAPIController
    {
        private readonly IQuestionGroupService _questionGroupService;
        public QuestionGroupController(IQuestionGroupService questionGroupService)
        {
            _questionGroupService = questionGroupService;
        }

        /// <summary>
        /// To get question Group by Question Group ID 
        /// </summary>
        /// <param name="questionGroupId"></param>
        /// <returns></returns>
        [HttpGet("GetQuestionGroupDetailsById/{questionGroupId}")]
        public async Task<object> GetQuestionGroupById(long questionGroupId)
        {
            try
            {
                var questionGroupModel = await _questionGroupService.GetQuestionDetailsById(questionGroupId);

                if (questionGroupModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = questionGroupModel
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
        /// To get all Question Groups
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionGroupList")]
        public async Task<List<QuestionGroupModel1>> ShowQuestionGroupList([FromQuery] ParameterParams parameterParams)
        {
            var questionGroupList = await _questionGroupService.GetQuestionGroupList(parameterParams);
            Response.AddPaginationHeader(questionGroupList.CurrentPage, questionGroupList.PageSize,
                    questionGroupList.TotalCount, questionGroupList.TotalPages);
            return questionGroupList;
        }

        [HttpPost("AddQuestionGroup")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewQuestionGroup(QuestionGroupModel questionGroupModel)
        {
            try
            {
                var questionGroup = questionGroupModel.ToQuestionGroupMaster();
                _questionGroupService.SaveQuestionGroup(questionGroup);
                if (await _questionGroupService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Added Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Message = "Failed To Add Data"
                    };
                }
            }
            catch (Exception ex)
            {
                return SafeError.Capture(ex, HttpContext);
            }
        }

        [HttpPost("UpdateQuestionGroupDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateQuestionGroupDetails(QuestionGroupModel updateQuestionGroupModel)
        {
            try
            {
                var data = await _questionGroupService.GetQuestionById(updateQuestionGroupModel.QuestionGroupId);
                updateQuestionGroupModel.CopyTo(data);
                _questionGroupService.UpdateQuestionGroup(data);
                if (await _questionGroupService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Updated Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Message = "Failed To Update Data"
                    };
                }
            }
            catch (Exception ex)
            {
                return Ok(SafeError.Capture(ex, HttpContext));
            }
        }

        [HttpPost("DeleteQuestionGroupDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteQuestionGroupDetails(int Id)
        {
            var data = await _questionGroupService.GetQuestionById(Id);
            try
            {
                _questionGroupService.DeleteQuestionGroup(data);
                if (await _questionGroupService.SaveAllAsync())
                {
                    return new
                    {
                        Status = 200,
                        Message = "Data Deleted Successfully"
                    };
                }
                else
                {
                    return new
                    {
                        Status = 400,
                        Message = "Failed To Delete Data"
                    };
                }
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }


        [HttpGet("GetQuestionGroupByExistanceId")]
        public async Task<List<QuestionGroupModel1>> GetQuestionGroupByExistanceId(long QuestionSectionId)
        {
            return await _questionGroupService.GetQuestionGroupByExistanceId(QuestionSectionId);
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To add new questions group
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(QuestionGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SaveQuestionGroup([FromServices] IQuestionGroupService questiongroupService, QuestionGroupModel questiongroupModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionGroupModel = questiongroupService.SaveQuestionGroup(questiongroupModel, ref errorResponseModel);

                if (questionGroupModel != null)
                {
                    return Ok(questionGroupModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete questions group 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteQuestionGroup")]
        [ProducesResponseType(typeof(QuestionGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteQuestionGroup([FromServices] IQuestionGroupService questiongroupService, QuestionGroupModel questiongroupModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionGroupModel = questiongroupService.DeleteQuestionGroup(questiongroupModel, ref errorResponseModel);

                if (questionGroupModel != null)
                {
                    return Ok(questionGroupModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get GetQuestionGroup by ExistanceId
        /// </summary>
        /// <param name="QuestionSectionId"></param>
        /// <returns></returns>
        [HttpGet("GetQuestionGroupByExistanceId/{QuestionSectionId}")]
        [ProducesResponseType(typeof(QuestionGroupModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [OldApiContract]
        public IActionResult GetQuestionGroupByExistanceId([FromServices] IQuestionGroupService questiongroupService, long QuestionSectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questiongroupModel1 = questiongroupService.GetQuestionGroupByExistanceId(QuestionSectionId, ref errorResponseModel);

                if (questiongroupModel1 != null)
                {
                    return Ok(questiongroupModel1);
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