using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// Address location lists: Country, then State, then City, then PinCode.
    /// Every call requires Authorization: Bearer token from login.
    /// </summary>
    [Route("api/Location")]
    [ApiController]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public LocationController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Countries")]
        public async Task<IActionResult> GetCountries()
        {
            var rows = await _context.CountryMasters.AsNoTracking()
                .Where(c => !c.DeleteStatus)
                .OrderBy(c => c.CountryName)
                .Select(c => new
                {
                    c.CountryId,
                    c.CountryName,
                    c.CountryCode
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Countries/{countryId:int}")]
        public async Task<IActionResult> GetCountryById(int countryId)
        {
            var row = await _context.CountryMasters.AsNoTracking()
                .Where(c => c.CountryId == countryId && !c.DeleteStatus)
                .Select(c => new
                {
                    c.CountryId,
                    c.CountryName,
                    c.CountryCode
                })
                .FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "Country not found." });
            return Ok(new { success = true, data = row });
        }

        [HttpGet("States")]
        public async Task<IActionResult> GetStates()
        {
            var rows = await _context.StateMasters.AsNoTracking()
                .Where(s => !s.DeleteStatus)
                .OrderBy(s => s.StateName)
                .Select(s => new
                {
                    s.StateId,
                    s.StateName,
                    s.CountryId
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("States/ByCountry/{countryId:int}")]
        public async Task<IActionResult> GetStatesByCountry(int countryId)
        {
            if (!await _context.CountryMasters.AnyAsync(c => c.CountryId == countryId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "Country not found." });

            var rows = await _context.StateMasters.AsNoTracking()
                .Where(s => s.CountryId == countryId && !s.DeleteStatus)
                .OrderBy(s => s.StateName)
                .Select(s => new
                {
                    s.StateId,
                    s.StateName,
                    s.CountryId
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Cities")]
        public async Task<IActionResult> GetCities()
        {
            var rows = await _context.CityMasters.AsNoTracking()
                .Where(c => !c.DeleteStatus)
                .OrderBy(c => c.CityName)
                .Select(c => new
                {
                    c.CityId,
                    c.CityName,
                    c.StateId
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Cities/ByState/{stateId:int}")]
        public async Task<IActionResult> GetCitiesByState(int stateId)
        {
            if (!await _context.StateMasters.AnyAsync(s => s.StateId == stateId && !s.DeleteStatus))
                return NotFound(new { success = false, message = "State not found." });

            var rows = await _context.CityMasters.AsNoTracking()
                .Where(c => c.StateId == stateId && !c.DeleteStatus)
                .OrderBy(c => c.CityName)
                .Select(c => new
                {
                    c.CityId,
                    c.CityName,
                    c.StateId
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes")]
        public async Task<IActionResult> GetPinCodes()
        {
            var rows = await _context.PinCodeMasters.AsNoTracking()
                .Where(p => !p.DeleteStatus)
                .OrderBy(p => p.PinCode)
                .Select(p => new
                {
                    p.PinCodeId,
                    p.PinCode,
                    p.CityId
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes/ByCity/{cityId:int}")]
        public async Task<IActionResult> GetPinCodesByCity(int cityId)
        {
            if (!await _context.CityMasters.AnyAsync(c => c.CityId == cityId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "City not found." });

            var rows = await _context.PinCodeMasters.AsNoTracking()
                .Where(p => p.CityId == cityId && !p.DeleteStatus)
                .OrderBy(p => p.PinCode)
                .Select(p => new
                {
                    p.PinCodeId,
                    p.PinCode,
                    p.CityId
                })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }
    }
}
