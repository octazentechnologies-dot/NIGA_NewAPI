using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.DTOs;
using Niga_Domain.Interface;
using System;

using Niga_Domain.Authorization;
namespace Niga_Domain.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SeriousSideEffectController : BaseAPIController
    {
        ISeriousSideEffectService _seriousSideEffectService;
        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="adverseReactionService"></param>
        public SeriousSideEffectController(ISeriousSideEffectService seriousSideEffectService)
        {
            _seriousSideEffectService = seriousSideEffectService;
        }
        /// <summary>
        /// To delete Qualification 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteSeriousSideEffect")]
        [ProducesResponseType(typeof(AdverseReactionModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteSeriousSideEffect(SeriousSideEffectModel seriousSideEffectModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var seriousSideEffectEntry = _seriousSideEffectService.DeleteSeriousSideEffect(seriousSideEffectModel, ref errorResponseModel);

                if (seriousSideEffectEntry != null)
                {
                    return Ok(seriousSideEffectEntry);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
