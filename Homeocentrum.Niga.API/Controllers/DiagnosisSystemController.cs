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
    public class DiagnosisSystemController : BaseAPIController
    {
        IDiagnosisSystemService _diagnosisSystemService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="diagnosisSystemService"></param>
        public DiagnosisSystemController(IDiagnosisSystemService diagnosisSystemService)
        {
            _diagnosisSystemService = diagnosisSystemService;
        }

        /// <summary>
        /// To add new DiagnosisSystem 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("SaveDiagnosisSystem")]
        [ProducesResponseType(typeof(DiagnosisSystemModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveDiagnosisSystem(DiagnosisSystemModel diagnosissystemModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisSystemEntity = _diagnosisSystemService.SaveDiagnosisSystem(diagnosissystemModel, ref errorResponseModel);

                if (diagnosisSystemEntity != null)
                {
                    return Ok(diagnosisSystemEntity);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To delete DeleteDiagnosisSystem 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("DeleteDiagnosisSystem")]
        [ProducesResponseType(typeof(DiagnosisSystemModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeleteDiagnosisSystem(DiagnosisSystemModel diagnosissystemModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisSystemEntity = _diagnosisSystemService.DeleteDiagnosisSystem(diagnosissystemModel, ref errorResponseModel);

                if (diagnosisSystemEntity != null)
                {
                    return Ok(diagnosisSystemEntity);
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
