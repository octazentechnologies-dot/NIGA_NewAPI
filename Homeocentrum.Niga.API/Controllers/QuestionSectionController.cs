using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using API.Extensions;
using API.Mapper;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;

using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Compatibility;
namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>
    /// APIs for Question Section entity 
    /// </summary>
    [Route("api/questionSection")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    public class QuestionSectionController : BaseAPIController
    {
        private readonly IQuestionSectionService _questionSectionService;
        public QuestionSectionController(IQuestionSectionService questionSectionService)
        {
            _questionSectionService = questionSectionService;
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
                return SafeError.Capture(ex, HttpContext);
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
                var questionSection = questionSectionModel.ToQuestionSectionMaster();
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
                return SafeError.Capture(ex, HttpContext);
            }
        }

        [HttpPost("UpdateQuestionSectionDetails")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<object> UpdateQuestionSectionDetails(QuestionSectionModel updateQuestionSectionModel)
        {
            try
            {
                var data = await _questionSectionService.GetQuestionSectionById(updateQuestionSectionModel.QuestionSectionId);
                updateQuestionSectionModel.CopyTo(data);
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
                return Ok(SafeError.Capture(ex, HttpContext));
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
                return this.ServerError(ex);
            }
        }


        [HttpGet("GetQuestionSectionDD")]
        public async Task<List<QuestionSectionModel>> GetQuestionSectionDD(string? search)
        {
            return await _questionSectionService.GetQuestionSectionDD(search);
        }


        #region Old API compatible endpoints
#nullable disable

        /// <summary>
        /// To add new question section 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(QuestionSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult SaveQuestionSection([FromServices] IQuestionSectionService questionsectionService, QuestionSectionModel questionSectionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionsectionModel = questionsectionService.SaveQuestionSection(questionSectionModel, ref errorResponseModel);

                if (questionsectionModel != null)
                {
                    return Ok(questionsectionModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete question section 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteQuestionSection")]
        [ProducesResponseType(typeof(QuestionSectionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [OldApiContract]
        public IActionResult DeleteQuestionSection([FromServices] IQuestionSectionService questionsectionService, QuestionSectionModel questionSectionModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var questionsectionModel = questionsectionService.DeleteQuestionSection(questionSectionModel, ref errorResponseModel);

                if (questionsectionModel != null)
                {
                    return Ok(questionsectionModel);
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