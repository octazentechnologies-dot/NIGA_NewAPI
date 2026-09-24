using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// Anonymous lookup APIs used by the public doctor registration form.
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

        [HttpGet("countries")]
        public IActionResult GetCountries()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var countries = _mastersAPIService.GetCountries(ref errorResponseModel)
                    ?.Where(x => !x.DeleteStatus)
                    .OrderBy(x => x.CountryName)
                    .ToList();

                return Ok(countries ?? new System.Collections.Generic.List<CountryModel>());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpGet("states")]
        public IActionResult GetStates([FromQuery] int? countryId)
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var states = _mastersAPIService.GetStates(countryId, ref errorResponseModel)
                    ?.Where(x => !x.DeleteStatus)
                    .OrderBy(x => x.StateName)
                    .ToList();

                return Ok(states ?? new System.Collections.Generic.List<StateModel>());
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
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
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }
    }
}
