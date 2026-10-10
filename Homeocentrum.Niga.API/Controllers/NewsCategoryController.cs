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
using Homeocentrum.Niga.API.Domain.Compatibility;

namespace Homeocentrum.Niga.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [OldApiContract]
    public class NewsCategoryController : BaseAPIController
    {
        INewsCategoryService newsCategoryService;

        /// <summary>
        /// Used to initialize controller and inject news category service
        /// </summary>
        /// <param name="_newsCategoryService"></param>
        public NewsCategoryController(INewsCategoryService _newsCategoryService) 
        {
            newsCategoryService = _newsCategoryService;            
        }

        /// <summary>
        /// To Get all NewsDetail
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAllNewsCategory")]
        [ProducesResponseType(typeof(NewsCategoryModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllNewsCategory()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var newsModelList = newsCategoryService.GetAllNewsCategory(ref errorResponseModel);

                if (newsModelList != null)
                {
                    return Ok(newsModelList);
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
