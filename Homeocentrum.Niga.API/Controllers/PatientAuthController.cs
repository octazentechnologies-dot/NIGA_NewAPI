using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>WEB-04.02 — anonymous PatientAuth OTP for public booking (reuses OtpChallenge).</summary>
    [Route("api/PatientAuth")]
    [ApiController]
    [AllowAnonymous]
    public class PatientAuthController : ControllerBase
    {
        private const int OtpTtlMinutes = 10;
        private readonly NIGACentrumContext _context;
        private readonly ITokenService _tokenService;
        private readonly ISmsSender _smsSender;
        private readonly ISmsTemplateRenderer _templates;
        private readonly ISmsOtpVerifyService _smsOtp;
        private readonly SmsOptions _smsOptions;
        private readonly IPatientAccountService _patientAccounts;

        public PatientAuthController(
            NIGACentrumContext context,
            ITokenService tokenService,
            ISmsSender smsSender,
            ISmsTemplateRenderer templates,
            ISmsOtpVerifyService smsOtp,
            IOptions<SmsOptions> smsOptions,
            IPatientAccountService patientAccounts)
        {
            _context = context;
            _tokenService = tokenService;
            _smsSender = smsSender;
            _templates = templates;
            _smsOtp = smsOtp;
            _smsOptions = smsOptions.Value ?? new SmsOptions();
            _patientAccounts = patientAccounts;
        }

        [SecurityAudit(SecurityAuditEvents.OtpRequested)]
        [HttpPost("RequestOtp")]
        public async Task<IActionResult> RequestOtp([FromBody] PatientAuthRequestOtpModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile))
                return BadRequest(new { success = false, message = "Mobile is required." });

            var digits = PhoneNormalizer.Digits(request.Mobile);
            if (digits.Length < 8)
                return BadRequest(new { success = false, message = "Destination must be a valid mobile number." });

            var since = DateTime.UtcNow.AddMinutes(-1);
            var recent = await _context.OtpChallenges.CountAsync(c =>
                c.Action == "PatientAuth" && c.EntityId == digits && c.CreatedAt >= since);
            if (recent >= 3)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = "Too many OTP requests." });

            // Registered = Patient-role UserMaster exists, OR a clinic chart exists that OTP can claim.
            var isUserRegistered = await _patientAccounts.IsRegisteredOrClaimableAsync(digits);

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var otpText = await _templates.RenderAsync("OTP", new Dictionary<string, string?>
            {
                ["otp"] = code,
                ["minutes"] = OtpTtlMinutes.ToString()
            });
            // Real SMS only when Sms:RealSms is true. Dummy mode keeps the generated code as devCode.
            var live = SmsOtpPolicy.RealSmsLive(_smsOptions);
            var send = live
                ? await _smsSender.SendAsync(digits, otpText)
                : SmsSendResult.Fail();
            if (!string.IsNullOrWhiteSpace(send.EmbeddedOtp))
                code = send.EmbeddedOtp!;
            var delivered = send.Accepted;

            var challenge = new OtpChallenge
            {
                Action = "PatientAuth",
                EntityType = "Mobile",
                EntityId = digits,
                DestinationMasked = digits.Length > 4 ? "******" + digits[^4..] : "****",
                OtpHash = SecurityTokenHash.Sha256Hex(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };
            _context.OtpChallenges.Add(challenge);
            await _context.SaveChangesAsync();

            var stored = await _smsOtp.StoreAsync(
                "PatientAuth",
                digits,
                code,
                send.ProviderLabel,
                send.MessageSid,
                OtpTtlMinutes,
                challenge.OtpChallengeId);

            var payload = new Dictionary<string, object?>
            {
                ["success"] = true,
                ["otpChallengeId"] = challenge.OtpChallengeId,
                ["smsOtpVerifyId"] = stored.SmsOtpVerifyId,
                ["expiresAt"] = stored.ExpiresAt,
                ["destinationMasked"] = stored.DestinationMasked,
                ["isUserRegistered"] = isUserRegistered,
                ["delivered"] = delivered,
                ["channel"] = "sms",
                ["trialTemplate"] = send.UsedTrialTemplate,
                ["message"] = delivered
                    ? "OTP sent by SMS. Enter the code from your phone."
                    : live
                        ? "OTP created but SMS delivery did not confirm. Check Twilio settings or try again."
                        : "OTP created. SMS is not live; use the development code."
            };
            // Never return plaintext OTP when Twilio/Msg91 is live.
            var devCode = SmsOtpPolicy.DevCodeOrNull(_smsOptions, code);
            if (devCode != null)
                payload["devCode"] = devCode;

            return Ok(payload);
        }

        [SecurityAudit(SecurityAuditEvents.Login)]
        [HttpPost("VerifyOtp")]
        public async Task<IActionResult> VerifyOtp([FromBody] PatientAuthVerifyOtpModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Mobile) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { success = false, message = "Mobile and Code are required." });

            var digits = PhoneNormalizer.Digits(request.Mobile);
            var check = await _smsOtp.VerifyAsync("PatientAuth", digits, request.Code.Trim());
            if (!check.Success)
            {
                var status = string.Equals(check.Error, "OTP locked. Try later.", StringComparison.Ordinal)
                    ? StatusCodes.Status429TooManyRequests
                    : StatusCodes.Status400BadRequest;
                return StatusCode(status, new { success = false, message = check.Error ?? "Invalid OTP." });
            }

            var bookingSessionId = check.OtpChallengeId
                ?? await _context.OtpChallenges.AsNoTracking()
                    .Where(c => c.Action == "PatientAuth" && c.EntityId == digits)
                    .OrderByDescending(c => c.OtpChallengeId)
                    .Select(c => (long?)c.OtpChallengeId)
                    .FirstOrDefaultAsync()
                ?? check.SmsOtpVerifyId
                ?? 0;

            var payload = new Dictionary<string, object?>
            {
                ["success"] = true,
                ["bookingSessionId"] = bookingSessionId,
                ["smsOtpVerifyId"] = check.SmsOtpVerifyId,
                ["mobile"] = digits
            };
            var user = await ExistingPatientUserAsync(digits);
            payload["isUserAlreadyRegistered"] = user != null;
            payload["isUserRegistered"] = user != null;
            if (user != null)
            {
                // Top-level fields for mobile binding + nested user object.
                payload["token"] = user.Token;
                payload["userId"] = user.UserId;
                payload["userName"] = user.UserName;
                payload["firstName"] = user.FirstName;
                payload["lastName"] = user.LastName;
                payload["email"] = user.Email;
                payload["mobileNo"] = user.MobileNo;
                payload["role"] = user.Role;
                payload["roleId"] = user.RoleId;
                payload["patientId"] = user.PatientId;
                payload["patientName"] = user.PatientName;
                payload["user"] = new
                {
                    userId = user.UserId,
                    userName = user.UserName,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    mobileNo = user.MobileNo,
                    role = user.Role,
                    roleId = user.RoleId,
                    patientId = user.PatientId,
                    patientName = user.PatientName
                };
            }
            return Ok(payload);
        }

        /// <summary>
        /// Resolve Patient-role login for this mobile (create + map when a clinic chart exists without login).
        /// </summary>
        private async Task<PatientAuthUserDetails?> ExistingPatientUserAsync(string digits)
        {
            var resolved = await _patientAccounts.ResolveOrClaimAfterOtpAsync(digits);
            if (resolved == null)
                return null;

            var user = resolved.Value.User;
            var roleName = await _context.RoleMasters.AsNoTracking()
                .Where(r => r.RoleId == user.RoleId && !r.DeleteStatus)
                .Select(r => r.RoleName)
                .FirstOrDefaultAsync();
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            int? doctorId = await _context.Doctors.AsNoTracking()
                .Where(d => d.UserId == user.UserId && !d.DeleteStatus)
                .Select(d => (int?)d.DoctorId)
                .FirstOrDefaultAsync();

            var token = await _tokenService.CreateToken(user, 7 * 24 * 60, roleName, doctorId);
            return new PatientAuthUserDetails
            {
                UserId = user.UserId,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.EmailId ?? resolved.Value.Email,
                MobileNo = digits,
                Role = roleName,
                RoleId = user.RoleId,
                PatientId = resolved.Value.PatientId,
                PatientName = resolved.Value.PatientName,
                Token = token
            };
        }

        private sealed class PatientAuthUserDetails
        {
            public long UserId { get; set; }
            public string? UserName { get; set; }
            public string? FirstName { get; set; }
            public string? LastName { get; set; }
            public string? Email { get; set; }
            public string? MobileNo { get; set; }
            public string? Role { get; set; }
            public int? RoleId { get; set; }
            public int PatientId { get; set; }
            public string? PatientName { get; set; }
            public string Token { get; set; } = string.Empty;
        }
    }
}
