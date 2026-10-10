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
    public class DrugGroupController : BaseAPIController
    {
        IDrugGroupService _drugSystemService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="authorService"></param>
        public DrugGroupController(IDrugGroupService drugSystemService)
        {
            _drugSystemService = drugSystemService;
        }

        /// <summary>
        /// To get DrugGroup by drugSystemID 
        /// </summary>
        /// <param name="drugSystemId"></param>
        /// <returns></returns>
        [HttpGet("{drugSystemId}")]
        [ProducesResponseType(typeof(DrugGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDrugGroupById(long drugSystemId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var drugGroupModel = _drugSystemService.GetDrugGroupById(drugSystemId, ref errorResponseModel);

                if (drugGroupModel != null)
                {
                    return Ok(drugGroupModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get all DrugGroup
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpGet("GetDrugGroup")]
        [ProducesResponseType(typeof(DrugGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetDrugGroup()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var drugGroupModelList = _drugSystemService.GetDrugGroup(ref errorResponseModel);

                if (drugGroupModelList != null)
                {
                    return Ok(drugGroupModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new DrugGroup 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(DrugGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveDrugGroup(DrugGroupModel drugGroupModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var drugGroupEntity = _drugSystemService.SaveDrugGroup(drugGroupModel, ref errorResponseModel);

                if (drugGroupEntity != null)
                {
                    return Ok(drugGroupEntity);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete DrugGroup 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteDrugGroup")]
        [ProducesResponseType(typeof(DrugGroupModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteDrugGroup(DrugGroupModel drugGroupModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var drugGroupEntity = _drugSystemService.DeleteDrugGroup(drugGroupModel, ref errorResponseModel);

                if (drugGroupEntity != null)
                {
                    return Ok(drugGroupEntity);
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
