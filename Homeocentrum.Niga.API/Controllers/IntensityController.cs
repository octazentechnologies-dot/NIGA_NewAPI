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
    [Route("api/intensity")]
    [ApiController]
    [Authorize]
    [DoctorOnly]
    [OldApiContract]
    public class IntensityController : BaseAPIController
    {
        IIntensityService _intensityService;

        /// <summary>
        /// Used to initialize controller and inject country service
        /// </summary>
        /// <param name="intensityService"></param>
        public IntensityController(IIntensityService intensityService)
        {
            _intensityService = intensityService;
        }

        /// <summary>
        /// To Get all Intensities
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(IntensityModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetIntensities()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var intensityModelList = _intensityService.GetIntensities(ref errorResponseModel);

                if (intensityModelList != null)
                {
                    return Ok(intensityModelList);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To add new Intensity 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(IntensityModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveIntensity(IntensityModel intensityModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var intensitymodel = _intensityService.SaveIntensity(intensityModel, ref errorResponseModel);

                if (intensitymodel != null)
                {
                    return Ok(intensitymodel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete Intensity 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteIntensity")]
        [ProducesResponseType(typeof(IntensityModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteIntensity(IntensityModel intensityModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var intensitymodel = _intensityService.DeleteIntensity(intensityModel, ref errorResponseModel);

                if (intensitymodel != null)
                {
                    return Ok(intensitymodel);
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
