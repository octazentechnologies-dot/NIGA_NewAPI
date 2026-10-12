using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>
    /// Anonymous lookups for doctor registration, used by the web form and the Doctor Mobile App.
    /// Country, state, district and city lists for the doctor app live in MobileDoctorController
    /// (the old /api/registration/countries|states|districts|cities URLs still answer).
    /// </summary>
    [Route("api/registration")]
    [ApiController]
    [AllowAnonymous]
    public class RegistrationController : BaseAPIController
    {
        private readonly IMastersAPIService _mastersAPIService;

        public RegistrationController(IMastersAPIService mastersAPIService)
        {
            _mastersAPIService = mastersAPIService;
        }

        [HttpGet("qualifications")]
        public IActionResult GetQualifications()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var qualifications = _mastersAPIService.GetQualifications(ref errorResponseModel)
                    ?.Where(x => !x.DeleteStatus)
                    .OrderBy(x => x.QualificationName)
                    .ToList();

                return Ok(qualifications ?? new System.Collections.Generic.List<QualificationModel>());
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }
    }
}
