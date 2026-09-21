using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>PAT-04.02 / PAT-01.02 — Patient app profile + preferred language.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [ForbidMoneyRoles]
    public class PatientProfileController : ControllerBase
    {
        private readonly NIGACentrumContext _context;

        public PatientProfileController(NIGACentrumContext context)
        {
            _context = context;
        }

        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var userId = (long)User.GetUserId();
            var user = await _context.UserMasters.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.DeleteStatus);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            var map = await _context.PatientUserMaps.AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsPrimary && !m.DeleteStatus);
            Patient? patient = null;
            if (map != null)
            {
                patient = await _context.Patients.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PatientId == map.PatientId && p.DeleteStatus != true);
            }

            UserAppPreference? pref = null;
            try
            {
                pref = await _context.UserAppPreferences.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId);
            }
            catch
            {
                // Table may not exist until 05 SQL is run.
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.EmailId ?? patient?.Email,
                    mobileNo = user.MobileNo ?? patient?.MobileNo,
                    patientId = patient?.PatientId,
                    patientName = patient?.PatientName,
                    dateOfBirth = patient?.DateOfBirth,
                    gender = patient?.Gender,
                    age = patient?.Age,
                    preferredLanguageId = pref?.PreferredLanguageId,
                    welcomeVersionSeen = pref?.WelcomeVersionSeen
                }
            });
        }

        [HttpPut("Me")]
        public async Task<IActionResult> Save([FromBody] PatientProfileUpdateRequest request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Body is required." });

            var userId = (long)User.GetUserId();
            var user = await _context.UserMasters
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.DeleteStatus);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            if (!string.IsNullOrWhiteSpace(request.MobileNo))
                user.MobileNo = PhoneNormalizer.Digits(request.MobileNo);
            if (!string.IsNullOrWhiteSpace(request.Email))
                user.EmailId = request.Email.Trim();
            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(request.LastName))
                user.LastName = request.LastName.Trim();
            if (!string.IsNullOrWhiteSpace(request.PatientName))
            {
                var parts = request.PatientName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (string.IsNullOrWhiteSpace(request.FirstName))
                    user.FirstName = parts[0];
                if (parts.Length > 1 && string.IsNullOrWhiteSpace(request.LastName))
                    user.LastName = parts[1];
            }
            user.ChangedDate = DateTime.Now;

            var map = await _context.PatientUserMaps
                .FirstOrDefaultAsync(m => m.UserId == userId && m.IsPrimary && !m.DeleteStatus);

            if (map == null && request.PatientId.GetValueOrDefault() > 0)
            {
                map = new PatientUserMap
                {
                    UserId = userId,
                    PatientId = request.PatientId!.Value,
                    IsPrimary = true,
                    DeleteStatus = false,
                    EnteredBy = userId.ToString(),
                    EnteredDate = DateTime.UtcNow
                };
                _context.PatientUserMaps.Add(map);
            }

            Patient? patient = null;
            if (map != null)
            {
                patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PatientId == map.PatientId && p.DeleteStatus != true);
            }

            if (patient == null && (!string.IsNullOrWhiteSpace(request.PatientName) || request.DateOfBirth.HasValue))
            {
                patient = new Patient
                {
                    PatientName = string.IsNullOrWhiteSpace(request.PatientName)
                        ? $"{user.FirstName} {user.LastName}".Trim()
                        : request.PatientName.Trim(),
                    MobileNo = user.MobileNo,
                    Email = user.EmailId,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Age = request.Age,
                    DeleteStatus = false
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                if (map == null)
                {
                    map = new PatientUserMap
                    {
                        UserId = userId,
                        PatientId = patient.PatientId,
                        IsPrimary = true,
                        DeleteStatus = false,
                        EnteredBy = userId.ToString(),
                        EnteredDate = DateTime.UtcNow
                    };
                    _context.PatientUserMaps.Add(map);
                }
            }
            else if (patient != null)
            {
                if (!string.IsNullOrWhiteSpace(request.PatientName))
                    patient.PatientName = request.PatientName.Trim();
                if (request.DateOfBirth.HasValue)
                    patient.DateOfBirth = request.DateOfBirth;
                if (request.Gender.HasValue)
                    patient.Gender = request.Gender;
                if (request.Age.HasValue)
                    patient.Age = request.Age;
                if (!string.IsNullOrWhiteSpace(request.MobileNo))
                    patient.MobileNo = PhoneNormalizer.Digits(request.MobileNo);
                if (!string.IsNullOrWhiteSpace(request.Email))
                    patient.Email = request.Email.Trim();
            }

            try
            {
                var pref = await _context.UserAppPreferences.FirstOrDefaultAsync(p => p.UserId == userId);
                if (pref == null)
                {
                    pref = new UserAppPreference { UserId = userId, UpdatedAt = DateTime.UtcNow };
                    _context.UserAppPreferences.Add(pref);
                }
                if (request.PreferredLanguageId.HasValue)
                    pref.PreferredLanguageId = request.PreferredLanguageId;
                if (request.WelcomeVersionSeen != null)
                    pref.WelcomeVersionSeen = string.IsNullOrWhiteSpace(request.WelcomeVersionSeen)
                        ? pref.WelcomeVersionSeen
                        : request.WelcomeVersionSeen.Trim();
                pref.UpdatedAt = DateTime.UtcNow;
            }
            catch
            {
                // Preference table optional until SQL 05.
            }

            await _context.SaveChangesAsync();
            return await Me();
        }
    }
}
