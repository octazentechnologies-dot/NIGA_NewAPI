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
    [Route("api/bodypart")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    [OldApiContract]
    public class BodyPartController : BaseAPIController
    {
        IBodyPartService _bodypartService;

        /// <summary>
        /// Used to initialize controller and inject bodypart service
        /// </summary>
        /// <param name="bodypartService"></param>
        public BodyPartController(IBodyPartService bodypartService)
        {
            _bodypartService = bodypartService;
        }

        /// <summary>
        /// To add new BodyPart 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(BodyPartModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveBodyPart(BodyPartModel bodypartModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var bodyPartModel = _bodypartService.SaveBodyPart(bodypartModel, ref errorResponseModel);

                if (bodyPartModel != null)
                {
                    return Ok(bodyPartModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete bodypart 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteBodyPart")]
        [ProducesResponseType(typeof(BodyPartModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteBodyPart(BodyPartModel bodypartModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var bodyPartModel = _bodypartService.DeleteBodyPart(bodypartModel, ref errorResponseModel);

                if (bodyPartModel != null)
                {
                    return Ok(bodyPartModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get body part by BodyPart ID 
        /// </summary>
        /// <param name="SectionId"></param>
        /// <returns></returns>
        [HttpGet("GetBodyPartsBySection/{SectionId}")]
        [ProducesResponseType(typeof(BodyPartModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetBodyPartsBySection(long SectionId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var bodypartModel = _bodypartService.GetBodyPartBySection(SectionId, ref errorResponseModel);

                if (bodypartModel != null)
                {
                    return Ok(bodypartModel);
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
