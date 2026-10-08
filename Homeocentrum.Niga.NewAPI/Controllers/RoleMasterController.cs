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
    [Route("api/roleMaster")]
    [ApiController]
    [Authorize]
    [OldApiContract]
    public class RoleMasterController : BaseAPIController
    {
        IRoleMasterService _roleMasterService;

        /// <summary>
        /// Used to initialize controller and inject rolemaster service
        /// </summary>
        /// <param name="menuMasterService"></param>
        public RoleMasterController(IRoleMasterService roleMasterService)
        {
            _roleMasterService = roleMasterService;
        }

        /// <summary>
        /// To get role by role ID 
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpGet("{roleId}")]
        [ProducesResponseType(typeof(RoleMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRoleById(long roleId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var roleMasterModel = _roleMasterService.GetRoleById(roleId, ref errorResponseModel);

                if (roleMasterModel != null)
                {
                    return Ok(roleMasterModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all rolemaster
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetRoleMaster")]
        [ProducesResponseType(typeof(RoleMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetRoleMaster()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var roleMasterModelList = _roleMasterService.GetRoleMaster(ref errorResponseModel);

                if (roleMasterModelList != null)
                {
                    return Ok(roleMasterModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new roleMaster 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(RoleMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveRoleMaster(RoleMasterModel roleMasterModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var rolemastermodel = _roleMasterService.SaveRoleMaster(roleMasterModel, ref errorResponseModel);

                if (rolemastermodel != null)
                {
                    return Ok(rolemastermodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete RoleMaster 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteRoleMaster")]
        [ProducesResponseType(typeof(RoleMasterModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteRoleMaster(RoleMasterModel roleMasterModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var rolemastermodel = _roleMasterService.DeleteRoleMaster(roleMasterModel, ref errorResponseModel);

                if (rolemastermodel != null)
                {
                    return Ok(rolemastermodel);
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
