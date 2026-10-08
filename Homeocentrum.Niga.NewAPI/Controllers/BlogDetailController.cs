#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Errors;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Compatibility;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [OldApiContract]
    public class BlogDetailController : BaseAPIController
    {
        IBlogDetailService blogDetailService;

        /// <summary>
        /// Used to initialize controller and inject blogdetail service
        /// </summary>
        /// <param name="_blogDetailService"></param>
        public BlogDetailController(IBlogDetailService _blogDetailService)
        {
            blogDetailService = _blogDetailService;
        }

        /// <summary>
        /// To get blogdetail by blogId 
        /// </summary>
        /// <param name="blogId"></param>
        /// <returns></returns>
        [HttpGet("GetBlogDetailById/{blogId}")]
        [ProducesResponseType(typeof(BlogDetailModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetBlogDetailById(long blogId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var blogModel = blogDetailService.GetBlogDetailById(blogId, ref errorResponseModel);

                if (blogModel != null)
                {
                    return Ok(blogModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To Get all BlogDetail
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetAllBlogDetail")]
        [ProducesResponseType(typeof(BlogDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetAllBlogDetail()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var blogDetails = blogDetailService.GetAllBlogDetail(ref errorResponseModel);

                if (blogDetails != null)
                {
                    return Ok(blogDetails);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add/update new blogDetails 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [ProducesResponseType(typeof(BlogDetailModel1), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult SaveBlogDetail(BlogDetailModel1 model)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var blogmodel = blogDetailService.SaveBlogDetail(model, ref errorResponseModel);

                if (blogmodel != null)
                {
                    return Ok(blogmodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete blogdetails 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        [Route("DeleteBlogDetail")]
        [ProducesResponseType(typeof(BlogDetailModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult DeleteBlogDetail(long blogId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var blogModel = blogDetailService.DeleteBlogDetail(blogId, ref errorResponseModel);

                if (blogModel != null)
                {
                    return Ok(blogModel);
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
