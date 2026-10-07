using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

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
        private readonly NIGACentrumContext _context;

        public RegistrationController(IMastersAPIService mastersAPIService, NIGACentrumContext context)
        {
            _mastersAPIService = mastersAPIService;
            _context = context;
        }

        [HttpGet("countries")]
        public IActionResult GetCountries()
        {
            ErrorResponseModel errorResponseModel = null;
            try
            {
                var countries = _mastersAPIService.GetCountries(ref errorResponseModel)
                    ?.Where(x => !x.DeleteStatus)
                    .OrderBy(x => x.CountryName == "Other")
                    .ThenBy(x => x.CountryName)
                    .ToList();

                return Ok(countries ?? new System.Collections.Generic.List<CountryModel>());
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
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
                    .OrderBy(x => x.StateName == "Other")
                    .ThenBy(x => x.StateName)
                    .ToList();

                return Ok(states ?? new System.Collections.Generic.List<StateModel>());
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("districts")]
        public async Task<IActionResult> GetDistricts([FromQuery] int? stateId)
        {
            try
            {
                var query = _context.DistrictMasters.AsNoTracking().Where(d => !d.DeleteStatus);
                if (stateId.HasValue && stateId.Value > 0)
                    query = query.Where(d => d.StateId == stateId.Value);

                var districts = await query
                    .OrderBy(d => d.DistrictName == "Other")
                    .ThenBy(d => d.DistrictName)
                    .Select(d => new { d.DistrictId, d.DistrictName, d.StateId })
                    .ToListAsync();

                return Ok(districts);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
            }
        }

        [HttpGet("cities")]
        public async Task<IActionResult> GetCities([FromQuery] int? districtId)
        {
            try
            {
                var query = _context.CityMasters.AsNoTracking().Where(c => !c.DeleteStatus);
                if (districtId.HasValue && districtId.Value > 0)
                    query = query.Where(c => c.DistrictId == districtId.Value);

                var cities = await query
                    .OrderBy(c => c.CityName == "Other")
                    .ThenBy(c => c.CityName)
                    .Select(c => new { c.CityId, c.CityName, c.DistrictId })
                    .ToListAsync();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return this.ServerError(ex);
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
                return this.ServerError(ex);
            }
        }
    }
}
