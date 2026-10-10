using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Errors;
using API.Extensions;
using API.Mapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using System;

using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Compatibility;
namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>
    /// APIs for Question Sub Group entity 
    /// </summary>
    [Route("api/questionsubgroup")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class QuestionSubGroupController : BaseAPIController
    {
        private readonly IQuestionSubGroupService _questionSubGroupService;
        public QuestionSubGroupController(IQuestionSubGroupService questionSubGroupService)
        {
            _questionSubGroupService = questionSubGroupService;
        }

        /// <summary>
        /// To get question sub group by Question Sub Group ID 
        /// </summary>
        /// <param name="questionSubGroupId"></param>
        /// <returns></returns>
        [HttpGet("GetQuestionSubGroupDetailsById/{questionSubGroupId}")]
        public async Task<object> GetQuestionSubGroupById(long questionSubGroupId)
        {
            try
            {
                var questionSubGroupModel = await _questionSubGroupService.GetQuestionSubGroupDetailsById(questionSubGroupId);

                if (questionSubGroupModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = questionSubGroupModel
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
        /// To get all Question Sub Groups
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionSubGroupList")]
        public async Task<List<QuestionSubGroupModel>> ShowQuestionSubGroupList([FromQuery] ParameterParams parameterParams)
        {
            var questionSubGroupList = await _questionSubGroupService.GetAllQuestionSubGroups(parameterParams);
            Response.AddPaginationHeader(questionSubGroupList.CurrentPage, questionSubGroupList.PageSize,
                    questionSubGroupList.TotalCount, questionSubGroupList.TotalPages);
            return questionSubGroupList;
        }

        [HttpPost("AddQuestionSubGroup")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewQuestionSubGroup(QuestionSubGroupModel questionSubGroupModel)
        {
            try
            {
                var questionSubGroup = questionSubGroupModel.ToQuestionSubgroup();
                _questionSubGroupService.SaveQuestionSubGroup(questionSubGroup);
                if (await _questionSubGroupService.SaveAllAsync())
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

        [HttpPost("UpdateQuestionSubGroupDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateQuestionSubGroupDetails(QuestionSubGroupModel updateQuestionSubGroupModel)
        {
            try
            {
                var data = await _questionSubGroupService.GetQuestionSubGroupById(updateQuestionSubGroupModel.QuestionSubgroupId);
                updateQuestionSubGroupModel.CopyTo(data);
                _questionSubGroupService.UpdateQuestionSubGroup(data);
                if (await _questionSubGroupService.SaveAllAsync())
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

        [HttpPost("DeleteQuestionSubGroupDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteQuestionSubGroupDetails(int Id)
        {
            var data = await _questionSubGroupService.GetQuestionSubGroupById(Id);
            try
            {
                _questionSubGroupService.DeleteQuestionSubGroup(data);
                if (await _questionSubGroupService.SaveAllAsync())
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

        [HttpGet]
        [Route("GetAllQuestionSubGroupsByFilter")]
        [ProducesResponseType(typeof(QuestionSubGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllQuestionSubGroupsByFilter(string search)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionSubGroupModel = _questionSubGroupService.GetQuestionSubGroupDD(search);

                if (questionSubGroupModel != null)
                {
                    return Ok(questionSubGroupModel);
                }
                return BadRequest(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("GetQuestionSubGroupDD")]
        public async Task<List<QuestionSubGroupModel>> GetQuestionSubGroupDD(string? search)
        {
            return await _questionSubGroupService.GetQuestionSubGroupDD(search);
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To add new QuestionSubGroup
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>

        [HttpPost]
        [ProducesResponseType(typeof(QuestionSubGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SaveQuestionSubGroup([FromServices] IQuestionSubGroupService questionsubgroupService, QuestionSubGroupModel questionSubGroupModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionSubGroupModel1 = questionsubgroupService.SaveQuestionSubGroup(questionSubGroupModel, ref errorResponseModel);



                if (questionSubGroupModel1 != null)
                {
                    return Ok(questionSubGroupModel1);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete QuestionSubGroup
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>

        [HttpPost]
        [Route("DeleteQuestionSubGroup")]
        [ProducesResponseType(typeof(QuestionSubGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteQuestionSubGroup([FromServices] IQuestionSubGroupService questionsubgroupService, QuestionSubGroupModel questionSubGroupModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionSubGroupModel1 = questionsubgroupService.DeleteQuestionSubGroup(questionSubGroupModel, ref errorResponseModel);



                if (questionSubGroupModel1 != null)
                {
                    return Ok(questionSubGroupModel1);
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
