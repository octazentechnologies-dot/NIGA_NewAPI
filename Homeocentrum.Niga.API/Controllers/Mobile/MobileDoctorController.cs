using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;
using Homeocentrum.Niga.API.Hosting;

namespace Homeocentrum.Niga.API.Controllers.Mobile;

/// <summary>
/// Endpoints only the Doctor Mobile App calls. Shared doctor endpoints (password login, profile, queue,
/// availability, refills, earnings) stay in their own controllers; see the "mobile-doctor" Swagger document.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[ApiAudience(ApiAudience.MobileDoctor)]
public class MobileDoctorController : ControllerBase
{
    private readonly NIGACentrumContext _context;
    private readonly ITokenService _tokenService;
    private readonly IMastersAPIService _mastersAPIService;

    public MobileDoctorController(NIGACentrumContext context, ITokenService tokenService, IMastersAPIService mastersAPIService)
    {
        _context = context;
        _tokenService = tokenService;
        _mastersAPIService = mastersAPIService;
    }

    /// <summary>DMO-01.02 — doctor phone + OTP login. Mobile is matched on Doctor only. Call POST /api/Otp/RequestOtp (action Login) first.</summary>
    [SecurityAudit(SecurityAuditEvents.Login)]
    [HttpPost("LoginWithOtp")]
    [LegacyRoute("/api/Account/LoginWithOtp")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginWithOtp([FromBody] LoginWithOtpRequest request)
    {
        try
        {
            if (request == null
                || request.OtpChallengeId <= 0
                || string.IsNullOrWhiteSpace(request.Code)
                || string.IsNullOrWhiteSpace(request.MobileNo))
            {
                return BadRequest(new { success = false, message = "OtpChallengeId, Code, and MobileNo are required." });
            }

            var mobile = PhoneNormalizer.Digits(request.MobileNo);
            if (mobile.Length < 8)
                return BadRequest(new { success = false, message = "MobileNo is invalid." });

            // TODO: production OTP. Until then LoginWithOtp consumes Dev/challenge OTP only (test mobile 7768046064).

            var challenge = await _context.OtpChallenges
                .FirstOrDefaultAsync(c => c.OtpChallengeId == request.OtpChallengeId);
            if (challenge == null)
                return NotFound(new { success = false, message = "OTP challenge not found." });

            if (!string.Equals(challenge.Action, "Login", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "OTP is not a login challenge." });

            if (challenge.LockedUntil.HasValue && challenge.LockedUntil > DateTime.UtcNow)
                return StatusCode(StatusCodes.Status423Locked, new { success = false, message = "OTP locked due to too many attempts." });

            if (challenge.VerifiedAt != null || challenge.ExpiresAt < DateTime.UtcNow)
                return BadRequest(new { success = false, message = "OTP expired or already used." });

            var destDigits = PhoneNormalizer.Digits(challenge.EntityId);
            if (string.IsNullOrEmpty(destDigits))
                destDigits = PhoneNormalizer.Digits(challenge.DestinationMasked);
            if (!PhoneNormalizer.EqualsNormalized(mobile, challenge.EntityId)
                && !string.Equals(challenge.EntityId, mobile, StringComparison.Ordinal))
            {
                // EntityId should be the mobile digits from RequestOtp.
                if (!string.Equals(challenge.EntityId, request.MobileNo.Trim(), StringComparison.OrdinalIgnoreCase)
                    && destDigits != mobile)
                {
                    return BadRequest(new { success = false, message = "OTP does not match this mobile number." });
                }
            }

            challenge.AttemptCount++;
            var ok = string.Equals(
                SecurityTokenHash.Sha256Hex(request.Code.Trim()),
                challenge.OtpHash,
                StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                if (challenge.AttemptCount >= 5)
                    challenge.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                await _context.SaveChangesAsync();
                return BadRequest(new { success = false, message = "Invalid OTP." });
            }

            challenge.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var userEntity = await FindDoctorUserByMobileAsync(mobile);
            if (userEntity == null)
            {
                return Ok(new
                {
                    success = true,
                    message = "No account for this mobile number.",
                    isUserAlreadyRegistered = false
                });
            }

            if (userEntity.IsUserActivated != true && userEntity.UserStatus != true)
                return Unauthorized(new { success = false, message = "Account is deactivated. Please contact administrator.", isUserAlreadyRegistered = true });

            var roleEntity = await _context.RoleMasters
                .FirstOrDefaultAsync(x => x.RoleId == userEntity.RoleId);
            if (roleEntity == null)
                return BadRequest(new { success = false, message = "User role not found." });

            int? doctorId = null;
            var doctorEntity = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userEntity.UserId && d.DeleteStatus == false);
            if (doctorEntity != null)
                doctorId = doctorEntity.DoctorId;

            var token = await _tokenService.CreateToken(userEntity, 7 * 24 * 60, roleEntity.RoleName, doctorId);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                isUserAlreadyRegistered = true,
                token,
                user = new
                {
                    userId = userEntity.UserId,
                    userName = userEntity.UserName,
                    firstName = userEntity.FirstName,
                    lastName = userEntity.LastName,
                    email = userEntity.EmailId,
                    mobileNo = mobile,
                    role = roleEntity.RoleName,
                    roleId = userEntity.RoleId,
                    doctorId,
                    activated = userEntity.IsUserActivated == true
                }
            });
        }
        catch (Exception ex)
        {
            return this.ServerError(ex);
        }
    }

    /// <summary>Registration form: countries, "Other" last.</summary>
    [HttpGet("Registration/Countries")]
    [LegacyRoute("/api/registration/countries")]
    [AllowAnonymous]
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

            return Ok(countries ?? new List<CountryModel>());
        }
        catch (Exception ex)
        {
            return this.ServerError(ex);
        }
    }

    /// <summary>Registration form: states, optionally for one country, "Other" last.</summary>
    [HttpGet("Registration/States")]
    [LegacyRoute("/api/registration/states")]
    [AllowAnonymous]
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

            return Ok(states ?? new List<StateModel>());
        }
        catch (Exception ex)
        {
            return this.ServerError(ex);
        }
    }

    /// <summary>Registration form: districts, optionally for one state, "Other" last.</summary>
    [HttpGet("Registration/Districts")]
    [LegacyRoute("/api/registration/districts")]
    [AllowAnonymous]
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

    /// <summary>Registration form: cities, optionally for one district, "Other" last.</summary>
    [HttpGet("Registration/Cities")]
    [LegacyRoute("/api/registration/cities")]
    [AllowAnonymous]
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

    /// <summary>Doctor OTP login matches Doctor.MobileNo only.</summary>
    private async Task<UserMaster?> FindDoctorUserByMobileAsync(string digits)
    {
        var doctors = await _context.Doctors.AsNoTracking()
            .Where(d => !d.DeleteStatus && d.MobileNo != null && d.UserId != null)
            .Select(d => new { d.UserId, d.MobileNo })
            .ToListAsync();
        var doctor = doctors.FirstOrDefault(d => PhoneNormalizer.EqualsNormalized(d.MobileNo, digits));
        if (doctor?.UserId == null)
            return null;

        return await _context.UserMasters
            .FirstOrDefaultAsync(u => u.UserId == doctor.UserId.Value && !u.DeleteStatus);
    }
}
