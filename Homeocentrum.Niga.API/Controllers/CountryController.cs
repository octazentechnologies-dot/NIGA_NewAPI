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
    [Route("api/country")]
    [ApiController]

    [OldApiContract]
    public class CountryController : BaseAPIController
    {
        ICountryService _countryService;

        /// <summary>
        /// Used to initialize controller and inject country service
        /// </summary>
        /// <param name="countryService"></param>
        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        /// <summary>
        /// To Get all counrriecountries
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(CountryModel), 200)]
        [ProducesResponseType(typeof(string), 404)]
        [ProducesResponseType(typeof(string), 400)]
        [ProducesResponseType(typeof(string), 500)]
        public IActionResult GetCountries()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var countryModelList = _countryService.GetCountries(ref errorResponseModel);

                if (countryModelList.Count != 0)
                {
                    return Ok(countryModelList);
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
