#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Errors;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interface;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http.Headers;
using Homeocentrum.Niga.API.Domain.Compatibility;

namespace Homeocentrum.Niga.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [OldApiContract]
    public class NewsDetailController : BaseAPIController
    {
        INewsDetailService newsDetailService;

        //  private readonly IWebHostEnvironment iwebhostingEnvironment;

        /// <summary>
        /// Used to initialize controller and inject news details service
        /// </summary>
        /// <param name="_newsDetailService"></param>
        public NewsDetailController(INewsDetailService _newsDetailService) //IWebHostEnvironment _webHostEnvironment)
        {
            newsDetailService = _newsDetailService;
           // iwebhostingEnvironment = _webHostEnvironment;
        }

        /// <summary>
        /// To get newsdetails by newsId 
        /// </summary>
        /// <param name="newsId"></param>
        /// <returns></returns>
        [HttpGet("GetNewsDetailsbyId/{newsId}")]
        [ProducesResponseType(typeof(NewDetailModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetNewsDetailsbyId(long newsId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var newsdetailModel = newsDetailService.GetNewsDetailsbyId(newsId, ref errorResponseModel);

                if (newsdetailModel != null)
                {
                    return Ok(newsdetailModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new newsdetails 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("SaveNewsDetails")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [ProducesResponseType(typeof(NewDetailModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SaveNewsDetails( NewDetailModel1 model)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var newsModel = newsDetailService.SaveNewsDetails(model, ref errorResponseModel);

                if (newsModel != null)
                {
                    return Ok(newsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete newsdetails 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [Route("DeleteNewsDetails")]
        [ProducesResponseType(typeof(NewDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult DeleteNewsDetails(int newsId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var newsModel = newsDetailService.DeleteNewsDetails(newsId, ref errorResponseModel);



                if (newsModel != null)
                {
                    return Ok(newsModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get newsdetails by newscategoryId 
        /// </summary>
        /// <param name="newscategoryId"></param>
        /// <returns></returns>
        [HttpGet("GetNewsDetailsbyCategoryId/{newscategoryId}")]
        [ProducesResponseType(typeof(NewDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetNewsDetailsbyCategoryId(long newscategoryId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var newsdetailModel = newsDetailService.GetNewsDetailsbyCategoryId(newscategoryId, ref errorResponseModel);

                if (newsdetailModel != null)
                {
                    return Ok(newsdetailModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}
