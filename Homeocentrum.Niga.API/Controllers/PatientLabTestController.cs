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
    [OldApiContract]
    public class PatientLabTestController : BaseAPIController
    {
        IPatientLabTestService _patientLabTest;

        /// <summary>
        /// Used to initialize controller and inject author service
        /// </summary>
        /// <param name="authorService"></param>
        public PatientLabTestController(IPatientLabTestService patientLabTestService)
        {
            _patientLabTest = patientLabTestService;
        }

        /// <summary>
        /// To add new Author 
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [HttpPost]
        [Route("AddEditPatientLabTest")]
        [ProducesResponseType(typeof(PatientLabTestModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult AddEditPatientLabTest(PatientLabTestModel patientLabTestModel)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                //int userId = 0;
                //if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                //{
                //    if (User != null && User.Identity != null && User.Identity.IsAuthenticated)
                //    {
                //        userId = Convert.ToInt32(((System.Security.Claims.ClaimsIdentity)User.Identity).FindFirst(System.Security.Claims.ClaimTypes.Name).Value);
                //    }
                //}
                int userId = 0;
                var identity = User?.Identity as System.Security.Claims.ClaimsIdentity;
                if (identity != null && identity.IsAuthenticated)
                {
                    var userIdClaim = identity.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                                      ?? identity.FindFirst("UserId")?.Value; // fallback

                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
                    {
                        userId = parsedUserId;
                    }
                }


                var Model = _patientLabTest.AddEditPatientLabTest(patientLabTestModel, userId, ref errorResponseModel);

                if (Model != null)
                {
                    return Ok(Model);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// To get Author by authorID 
        /// </summary>
        /// <param name="pathologyId"></param>
        /// <returns></returns>
        [HttpGet("GetPatientLabTestById/{patientLabTestId}")]
        [ProducesResponseType(typeof(PatientLabTestModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetPatientLabTestById(int patientLabTestId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var rModel = _patientLabTest.GetPatientLabTestById(patientLabTestId, ref errorResponseModel);

                if (rModel != null)
                {
                    return Ok(rModel);
                }
                return ReturnErrorResponse(errorResponseModel);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        /// <summary>
        /// Soft-deletes a lab / imaging test (Admin > Labs &amp; Imaging).
        /// </summary>
        [HttpPost("DeletePatientLabTest/{patientLabTestId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 500)]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public IActionResult DeletePatientLabTest(int patientLabTestId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var message = _patientLabTest.DeletePatientLabTest(patientLabTestId, ref errorResponseModel);
                if (!string.IsNullOrEmpty(message))
                {
                    return Ok(message);
                }
                return NotFound("Lab test not found");
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}
