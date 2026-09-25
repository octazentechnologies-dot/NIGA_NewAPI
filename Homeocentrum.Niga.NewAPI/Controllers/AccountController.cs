using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Services;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly NIGACentrumContext _context;
        private readonly IReceptionStaffService _receptionStaffService;
        private readonly ISubscriptionStatusService _subscriptionStatusService;
        private readonly IAuditEventWriter _auditEventWriter;
        private readonly IJwtDenylistService _jwtDenylist;
        private readonly IOptions<SmtpSettingsModel> _mailSettings;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly EmailSenderService _emailSender = new();

        public AccountController(
            ITokenService tokenService,
            NIGACentrumContext context,
            IReceptionStaffService receptionStaffService,
            ISubscriptionStatusService subscriptionStatusService,
            IAuditEventWriter auditEventWriter,
            IJwtDenylistService jwtDenylist,
            IOptions<SmtpSettingsModel> mailSettings,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _tokenService = tokenService;
            _context = context;
            _receptionStaffService = receptionStaffService;
            _subscriptionStatusService = subscriptionStatusService;
            _auditEventWriter = auditEventWriter;
            _jwtDenylist = jwtDenylist;
            _mailSettings = mailSettings;
            _configuration = configuration;
            _env = env;
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Invalid request data" });

                if (string.IsNullOrWhiteSpace(model.UserName) || string.IsNullOrWhiteSpace(model.Password))
                    return BadRequest(new { message = "Username and password are required" });

                var userEntity = await _context.UserMasters
                    .FirstOrDefaultAsync(x => x.UserName == model.UserName);

                if (userEntity != null)
                {
                    if (UserPasswordHasher.IsCorruptHash(userEntity.UserPassword))
                    {
                        return Unauthorized(new
                        {
                            message = "Invalid username or password",
                            detail = "Password hash in database is corrupted/truncated. Reset UserPassword to plaintext (known password) after ensuring column is NVARCHAR(500), then login again so the API can re-hash it. Do not paste manual encryption."
                        });
                    }

                    if (!UserPasswordHasher.Verify(model.Password, userEntity.UserPassword))
                        return Unauthorized(new { message = "Invalid username or password" });

                    // SEC-01.01 / SEC-01.02 — lazy migrate plaintext → PBKDF2
                    string? passwordHashWarning = null;
                    if (!UserPasswordHasher.IsHashed(userEntity.UserPassword))
                    {
                        passwordHashWarning = await PersistUserPasswordHashAsync(userEntity, model.Password);
                    }

                    if (userEntity.IsUserActivated != true)
                        return Unauthorized(new { message = "Account is deactivated. Please contact administrator." });

                    var roleEntity = await _context.RoleMasters
                        .FirstOrDefaultAsync(x => x.RoleId == userEntity.RoleId);

                    if (roleEntity == null)
                        return BadRequest(new { message = "User role not found" });

                    int? doctorId = null;
                    var doctorEntity = await _context.Doctors
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d =>
                            d.UserId == userEntity.UserId &&
                            d.DeleteStatus == false);

                    if (doctorEntity != null)
                        doctorId = doctorEntity.DoctorId;

                    var token = await _tokenService.CreateToken(
                        userEntity,
                        7 * 24 * 60,
                        roleEntity.RoleName,
                        doctorId);

                    var userData = new AuthModel
                    {
                        IsSuperUser = roleEntity.RoleId == 1,
                        UserId = userEntity.UserId,
                        UserName = $"{userEntity.FirstName} {userEntity.LastName}".Trim(),
                        Role = roleEntity.RoleName,
                        RoleId = userEntity.RoleId,
                        FirmIds = userEntity.FirmIds,
                        Token = token,
                        IsPlanActive = false,
                        IslastFiveDays = false,
                        DaysRemaining = 0,
                        DoctorId = doctorId
                    };

                    if (doctorEntity != null && (roleEntity.RoleId == 3
                        || string.Equals(roleEntity.RoleName, "Doctor", StringComparison.OrdinalIgnoreCase)))
                    {
                        var subscriptionStatus = await _subscriptionStatusService
                            .GetForDoctorAsync(doctorEntity.DoctorId);

                        userData.IsPlanActive = subscriptionStatus.IsPlanActive;
                        userData.DaysRemaining = subscriptionStatus.DaysRemaining;
                        userData.IslastFiveDays = subscriptionStatus.IslastFiveDays;
                    }

                    try
                    {
                        _context.UserLoginStatuses.Add(new UserLoginStatus
                        {
                            LoginId = await NextLoginIdAsync(),
                            LogDate = DateTime.Now.Date,
                            UserId = userEntity.UserId,
                            MachineNo = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "api",
                            InTime = DateTime.Now,
                            OutTime = null,
                            Satus = true
                        });
                        await _context.SaveChangesAsync();
                    }
                    catch
                    {
                        // Login status is best-effort; do not fail auth.
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Login successful",
                        data = userData,
                        warning = passwordHashWarning
                    });
                }

                var receptionAuth = await _receptionStaffService.TryBuildAuthModelForLoginAsync(
                    model.UserName,
                    model.Password);

                if (receptionAuth == null)
                    return Unauthorized(new { message = "Invalid username or password" });

                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = receptionAuth
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during login. Please try again."
                });
            }
        }

        /// <summary>SEC-03.01 — Persist UserLoginStatus.OutTime; denylist jti in memory.</summary>
        [HttpPost("Logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                int userId;
                try { userId = User.GetUserId(); }
                catch { userId = 0; }

                var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
                if (!string.IsNullOrEmpty(jti))
                {
                    var expires = DateTime.UtcNow.AddDays(7);
                    if (long.TryParse(expClaim, out var expUnix))
                        expires = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
                    _jwtDenylist.Deny(jti, expires);
                }

                if (userId > 0)
                {
                    try
                    {
                        var open = await _context.UserLoginStatuses
                            .Where(x => x.UserId == userId && x.OutTime == null)
                            .OrderByDescending(x => x.InTime)
                            .FirstOrDefaultAsync();
                        if (open != null)
                        {
                            open.OutTime = DateTime.Now;
                            open.Satus = false;
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch
                    {
                        // Table may be missing or LoginId constraints — ignore.
                    }
                }

                await _auditEventWriter.WriteAsync(
                    userId > 0 ? userId : null,
                    AdminAuthorizationPolicies.GetRoleName(User),
                    "Logout",
                    "UserSession",
                    correlationId: jti);

                return Ok(new { success = true, message = "Logged out successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Logout failed." });
            }
        }

        /// <summary>SEC-02.02 — Creates PasswordResetToken; emails reset LINK (never plaintext password).</summary>
        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            const string genericMessage = "If an account exists for that email, a password reset link has been sent.";

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Email))
                    return BadRequest(new { success = false, message = "Email is required." });

                var email = request.Email.Trim();
                var user = await _context.UserMasters.FirstOrDefaultAsync(x =>
                    !x.DeleteStatus
                    && ((x.EmailId != null && x.EmailId.ToLower() == email.ToLower())
                        || x.UserName.ToLower() == email.ToLower()));

                string? resetLink = null;
                var mailSent = false;

                if (user != null)
                {
                    var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
                    var tokenHash = SecurityTokenHash.Sha256Hex(rawToken);

                    _context.PasswordResetTokens.Add(new PasswordResetToken
                    {
                        UserId = user.UserId,
                        TokenHash = tokenHash,
                        ExpiresAt = DateTime.UtcNow.AddHours(2),
                        UsedAt = null,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    var siteUrl = GetUiSiteUrl();
                    // Path-style href survives Gmail's google.com/url?q= wrap (no ?token= inside q).
                    var pathLink = $"{siteUrl}/reset-password/{Uri.EscapeDataString(rawToken)}";
                    var queryLink = $"{siteUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
                    resetLink = pathLink;

                    var fullName = string.Join(" ", new[] { user.FirstName, user.LastName }
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .Select(s => s!.Trim()));
                    var greeting = WebUtility.HtmlEncode(
                        !string.IsNullOrWhiteSpace(fullName) ? fullName : (user.UserName ?? "there"));
                    var body = new StringBuilder();
                    body.Append("<body style='font-family:Arial,sans-serif;color:#1f2937;'>");
                    body.Append("<p>Hello " + greeting + ",</p>");
                    body.Append("<p>Use the button below to reset your Homeocentrum password. This link expires in 2 hours.</p>");
                    body.Append($"<p><a href=\"{WebUtility.HtmlEncode(pathLink)}\" style='display:inline-block;padding:10px 18px;background:#1e88e5;color:#fff;text-decoration:none;border-radius:6px;'>Reset password</a></p>");
                    body.Append("<p>If the button does not open, copy this address into your browser:</p>");
                    body.Append($"<p style='word-break:break-all;'>{WebUtility.HtmlEncode(pathLink)}</p>");
                    body.Append($"<p style='color:#6b7280;font-size:12px;'>Alternate link: {WebUtility.HtmlEncode(queryLink)}</p>");
                    body.Append("<p>If you did not request this, ignore this email.</p>");
                    body.Append("</body>");

                    if (!string.IsNullOrWhiteSpace(user.EmailId))
                    {
                        mailSent = _emailSender.SendMail(new EmailSenderModel
                        {
                            ToAddress = user.EmailId,
                            Subject = "Homeocentrum - Password Reset",
                            Body = body.ToString(),
                            isHtml = true
                        }, _mailSettings.Value);
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = genericMessage,
                    resetLink = _env.IsDevelopment() ? resetLink : null,
                    mailSent = _env.IsDevelopment() ? mailSent : (bool?)null,
                    smtpError = _env.IsDevelopment() && !mailSent ? _emailSender.LastError : null
                });
            }
            catch (Exception)
            {
                return Ok(new { success = true, message = genericMessage });
            }
        }

        [HttpPost("ResetPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (request == null
                    || string.IsNullOrWhiteSpace(request.Token)
                    || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { success = false, message = "Token and newPassword are required." });
                }

                var token = NormalizeResetToken(request.Token);
                if (string.IsNullOrWhiteSpace(token))
                    return BadRequest(new { success = false, message = "Token and newPassword are required." });

                var tokenHash = SecurityTokenHash.Sha256Hex(token);
                var reset = await _context.PasswordResetTokens
                    .Where(x => x.TokenHash == tokenHash && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

                if (reset == null)
                    return BadRequest(new { success = false, message = "Invalid or expired reset token." });

                var user = await _context.UserMasters.FirstOrDefaultAsync(x => x.UserId == reset.UserId);
                if (user == null)
                    return BadRequest(new { success = false, message = "Invalid or expired reset token." });

                user.UserPassword = UserPasswordHasher.Hash(request.NewPassword);
                user.PasswordRenewDate = DateTime.Now;
                user.ChangedDate = DateTime.Now;
                reset.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await _auditEventWriter.WriteAsync(user.UserId, null, "ResetPassword", "UserMaster");

                return Ok(new { success = true, message = "Password has been reset successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Unable to reset password." });
            }
        }

        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                if (request == null
                    || string.IsNullOrWhiteSpace(request.CurrentPassword)
                    || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { success = false, message = "currentPassword and newPassword are required." });
                }

                var userId = User.GetUserId();
                var user = await _context.UserMasters.FirstOrDefaultAsync(x => x.UserId == userId);
                if (user == null)
                    return Unauthorized(new { success = false, message = "User not found." });

                if (!UserPasswordHasher.Verify(request.CurrentPassword, user.UserPassword))
                    return BadRequest(new { success = false, message = "Current password is incorrect." });

                user.UserPassword = UserPasswordHasher.Hash(request.NewPassword);
                user.PasswordRenewDate = DateTime.Now;
                user.ChangedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                await _auditEventWriter.WriteAsync(
                    userId,
                    AdminAuthorizationPolicies.GetRoleName(User),
                    "ChangePassword",
                    "UserMaster");

                return Ok(new { success = true, message = "Password changed successfully." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Unable to change password." });
            }
        }

        /// <summary>
        /// SEC-01.01 — Bulk hash remaining plaintext UserMaster passwords.
        /// Requires UserPassword column widened (NVARCHAR(500)) first.
        /// </summary>
        [HttpPost("MigratePlaintextPasswords")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> MigratePlaintextPasswords()
        {
            try
            {
                var users = await _context.UserMasters
                    .Where(x => !x.DeleteStatus)
                    .ToListAsync();

                var migrated = 0;
                var skippedCorrupt = 0;
                foreach (var user in users)
                {
                    if (string.IsNullOrEmpty(user.UserPassword))
                        continue;

                    // Truncated/manual bad hashes cannot be migrated — need plaintext reset first
                    if (UserPasswordHasher.IsCorruptHash(user.UserPassword))
                    {
                        skippedCorrupt++;
                        continue;
                    }

                    if (UserPasswordHasher.IsHashed(user.UserPassword))
                        continue;

                    user.UserPassword = UserPasswordHasher.Hash(user.UserPassword);
                    user.ChangedDate = DateTime.Now;
                    migrated++;
                }

                if (migrated > 0)
                    await _context.SaveChangesAsync();

                await _auditEventWriter.WriteAsync(
                    User.GetUserId(),
                    AdminAuthorizationPolicies.GetRoleName(User),
                    "MigratePlaintextPasswords",
                    "UserMaster",
                    newJson: $"{{\"migrated\":{migrated},\"skippedCorrupt\":{skippedCorrupt}}}");

                return Ok(new
                {
                    success = true,
                    migrated,
                    skippedCorrupt,
                    message = skippedCorrupt > 0
                        ? $"Migrated {migrated} plaintext password(s). Skipped {skippedCorrupt} corrupt/truncated hash(es) — reset those users to plaintext then login or call SetUserPassword."
                        : $"Migrated {migrated} plaintext password(s)."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Migration failed. Ensure UserPassword column is widened (NVARCHAR(500)).",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// Ops recovery: set a user's password correctly (hashed). Use when DB has corrupt/manual hash.
        /// Requires AdminPortal. Always run SQL widen first.
        /// </summary>
        [HttpPost("SetUserPassword")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> SetUserPassword([FromBody] SetUserPasswordRequest request)
        {
            try
            {
                if (request == null
                    || string.IsNullOrWhiteSpace(request.UserName)
                    || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { message = "UserName and NewPassword are required" });
                }

                if (request.NewPassword.Length < 4)
                    return BadRequest(new { message = "NewPassword is too short" });

                var user = await _context.UserMasters
                    .FirstOrDefaultAsync(x => x.UserName == request.UserName && !x.DeleteStatus);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                var hashed = UserPasswordHasher.Hash(request.NewPassword);
                user.UserPassword = hashed;
                user.ChangedDate = DateTime.Now;
                await _context.SaveChangesAsync();

                await _context.Entry(user).ReloadAsync();
                if (!UserPasswordHasher.Verify(request.NewPassword, user.UserPassword))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Hash was truncated on save. Run M01_Foundation_Security_Server.sql to set UserPassword NVARCHAR(500)."
                    });
                }

                await _auditEventWriter.WriteAsync(
                    User.GetUserId(),
                    AdminAuthorizationPolicies.GetRoleName(User),
                    "SetUserPassword",
                    "UserMaster",
                    newJson: $"{{\"userId\":{user.UserId}}}");

                return Ok(new { success = true, message = "Password updated and verified." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>PAT-03.02 — Phone + OTP → JWT (web login stays classic password).</summary>
        [HttpPost("LoginWithOtp")]
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

                var users = await _context.UserMasters
                    .Where(u => !u.DeleteStatus && u.MobileNo != null)
                    .ToListAsync();
                var userEntity = users.FirstOrDefault(u => PhoneNormalizer.EqualsNormalized(u.MobileNo, mobile));
                if (userEntity == null)
                    return Unauthorized(new { success = false, message = "No account for this mobile number." });

                if (userEntity.IsUserActivated != true)
                    return Unauthorized(new { success = false, message = "Account is deactivated. Please contact administrator." });

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
                var userData = new AuthModel
                {
                    IsSuperUser = roleEntity.RoleId == 1,
                    UserId = userEntity.UserId,
                    UserName = $"{userEntity.FirstName} {userEntity.LastName}".Trim(),
                    Role = roleEntity.RoleName,
                    RoleId = userEntity.RoleId,
                    FirmIds = userEntity.FirmIds,
                    Token = token,
                    DoctorId = doctorId
                };

                return Ok(new { success = true, message = "Login successful", data = userData });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>DMO-02.02 — Confirm entered number matches UserMaster / Doctor profile.</summary>
        [HttpPost("ConfirmMobile")]
        [Authorize]
        public async Task<IActionResult> ConfirmMobile([FromBody] ConfirmMobileRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.MobileNo))
                return BadRequest(new { success = false, message = "MobileNo is required." });

            var userId = User.GetUserId();
            var user = await _context.UserMasters.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId && !u.DeleteStatus);
            if (user == null)
                return NotFound(new { success = false, message = "User not found." });

            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId && d.DeleteStatus == false);

            var patientMobile = await (
                from m in _context.PatientUserMaps.AsNoTracking()
                join p in _context.Patients.AsNoTracking() on m.PatientId equals p.PatientId
                where m.UserId == userId && !m.DeleteStatus && p.DeleteStatus != true
                orderby m.IsPrimary descending
                select p.MobileNo).FirstOrDefaultAsync();

            var entered = PhoneNormalizer.Digits(request.MobileNo);
            var matched = PhoneNormalizer.EqualsNormalized(entered, user.MobileNo)
                || PhoneNormalizer.EqualsNormalized(entered, doctor?.MobileNo)
                || PhoneNormalizer.EqualsNormalized(entered, patientMobile);

            var profileSource = user.MobileNo ?? doctor?.MobileNo ?? patientMobile;

            return Ok(new
            {
                success = true,
                data = new
                {
                    matched,
                    profileMasked = PhoneNormalizer.Mask(profileSource),
                    role = AdminAuthorizationPolicies.GetRoleName(User)
                }
            });
        }

        [HttpGet("SubscriptionStatus")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            try
            {
                int userId = User.GetUserId();

                var doctorEntity = await _context.Doctors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.UserId == userId && d.DeleteStatus == false);

                if (doctorEntity == null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new SubscriptionStatusModel()
                    });
                }

                var subscriptionStatus = await _subscriptionStatusService
                    .GetForDoctorAsync(doctorEntity.DoctorId);

                return Ok(new
                {
                    success = true,
                    data = subscriptionStatus
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to fetch subscription status."
                });
            }
        }

        /// <summary>
        /// Writes PBKDF2 hash via SQL UPDATE (avoids EF tracker quirks).
        /// Restores plaintext only when DB value is clearly truncated/corrupt.
        /// </summary>
        private async Task<string?> PersistUserPasswordHashAsync(UserMaster user, string plaintextPassword)
        {
            var hashed = UserPasswordHasher.Hash(plaintextPassword);
            if (!UserPasswordHasher.Verify(plaintextPassword, hashed))
                throw new InvalidOperationException("Password hasher self-check failed.");

            var now = DateTime.UtcNow;
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.UserMaster SET UserPassword = {0}, ChangedDate = {1} WHERE UserId = {2}",
                hashed,
                now,
                user.UserId);

            var stored = await _context.UserMasters
                .AsNoTracking()
                .Where(x => x.UserId == user.UserId)
                .Select(x => x.UserPassword)
                .FirstOrDefaultAsync();

            if (UserPasswordHasher.IsWellFormedHash(stored)
                && UserPasswordHasher.Verify(plaintextPassword, stored))
            {
                user.UserPassword = stored;
                user.ChangedDate = now;
                return null;
            }

            if (string.IsNullOrEmpty(stored)
                || stored.Length < UserPasswordHasher.MinWellFormedHashLength
                || UserPasswordHasher.IsCorruptHash(stored))
            {
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE dbo.UserMaster SET UserPassword = {0}, ChangedDate = {1} WHERE UserId = {2}",
                    plaintextPassword,
                    now,
                    user.UserId);
                user.UserPassword = plaintextPassword;
                user.ChangedDate = now;
                return "UserPassword column still too short or truncated the hash. Password kept as plaintext. Confirm NVARCHAR(500) on this DB, restart API, login again.";
            }

            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.UserMaster SET UserPassword = {0}, ChangedDate = {1} WHERE UserId = {2}",
                hashed,
                now,
                user.UserId);
            user.UserPassword = hashed;
            user.ChangedDate = now;
            return $"Password hash re-written (previous len={stored.Length}); hash retained.";
        }

        private async Task<int> NextLoginIdAsync()
        {
            var max = await _context.UserLoginStatuses
                .Select(x => (int?)x.LoginId)
                .MaxAsync();
            return (max ?? 0) + 1;
        }

        private string GetUiSiteUrl()
        {
            var url = _configuration["ConfigurationModel:SiteUrl"]
                ?? _configuration["AppSettings:UiBaseUrl"]
                ?? _mailSettings.Value?.SiteUrl;
            if (string.IsNullOrWhiteSpace(url))
                url = _env.IsDevelopment() ? "http://localhost:3000" : "https://homeocentrum.com";
            return url.TrimEnd('/');
        }

        /// <summary>
        /// Accepts a raw token, a /reset-password/:token path, ?token=, or a Gmail google.com/url?q= wrap.
        /// </summary>
        internal static string NormalizeResetToken(string? raw)
        {
            var token = (raw ?? string.Empty).Trim().Trim('"');
            if (string.IsNullOrEmpty(token))
                return token;

            for (var i = 0; i < 3; i++)
            {
                try
                {
                    var decoded = Uri.UnescapeDataString(token.Replace("+", "%2B"));
                    if (decoded == token)
                        break;
                    token = decoded;
                }
                catch
                {
                    break;
                }
            }

            var qMatch = Regex.Match(token, @"[?&]q=([^&]+)", RegexOptions.IgnoreCase);
            if (qMatch.Success)
            {
                try
                {
                    token = Uri.UnescapeDataString(qMatch.Groups[1].Value);
                }
                catch
                {
                    token = qMatch.Groups[1].Value;
                }
            }

            var tokenMatch = Regex.Match(token, @"[?&]token=([^&]+)", RegexOptions.IgnoreCase);
            if (tokenMatch.Success)
            {
                try
                {
                    return Uri.UnescapeDataString(tokenMatch.Groups[1].Value).Trim();
                }
                catch
                {
                    return tokenMatch.Groups[1].Value.Trim();
                }
            }

            var pathMatch = Regex.Match(token, @"reset-password/([^/?#]+)", RegexOptions.IgnoreCase);
            if (pathMatch.Success)
            {
                try
                {
                    return Uri.UnescapeDataString(pathMatch.Groups[1].Value).Trim();
                }
                catch
                {
                    return pathMatch.Groups[1].Value.Trim();
                }
            }

            return token.Split('&', '#')[0].Trim();
        }
    }
}
