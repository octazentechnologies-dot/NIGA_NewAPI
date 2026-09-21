using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;
using AutoMapper;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Master;
using Niga_Domain.Interface;

using Niga_Domain.Authorization;
using Microsoft.AspNetCore.Authorization;
using Niga_Domain.Security;
namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for Question Group entity 
    /// </summary>
    [Route("api/questionGroup")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class QuestionGroupController : ControllerBase
    {
        private readonly IQuestionGroupService _questionGroupService;
        private readonly IMapper _mapper;

        public QuestionGroupController(IQuestionGroupService questionGroupService, IMapper mapper)
        {
            _questionGroupService = questionGroupService;
            _mapper = mapper;
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
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
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
                var questionGroup = _mapper.Map<QuestionGroupMaster>(questionGroupModel);
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
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateQuestionGroupDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateQuestionGroupDetails(QuestionGroupModel updateQuestionGroupModel)
        {
            try
            {
                var data = await _questionGroupService.GetQuestionById(updateQuestionGroupModel.QuestionGroupId);
                _mapper.Map(updateQuestionGroupModel, data);
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return Ok(result);
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return BadRequest(result);
            }
        }


        [HttpGet("GetQuestionGroupByExistanceId")]
        public async Task<List<QuestionGroupModel1>> GetQuestionGroupByExistanceId(long QuestionSectionId)
        {
            return await _questionGroupService.GetQuestionGroupByExistanceId(QuestionSectionId);
        }
    }
}