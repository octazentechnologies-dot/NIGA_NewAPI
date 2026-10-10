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
    [Authorize]
    [OldApiContract]
    public class AuthorController : BaseAPIController
    {
        IAuthorService _authorService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="authorService"></param>
        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        /// <summary>
        /// To Get all Authors
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(AuthorMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAuthor()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var authorModelList = _authorService.GetAuthor(ref errorResponseModel);

                if (authorModelList != null)
                {
                    return Ok(authorModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
           
        }

        /// <summary>
        /// To Get all Authors for Repertory
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetData")]
        [ProducesResponseType(typeof(AuthorMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAuthorforRepertory()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var authorModelList = _authorService.GetAuthorforRepertory(ref errorResponseModel);

                if (authorModelList != null)
                {
                    return Ok(authorModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
            
        }

        /// <summary>
        /// To add new Author 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(AuthorMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveAuthor(AuthorMasterModel authorModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var AuthorModel = _authorService.SaveAuthor(authorModel, ref errorResponseModel);

                if (AuthorModel != null)
                {
                    return Ok(AuthorModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete author 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteAuthor")]
        [ProducesResponseType(typeof(AuthorMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteAuthor(AuthorMasterModel authorModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var AuthorModel = _authorService.DeleteAuthor(authorModel, ref errorResponseModel);

                if (AuthorModel != null)
                {
                    return Ok(AuthorModel);
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
