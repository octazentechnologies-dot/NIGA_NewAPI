using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers
{
    /// <summary>SEC-07.02 / SEC-07.03 — Generic OTP request/verify + audit (SMS via ISmsSender; email via SMTP).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OtpController : ControllerBase
    {
        private const int MaxAttempts = 5;
        private const int LockoutMinutes = 15;
        private const int OtpTtlMinutes = 10;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);
        private const int MaxPerWindow = 3;

        private readonly NIGACentrumContext _context;
        private readonly ISmsSender _smsSender;
        private readonly ISmsTemplateRenderer _templates;
        private readonly ISmsOtpVerifyService _smsOtp;
        private readonly IOptions<SmtpSettingsModel> _mailSettings;
        private readonly SmsOptions _smsOptions;
        private readonly EmailSenderService _emailSender = new();

        public OtpController(
            NIGACentrumContext context,
            ISmsSender smsSender,
            ISmsTemplateRenderer templates,
            ISmsOtpVerifyService smsOtp,
            IOptions<SmtpSettingsModel> mailSettings,
            IOptions<SmsOptions> smsOptions)
        {
            _context = context;
            _smsSender = smsSender;
            _templates = templates;
            _smsOtp = smsOtp;
            _mailSettings = mailSettings;
            _smsOptions = smsOptions.Value ?? new SmsOptions();
        }

        [SecurityAudit(SecurityAuditEvents.OtpRequested)]
        [HttpPost("RequestOtp")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpModel request)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.Action)
                || string.IsNullOrWhiteSpace(request.EntityType)
                || string.IsNullOrWhiteSpace(request.EntityId)
                || string.IsNullOrWhiteSpace(request.Destination))
            {
                return BadRequest(new { success = false, message = "Action, EntityType, EntityId, and Destination are required." });
            }

            var isAnonymousAction =
                request.Action.Equals("Login", StringComparison.OrdinalIgnoreCase)
                || request.Action.Equals("PatientAuth", StringComparison.OrdinalIgnoreCase);
            if (!isAnonymousAction && User?.Identity?.IsAuthenticated != true)
                return Unauthorized(new { success = false, message = "Sign in required to request this OTP." });

            if (isAnonymousAction)
            {
                var digits = Homeocentrum.Niga.API.Domain.Security.PhoneNormalizer.Digits(request.Destination);
                if (digits.Length < 8)
                    return BadRequest(new { success = false, message = "Destination must be a valid mobile number for Login OTP." });
                request.EntityType = "Mobile";
                request.EntityId = digits;
                request.Destination = digits;
            }

            var since = DateTime.UtcNow.Subtract(RateLimitWindow);
            var recent = await _context.OtpChallenges.CountAsync(c =>
                c.EntityType == request.EntityType
                && c.EntityId == request.EntityId
                && c.Action == request.Action
                && c.CreatedAt >= since);

            if (recent >= MaxPerWindow)
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = "Too many OTP requests. Try again later." });

            var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
            var masked = MaskDestination(request.Destination);

            // Delivery via ISmsSender (ConfigurableSmsSender — Stub / Msg91 / Twilio).
            // Plaintext OTP is only returned as devCode when real SMS is not live.
            // Twilio Trial sms_2fa embeds its own code — use that for the stored hash.

            var otpText = await _templates.RenderAsync("OTP", new Dictionary<string, string?>
            {
                ["otp"] = code,
                ["minutes"] = OtpTtlMinutes.ToString()
            });
            var isEmailDestination = LooksLikeEmail(request.Destination);
            var delivered = false;
            var channel = isEmailDestination ? "email" : "sms";
            SmsSendResult? smsSend = null;

            if (isEmailDestination)
            {
                delivered = _emailSender.SendMail(new EmailSenderModel
                {
                    ToAddress = request.Destination.Trim(),
                    Subject = "Homeocentrum - Verification code",
                    Body = BuildOtpEmailBody(code, OtpTtlMinutes),
                    isHtml = true
                }, _mailSettings.Value);
            }
            else if (SmsOtpPolicy.RealSmsLive(_smsOptions))
            {
                smsSend = await _smsSender.SendAsync(request.Destination, otpText);
                delivered = smsSend.Accepted;
                if (!string.IsNullOrWhiteSpace(smsSend.EmbeddedOtp))
                    code = smsSend.EmbeddedOtp!;
            }

            var challenge = new OtpChallenge
            {
                Action = request.Action.Trim(),
                EntityType = request.EntityType.Trim(),
                EntityId = request.EntityId.Trim(),
                DestinationMasked = masked,
                OtpHash = SecurityTokenHash.Sha256Hex(code),
                ExpiresAt = DateTime.UtcNow.AddMinutes(OtpTtlMinutes),
                AttemptCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.OtpChallenges.Add(challenge);
            _context.OtpAuditLogs.Add(new OtpAuditLog
            {
                Action = "RequestOtp",
                EntityType = challenge.EntityType,
                EntityId = challenge.EntityId,
                ToMasked = masked,
                Success = true,
                At = DateTime.UtcNow,
                ActorUserId = User?.Identity?.IsAuthenticated == true ? User.GetUserId() : null
            });
            await _context.SaveChangesAsync();

            long? smsOtpVerifyId = null;
            if (!isEmailDestination)
            {
                var digits = Homeocentrum.Niga.API.Domain.Security.PhoneNormalizer.Digits(request.Destination);
                var stored = await _smsOtp.StoreAsync(
                    challenge.Action,
                    digits,
                    code,
                    smsSend?.ProviderLabel,
                    smsSend?.MessageSid,
                    OtpTtlMinutes,
                    challenge.OtpChallengeId);
                smsOtpVerifyId = stored.SmsOtpVerifyId;
            }

            var payload = new Dictionary<string, object?>
            {
                ["otpChallengeId"] = challenge.OtpChallengeId,
                ["smsOtpVerifyId"] = smsOtpVerifyId,
                ["destinationMasked"] = masked,
                ["expiresAt"] = challenge.ExpiresAt,
                ["channel"] = channel,
                ["delivered"] = delivered,
                ["trialTemplate"] = smsSend?.UsedTrialTemplate == true,
                ["message"] = delivered
                    ? $"OTP sent via {channel}. Enter the code from your phone or email."
                    : SmsOtpPolicy.RealSmsLive(_smsOptions)
                        ? $"OTP created but {channel} delivery did not confirm. Check provider/SMTP settings."
                        : "OTP created. SMS is not live; use the development code."
            };
            // Sms:RealSms false → no provider SMS, always return devCode.
            // Sms:RealSms true → DevCodeOrNull is null; the phone is the only source of the OTP.
            var devCode = SmsOtpPolicy.DevCodeOrNull(_smsOptions, code);
            if (devCode != null)
                payload["devCode"] = devCode;

            return Ok(new { success = true, data = payload });
        }

        [SecurityAudit(SecurityAuditEvents.Login)]
        [HttpPost("VerifyOtp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpModel request)
        {
            if (request == null || request.OtpChallengeId <= 0 || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { success = false, message = "OtpChallengeId and Code are required." });

            var challenge = await _context.OtpChallenges
                .FirstOrDefaultAsync(c => c.OtpChallengeId == request.OtpChallengeId);
            if (challenge == null)
                return NotFound(new { success = false, message = "Challenge not found." });

            // Prefer SmsOtpVerify when a row was stored for this challenge (real SMS path).
            var smsRowId = await _context.SmsOtpVerifies.AsNoTracking()
                .Where(x => x.OtpChallengeId == challenge.OtpChallengeId && x.VerifiedAt == null)
                .OrderByDescending(x => x.SmsOtpVerifyId)
                .Select(x => (long?)x.SmsOtpVerifyId)
                .FirstOrDefaultAsync();
            if (smsRowId is > 0)
            {
                var check = await _smsOtp.VerifyByIdAsync(smsRowId.Value, request.Code.Trim());
                await WriteAuditAsync(challenge, check.Success);
                if (!check.Success)
                {
                    var locked = string.Equals(check.Error, "OTP locked. Try later.", StringComparison.Ordinal);
                    return StatusCode(
                        locked ? StatusCodes.Status423Locked : StatusCodes.Status400BadRequest,
                        new { success = false, message = check.Error ?? "Invalid OTP." });
                }
                return Ok(new { success = true, message = "OTP verified.", smsOtpVerifyId = check.SmsOtpVerifyId });
            }

            if (challenge.LockedUntil.HasValue && challenge.LockedUntil > DateTime.UtcNow)
            {
                await WriteAuditAsync(challenge, false);
                return StatusCode(StatusCodes.Status423Locked, new { success = false, message = "OTP locked due to too many attempts." });
            }

            if (challenge.VerifiedAt != null || challenge.ExpiresAt < DateTime.UtcNow)
            {
                await WriteAuditAsync(challenge, false);
                return BadRequest(new { success = false, message = "OTP expired or already used." });
            }

            challenge.AttemptCount++;
            var ok = string.Equals(
                SecurityTokenHash.Sha256Hex(request.Code.Trim()),
                challenge.OtpHash,
                StringComparison.OrdinalIgnoreCase);

            if (!ok)
            {
                if (challenge.AttemptCount >= MaxAttempts)
                    challenge.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _context.SaveChangesAsync();
                await WriteAuditAsync(challenge, false);
                return BadRequest(new { success = false, message = "Invalid OTP." });
            }

            challenge.VerifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await WriteAuditAsync(challenge, true);

            return Ok(new { success = true, message = "OTP verified." });
        }

        /// <summary>SEC-07.03 — Account and Admin roles (masked destination).</summary>
        [HttpGet("Audit")]
        [Authorize(Policy = AdminAuthorizationPolicies.AccountOrAdmin)]
        public async Task<IActionResult> Audit([FromQuery] int take = 100)
        {
            take = Math.Clamp(take, 1, 500);
            var rows = await _context.OtpAuditLogs
                .AsNoTracking()
                .OrderByDescending(a => a.At)
                .Take(take)
                .Select(a => new
                {
                    a.OtpAuditLogId,
                    a.Action,
                    a.EntityType,
                    a.EntityId,
                    a.ToMasked,
                    a.Success,
                    a.At,
                    a.ActorUserId
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }

        private async Task WriteAuditAsync(OtpChallenge challenge, bool success)
        {
            _context.OtpAuditLogs.Add(new OtpAuditLog
            {
                Action = "VerifyOtp",
                EntityType = challenge.EntityType,
                EntityId = challenge.EntityId,
                ToMasked = challenge.DestinationMasked,
                Success = success,
                At = DateTime.UtcNow,
                ActorUserId = User?.Identity?.IsAuthenticated == true ? User.GetUserId() : null
            });
            await _context.SaveChangesAsync();
        }

        private static string MaskDestination(string destination)
        {
            var d = destination.Trim();
            if (d.Contains('@'))
            {
                var at = d.IndexOf('@');
                if (at <= 1) return "***" + d[at..];
                return d[0] + "***" + d[at..];
            }

            if (d.Length <= 4) return "****";
            return new string('*', d.Length - 4) + d[^4..];
        }

        private static bool LooksLikeEmail(string destination) =>
            !string.IsNullOrWhiteSpace(destination) && destination.Contains('@');

        private static string BuildOtpEmailBody(string code, int ttlMinutes)
        {
            var safeCode = WebUtility.HtmlEncode(code);
            var body = new StringBuilder();
            body.Append("<body style='font-family:Arial,sans-serif;color:#1f2937;'>");
            body.Append("<p>Hello,</p>");
            body.Append($"<p>Your Homeocentrum verification code is <strong>{safeCode}</strong>.</p>");
            body.Append($"<p>This code is valid for {ttlMinutes} minutes. Do not share it with anyone.</p>");
            body.Append("<p>If you did not request this, ignore this email.</p>");
            body.Append("</body>");
            return body.ToString();
        }
    }
}
