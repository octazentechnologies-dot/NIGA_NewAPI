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
    [Authorize]
    [OldApiContract]
    public class OtherSideEffectController : BaseAPIController
    {
        IOtherSideEffectService _otherSideEffectService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="adverseReactionService"></param>
        public OtherSideEffectController(IOtherSideEffectService otherSideEffectService)
        {
            _otherSideEffectService = otherSideEffectService;
        }

        /// <summary>
        /// To delete OtherSideEffect 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteOtherSideEffect")]
        [ProducesResponseType(typeof(AdverseReactionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteOtherSideEffect(OtherSideEffectModel otherSideEffectModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var otherSideEffectEntry = _otherSideEffectService.DeleteOtherSideEffect(otherSideEffectModel, ref errorResponseModel);

                if (otherSideEffectEntry != null)
                {
                    return Ok(otherSideEffectEntry);
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
