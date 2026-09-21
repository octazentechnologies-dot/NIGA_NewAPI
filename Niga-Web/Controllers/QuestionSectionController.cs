using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;
using AutoMapper;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

using Niga_Domain.Authorization;
using Niga_Domain.Security;
namespace Niga_Domain.API.Controllers
{
    /// <summary>
    /// APIs for Question Section entity 
    /// </summary>
    [Route("api/questionSection")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class QuestionSectionController : ControllerBase
    {
        private readonly IQuestionSectionService _questionSectionService;
        private readonly IMapper _mapper;

        public QuestionSectionController(IQuestionSectionService questionSectionService, IMapper mapper)
        {
            _questionSectionService = questionSectionService;
            _mapper = mapper;
        }

        /// <summary>
        /// To get question Section by Question Section ID 
        /// </summary>
        /// <param name="questionSectionId"></param>
        /// <returns></returns>
        [HttpGet("GetQuestionSectionDetailsById/{questionSectionId}")]
        public async Task<object> GetQuestionSectionById(long questionSectionId)
        {
            try
            {
                var questionSectionModel = await _questionSectionService.GetQuestionSectionById(questionSectionId);

                if (questionSectionModel != null)
                {
                    return new
                    {
                        Status = 200,
                        Data = questionSectionModel
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
        /// To get all Question Sections
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetQuestionSectionList")]
        public async Task<List<QuestionSectionModel>> ShowQuestionSectionList([FromQuery] ParameterParams parameterParams)
        {
            var questionSectionList = await _questionSectionService.GetAllQuestionSections(parameterParams);
            Response.AddPaginationHeader(questionSectionList.CurrentPage, questionSectionList.PageSize,
                    questionSectionList.TotalCount, questionSectionList.TotalPages);
            return questionSectionList;
        }

        [HttpPost("AddQuestionSection")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> AddNewQuestionSection(QuestionSectionModel questionSectionModel)
        {
            try
            {
                var questionSection = _mapper.Map<QuestionSectionMaster>(questionSectionModel);
                _questionSectionService.SaveQuestionSection(questionSection);
                if (await _questionSectionService.SaveAllAsync())
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

        [HttpPost("UpdateQuestionSectionDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateQuestionSectionDetails(QuestionSectionModel updateQuestionSectionModel)
        {
            try
            {
                var data = await _questionSectionService.GetQuestionSectionById(updateQuestionSectionModel.QuestionSectionId);
                _mapper.Map(updateQuestionSectionModel, data);
                _questionSectionService.UpdateQuestionSection(data);
                if (await _questionSectionService.SaveAllAsync())
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

        [HttpPost("DeleteQuestionSectionDetails/{Id}")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> DeleteQuestionSectionDetails(int Id)
        {
            var data = await _questionSectionService.GetQuestionSectionById(Id);
            try
            {
                _questionSectionService.DeleteQuestionSection(data);
                if (await _questionSectionService.SaveAllAsync())
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


        [HttpGet("GetQuestionSectionDD")]
        public async Task<List<QuestionSectionModel>> GetQuestionSectionDD(string? search)
        {
            return await _questionSectionService.GetQuestionSectionDD(search);
        }
    }
}