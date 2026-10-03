using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Data;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// Address lists: Country, State, District, City, PinCode.
    /// Every call requires Authorization: Bearer token from login.
    /// </summary>
    [Route("api/UserAddressLocation")]
    [ApiController]
    [Authorize]
    public class UserAddressLocationController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public UserAddressLocationController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Countries")]
        public async Task<IActionResult> GetCountries()
        {
            var rows = await _context.CountryMasters.AsNoTracking()
                .Where(c => !c.DeleteStatus)
                .OrderBy(c => c.CountryName == "Other")
                .ThenBy(c => c.CountryName)
                .Select(c => new
                {
                    c.CountryId,
                    c.CountryName,
                    c.CountryCode,
                    c.Iso2Code,
                    c.Iso3Code
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
                    c.CountryCode,
                    c.Iso2Code,
                    c.Iso3Code
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
                .OrderBy(s => s.StateName == "Other")
                .ThenBy(s => s.StateName)
                .Select(s => new { s.StateId, s.StateName, s.CountryId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("States/{stateId:int}")]
        public async Task<IActionResult> GetStateById(int stateId)
        {
            var row = await _context.StateMasters.AsNoTracking()
                .Where(s => s.StateId == stateId && !s.DeleteStatus)
                .Select(s => new { s.StateId, s.StateName, s.CountryId })
                .FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "State not found." });
            return Ok(new { success = true, data = row });
        }

        [HttpGet("States/ByCountry/{countryId:int}")]
        public async Task<IActionResult> GetStatesByCountry(int countryId)
        {
            if (!await _context.CountryMasters.AnyAsync(c => c.CountryId == countryId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "Country not found." });

            var rows = await _context.StateMasters.AsNoTracking()
                .Where(s => s.CountryId == countryId && !s.DeleteStatus)
                .OrderBy(s => s.StateName == "Other")
                .ThenBy(s => s.StateName)
                .Select(s => new { s.StateId, s.StateName, s.CountryId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Districts")]
        public async Task<IActionResult> GetDistricts()
        {
            var rows = await _context.DistrictMasters.AsNoTracking()
                .Where(d => !d.DeleteStatus)
                .OrderBy(d => d.DistrictName == "Other")
                .ThenBy(d => d.DistrictName)
                .Select(d => new { d.DistrictId, d.DistrictName, d.StateId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Districts/{districtId:int}")]
        public async Task<IActionResult> GetDistrictById(int districtId)
        {
            var row = await _context.DistrictMasters.AsNoTracking()
                .Where(d => d.DistrictId == districtId && !d.DeleteStatus)
                .Select(d => new { d.DistrictId, d.DistrictName, d.StateId })
                .FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "District not found." });
            return Ok(new { success = true, data = row });
        }

        [HttpGet("Districts/ByState/{stateId:int}")]
        public async Task<IActionResult> GetDistrictsByState(int stateId)
        {
            if (!await _context.StateMasters.AnyAsync(s => s.StateId == stateId && !s.DeleteStatus))
                return NotFound(new { success = false, message = "State not found." });

            var rows = await _context.DistrictMasters.AsNoTracking()
                .Where(d => d.StateId == stateId && !d.DeleteStatus)
                .OrderBy(d => d.DistrictName == "Other")
                .ThenBy(d => d.DistrictName)
                .Select(d => new { d.DistrictId, d.DistrictName, d.StateId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Districts/ByCountry/{countryId:int}")]
        public async Task<IActionResult> GetDistrictsByCountry(int countryId)
        {
            if (!await _context.CountryMasters.AnyAsync(c => c.CountryId == countryId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "Country not found." });

            var rows = await (
                from district in _context.DistrictMasters.AsNoTracking()
                join state in _context.StateMasters.AsNoTracking() on district.StateId equals state.StateId
                where state.CountryId == countryId && !district.DeleteStatus && !state.DeleteStatus
                orderby district.DistrictName == "Other", district.DistrictName
                select new { district.DistrictId, district.DistrictName, district.StateId }
            ).ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Cities")]
        public async Task<IActionResult> GetCities()
        {
            var rows = await _context.CityMasters.AsNoTracking()
                .Where(c => !c.DeleteStatus)
                .OrderBy(c => c.CityName == "Other")
                .ThenBy(c => c.CityName)
                .Select(c => new { c.CityId, c.CityName, c.DistrictId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Cities/{cityId:int}")]
        public async Task<IActionResult> GetCityById(int cityId)
        {
            var row = await _context.CityMasters.AsNoTracking()
                .Where(c => c.CityId == cityId && !c.DeleteStatus)
                .Select(c => new { c.CityId, c.CityName, c.DistrictId })
                .FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "City not found." });
            return Ok(new { success = true, data = row });
        }

        [HttpGet("Cities/ByDistrict/{districtId:int}")]
        public async Task<IActionResult> GetCitiesByDistrict(int districtId)
        {
            if (!await _context.DistrictMasters.AnyAsync(d => d.DistrictId == districtId && !d.DeleteStatus))
                return NotFound(new { success = false, message = "District not found." });

            var rows = await _context.CityMasters.AsNoTracking()
                .Where(c => c.DistrictId == districtId && !c.DeleteStatus)
                .OrderBy(c => c.CityName == "Other")
                .ThenBy(c => c.CityName)
                .Select(c => new { c.CityId, c.CityName, c.DistrictId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Cities/ByState/{stateId:int}")]
        public async Task<IActionResult> GetCitiesByState(int stateId)
        {
            if (!await _context.StateMasters.AnyAsync(s => s.StateId == stateId && !s.DeleteStatus))
                return NotFound(new { success = false, message = "State not found." });

            var rows = await (
                from city in _context.CityMasters.AsNoTracking()
                join district in _context.DistrictMasters.AsNoTracking() on city.DistrictId equals district.DistrictId
                where district.StateId == stateId && !city.DeleteStatus && !district.DeleteStatus
                orderby city.CityName == "Other", city.CityName
                select new { city.CityId, city.CityName, city.DistrictId }
            ).ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("Cities/ByCountry/{countryId:int}")]
        public async Task<IActionResult> GetCitiesByCountry(int countryId)
        {
            if (!await _context.CountryMasters.AnyAsync(c => c.CountryId == countryId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "Country not found." });

            var rows = await (
                from city in _context.CityMasters.AsNoTracking()
                join district in _context.DistrictMasters.AsNoTracking() on city.DistrictId equals district.DistrictId
                join state in _context.StateMasters.AsNoTracking() on district.StateId equals state.StateId
                where state.CountryId == countryId && !city.DeleteStatus && !district.DeleteStatus && !state.DeleteStatus
                orderby city.CityName == "Other", city.CityName
                select new { city.CityId, city.CityName, city.DistrictId }
            ).ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes")]
        public async Task<IActionResult> GetPinCodes()
        {
            var rows = await _context.PinCodeMasters.AsNoTracking()
                .Where(p => !p.DeleteStatus)
                .OrderBy(p => p.PinCode == "Other")
                .ThenBy(p => p.PinCode)
                .Select(p => new { p.PinCodeId, p.PinCode, p.CityId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes/{pinCodeId:int}")]
        public async Task<IActionResult> GetPinCodeById(int pinCodeId)
        {
            var row = await _context.PinCodeMasters.AsNoTracking()
                .Where(p => p.PinCodeId == pinCodeId && !p.DeleteStatus)
                .Select(p => new { p.PinCodeId, p.PinCode, p.CityId })
                .FirstOrDefaultAsync();
            if (row == null)
                return NotFound(new { success = false, message = "Pin code not found." });
            return Ok(new { success = true, data = row });
        }

        [HttpGet("PinCodes/ByCity/{cityId:int}")]
        public async Task<IActionResult> GetPinCodesByCity(int cityId)
        {
            if (!await _context.CityMasters.AnyAsync(c => c.CityId == cityId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "City not found." });

            var rows = await _context.PinCodeMasters.AsNoTracking()
                .Where(p => p.CityId == cityId && !p.DeleteStatus)
                .OrderBy(p => p.PinCode == "Other")
                .ThenBy(p => p.PinCode)
                .Select(p => new { p.PinCodeId, p.PinCode, p.CityId })
                .ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes/ByDistrict/{districtId:int}")]
        public async Task<IActionResult> GetPinCodesByDistrict(int districtId)
        {
            if (!await _context.DistrictMasters.AnyAsync(d => d.DistrictId == districtId && !d.DeleteStatus))
                return NotFound(new { success = false, message = "District not found." });

            var rows = await (
                from pin in _context.PinCodeMasters.AsNoTracking()
                join city in _context.CityMasters.AsNoTracking() on pin.CityId equals city.CityId
                where city.DistrictId == districtId && !pin.DeleteStatus && !city.DeleteStatus
                orderby pin.PinCode == "Other", pin.PinCode
                select new { pin.PinCodeId, pin.PinCode, pin.CityId }
            ).ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes/ByState/{stateId:int}")]
        public async Task<IActionResult> GetPinCodesByState(int stateId)
        {
            if (!await _context.StateMasters.AnyAsync(s => s.StateId == stateId && !s.DeleteStatus))
                return NotFound(new { success = false, message = "State not found." });

            var rows = await (
                from pin in _context.PinCodeMasters.AsNoTracking()
                join city in _context.CityMasters.AsNoTracking() on pin.CityId equals city.CityId
                join district in _context.DistrictMasters.AsNoTracking() on city.DistrictId equals district.DistrictId
                where district.StateId == stateId && !pin.DeleteStatus && !city.DeleteStatus && !district.DeleteStatus
                orderby pin.PinCode == "Other", pin.PinCode
                select new { pin.PinCodeId, pin.PinCode, pin.CityId }
            ).ToListAsync();
            return Ok(new { success = true, data = rows });
        }

        [HttpGet("PinCodes/ByCountry/{countryId:int}")]
        public async Task<IActionResult> GetPinCodesByCountry(int countryId)
        {
            if (!await _context.CountryMasters.AnyAsync(c => c.CountryId == countryId && !c.DeleteStatus))
                return NotFound(new { success = false, message = "Country not found." });

            var rows = await (
                from pin in _context.PinCodeMasters.AsNoTracking()
                join city in _context.CityMasters.AsNoTracking() on pin.CityId equals city.CityId
                join district in _context.DistrictMasters.AsNoTracking() on city.DistrictId equals district.DistrictId
                join state in _context.StateMasters.AsNoTracking() on district.StateId equals state.StateId
                where state.CountryId == countryId && !pin.DeleteStatus && !city.DeleteStatus && !district.DeleteStatus && !state.DeleteStatus
                orderby pin.PinCode == "Other", pin.PinCode
                select new { pin.PinCodeId, pin.PinCode, pin.CityId }
            ).ToListAsync();
            return Ok(new { success = true, data = rows });
        }
    }
}
