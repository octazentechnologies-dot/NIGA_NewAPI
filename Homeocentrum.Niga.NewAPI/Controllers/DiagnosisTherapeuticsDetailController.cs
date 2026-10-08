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
    [DoctorOnly]
    [OldApiContract]
    public class DiagnosisTherapeuticsDetailController : BaseAPIController
    {
        IDiagnosisTherapeuticsDetailService _diagnosisTherapeuticsDetailService;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="diagnosisTherapeuticsDetailService"></param>
        public DiagnosisTherapeuticsDetailController(IDiagnosisTherapeuticsDetailService diagnosisTherapeuticsDetailService)
        {
            _diagnosisTherapeuticsDetailService = diagnosisTherapeuticsDetailService;
        }

        /// <summary>
        /// To add new DiagnosisSystem 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost("SaveDiagnosisTherapeuticsDetail")]
        [ProducesResponseType(typeof(DiagnosisSystemModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult SaveDiagnosisTherapeuticsDetail(DiagnosisTherapeuticsDetailModel diagnosisTherapeuticsDetailModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var diagnosisSystemEntity = _diagnosisTherapeuticsDetailService.SaveDiagnosisTherapeuticsDetail(diagnosisTherapeuticsDetailModel, ref errorResponseModel);

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
