using API.Extensions;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using System;

using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Security;
namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// APIs for Question Sub Group entity 
    /// </summary>
    [Route("api/questionsubgroup")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class QuestionSubGroupController : ControllerBase
    {
        private readonly IQuestionSubGroupService _questionSubGroupService;
        private readonly IMapper _mapper;

        public QuestionSubGroupController(IQuestionSubGroupService questionSubGroupService, IMapper mapper)
        {
            _questionSubGroupService = questionSubGroupService;
            _mapper = mapper;
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
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
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
                var questionSubGroup = _mapper.Map<QuestionSubgroup>(questionSubGroupModel);
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
                return new
                {
                    Status = 500,
                    Message = ex.Message
                };
            }
        }

        [HttpPost("UpdateQuestionSubGroupDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateQuestionSubGroupDetails(QuestionSubGroupModel updateQuestionSubGroupModel)
        {
            try
            {
                var data = await _questionSubGroupService.GetQuestionSubGroupById(updateQuestionSubGroupModel.QuestionSubgroupId);
                _mapper.Map(updateQuestionSubGroupModel, data);
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return Ok(result);
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
                var result = new
                {
                    Status = 500,
                    Message = ex.Message
                };
                return BadRequest(result);
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("GetQuestionSubGroupDD")]
        public async Task<List<QuestionSubGroupModel>> GetQuestionSubGroupDD(string? search)
        {
            return await _questionSubGroupService.GetQuestionSubGroupDD(search);
        }
    }
}
