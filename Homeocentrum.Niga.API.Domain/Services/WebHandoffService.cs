using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Security.Audit;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Extensions;

namespace Homeocentrum.Niga.API.Domain.Services;

public interface IWebHandoffGuard
{
    /// <summary>Null when the handoff token is the current one for a live session.</summary>
    Task<string?> CheckAsync(string? sid, string? jti);
}

public interface IWebHandoffService
{
    Task<WebHandoffHttpResult> CreateAsync(ClaimsPrincipal user, string? purpose, int? patientAppId, string? returnUrl, string? ip);
    Task<WebHandoffHttpResult> ExchangeAsync(string? code, string? ip);
    Task<WebHandoffHttpResult> RenewAsync(ClaimsPrincipal user);
    Task<WebHandoffHttpResult> RevokeAsync(ClaimsPrincipal user);
    Task RevokeAllForUserAsync(long userId, string reason);
}

public sealed class WebHandoffHttpResult
{
    public int Status { get; init; }
    public object Body { get; init; } = new { success = false };

    public static WebHandoffHttpResult Ok(object body) => new() { Status = 200, Body = body };
    public static WebHandoffHttpResult Fail(int status, string code, string message) =>
        new() { Status = status, Body = new { success = false, code, message } };
}

public sealed class WebHandoffService : IWebHandoffService, IWebHandoffGuard
{
    private const string ExpiredMessage =
        "This link has expired. Return to the Doctor app and open the portal again.";

    private readonly NIGACentrumContext _context;
    private readonly ITokenService _tokens;
    private readonly ISubscriptionStatusService _subscription;
    private readonly IJwtDenylistService _denylist;
    private readonly ISecurityAuditLog _audit;
    private readonly IHttpContextAccessor _http;

    public WebHandoffService(
        NIGACentrumContext context,
        ITokenService tokens,
        ISubscriptionStatusService subscription,
        IJwtDenylistService denylist,
        ISecurityAuditLog audit,
        IHttpContextAccessor http)
    {
        _context = context;
        _tokens = tokens;
        _subscription = subscription;
        _denylist = denylist;
        _audit = audit;
        _http = http;
    }

    public async Task<WebHandoffHttpResult> CreateAsync(
        ClaimsPrincipal user, string? purpose, int? patientAppId, string? returnUrl, string? ip)
    {
        var normalized = WebHandoffPolicy.NormalizePurpose(purpose);
        if (normalized == null)
            return WebHandoffHttpResult.Fail(400, "PURPOSE_INVALID", "Choose the patient board or the doctor dashboard.");

        if (!IsDoctor(user))
            return WebHandoffHttpResult.Fail(403, "DOCTOR_ONLY", "Only a doctor can open the portal this way.");

        var userId = user.GetUserId();
        if (userId <= 0)
            return WebHandoffHttpResult.Fail(401, "TOKEN_INVALID", "Sign in again from the Doctor app.");

        try
        {
            var doctor = await ResolveDoctorAsync(user, userId);
            if (doctor == null)
                return WebHandoffHttpResult.Fail(403, "DOCTOR_ONLY", "Only a doctor can open the portal this way.");

            var now = DateTime.UtcNow;
            var since = now.Subtract(WebHandoffPolicy.CreateWindow);
            var recent = await _context.WebHandoffSessions.AsNoTracking()
                .CountAsync(s => s.DoctorUserId == userId && s.EnteredDate >= since);
            if (recent >= WebHandoffPolicy.MaxCreatesPerWindow)
                return WebHandoffHttpResult.Fail(429, "TOO_MANY", "Wait a moment, then open the portal again.");

            int? resolvedPatientId = null;
            int? resolvedCaseId = null;
            int? resolvedAppId = null;
            string? appointmentDate = null;
            string? visitType = null;
            string? consultMode = null;
            var isTele = false;

            if (normalized == WebHandoffPolicy.PurposePatientBoard)
            {
                if (patientAppId == null || patientAppId <= 0)
                    return WebHandoffHttpResult.Fail(400, "APPOINTMENT_REQUIRED", "Open the portal from the appointment.");

                var appointment = await _context.PatientAppointments.AsNoTracking()
                    .FirstOrDefaultAsync(a => a.PatientAppId == patientAppId.Value && a.DeleteStatus != true);
                if (appointment == null || appointment.DoctorId != doctor.DoctorId)
                    return WebHandoffHttpResult.Fail(404, "APPOINTMENT_NOT_YOURS", "This appointment is not on your list.");

                resolvedPatientId = appointment.PatientId;
                resolvedAppId = appointment.PatientAppId;
                appointmentDate = appointment.AppointmentDate?.ToString("yyyy-MM-dd");
                visitType = Trim40(appointment.VisitType);
                consultMode = Trim40(appointment.ConsultMode);
                isTele = appointment.IsTele == true;
                resolvedCaseId = await _context.CaseEntryDetails.AsNoTracking()
                    .Where(c => c.PatientId == appointment.PatientId
                        && c.DoctorId == doctor.DoctorId
                        && c.DeleteStatus != true)
                    .OrderByDescending(c => c.CaseId)
                    .Select(c => (int?)c.CaseId)
                    .FirstOrDefaultAsync();
            }

            var code = WebHandoffPolicy.CreateCode();
            var session = new WebHandoffSession
            {
                Sid = Guid.NewGuid(),
                CodeHash = SecurityTokenHash.Sha256Hex(code),
                DoctorUserId = userId,
                DoctorId = doctor.DoctorId,
                Purpose = normalized,
                PatientId = resolvedPatientId,
                CaseId = resolvedCaseId,
                PatientAppId = resolvedAppId,
                AppointmentDate = appointmentDate,
                VisitType = visitType,
                ConsultMode = consultMode,
                IsTele = isTele,
                ReturnUrl = WebHandoffPolicy.ResolveReturnUrl(returnUrl),
                CodeExpiresAtUtc = now.Add(WebHandoffPolicy.CodeLifetime),
                EnteredDate = now,
                CreatedFromIp = Trim64(ip),
            };
            _context.WebHandoffSessions.Add(session);
            await _context.SaveChangesAsync();

            await AuditAsync(SecurityAuditEvents.WebHandoffIssued, userId, "Doctor",
                $"sid={session.Sid:D}; purpose={normalized}; patientAppId={resolvedAppId}");

            return WebHandoffHttpResult.Ok(new
            {
                success = true,
                code,
                expiresIn = (int)WebHandoffPolicy.CodeLifetime.TotalSeconds,
                purpose = normalized,
            });
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return NotInstalled();
        }
    }

    public async Task<WebHandoffHttpResult> ExchangeAsync(string? code, string? ip)
    {
        var raw = (code ?? "").Trim();
        if (raw.Length < 20 || raw.Length > 200)
            return Rejected();

        try
        {
            var hash = SecurityTokenHash.Sha256Hex(raw);
            var now = DateTime.UtcNow;
            var claimed = await _context.WebHandoffSessions
                .Where(s => s.CodeHash == hash
                    && s.ConsumedAtUtc == null
                    && s.RevokedAtUtc == null
                    && s.CodeExpiresAtUtc > now)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ConsumedAtUtc, now));
            if (claimed != 1)
            {
                await AuditAsync(SecurityAuditEvents.WebHandoffRejected, null, null, "exchange rejected");
                return Rejected();
            }

            var session = await _context.WebHandoffSessions.FirstAsync(s => s.CodeHash == hash);
            var absolute = now.Add(WebHandoffPolicy.AbsoluteLifetime);
            var issued = await IssueAsync(session, now, absolute);
            if (issued == null)
            {
                session.RevokedAtUtc = now;
                session.RevokeReason = "exchange";
                await _context.SaveChangesAsync();
                return Rejected();
            }

            session.AbsoluteExpiresAtUtc = issued.Value.Absolute;
            session.LastActivityAtUtc = now;
            session.CurrentJti = issued.Value.Jti;
            _context.WebHandoffJtis.Add(new WebHandoffJti
            {
                Jti = issued.Value.Jti,
                Sid = session.Sid,
                ExpiresAtUtc = issued.Value.Expires,
            });
            await _context.SaveChangesAsync();

            await AuditAsync(SecurityAuditEvents.WebHandoffExchanged, session.DoctorUserId, "Doctor",
                $"sid={session.Sid:D}; purpose={session.Purpose}; patientAppId={session.PatientAppId}; ip={Trim64(ip)}");

            return WebHandoffHttpResult.Ok(new
            {
                success = true,
                message = "Portal opened.",
                data = issued.Value.Payload,
            });
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return NotInstalled();
        }
    }

    public async Task<WebHandoffHttpResult> RenewAsync(ClaimsPrincipal user)
    {
        if (!IsHandoff(user, out var sid, out var jti))
            return WebHandoffHttpResult.Fail(401, "NOT_HANDOFF", "This session was not opened from the Doctor app.");

        try
        {
            var now = DateTime.UtcNow;
            var session = await _context.WebHandoffSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Sid == sid);
            if (session == null)
                return WebHandoffHttpResult.Fail(401, "REVOKED", "This portal session has ended. Open it again from the Doctor app.");

            if (session.RevokedAtUtc != null)
                return WebHandoffHttpResult.Fail(401, "REVOKED", "This portal session was signed out. Open it again from the Doctor app.");

            if (session.AbsoluteExpiresAtUtc == null || session.AbsoluteExpiresAtUtc <= now.AddSeconds(30))
                return await EndAsync(session, "ABSOLUTE", "This portal session has reached its limit. Save your work, return to the app, and open the portal again.");

            if (!string.Equals(session.CurrentJti, jti, StringComparison.Ordinal))
                return await ReplayAsync(session, jti, now);

            if (session.LastActivityAtUtc != null
                && now - session.LastActivityAtUtc.Value > WebHandoffPolicy.IdleTimeout)
            {
                return await EndAsync(session, "IDLE", "The portal paused because it was not used. Return to the Doctor app and open it again. Saved work is kept.");
            }

            var expires = WebHandoffPolicy.CapExpiry(now, session.AbsoluteExpiresAtUtc.Value);
            if (expires <= now.AddSeconds(30))
                return await EndAsync(session, "ABSOLUTE", "This portal session has reached its limit. Save your work, return to the app, and open the portal again.");

            var doctorUser = await _context.UserMasters.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == session.DoctorUserId && !u.DeleteStatus);
            if (doctorUser == null)
                return await EndAsync(session, "REVOKED", "This portal session has ended. Open it again from the Doctor app.");

            var roleName = await RoleNameAsync(doctorUser);
            var (token, newJti) = await _tokens.CreateWebHandoffToken(
                doctorUser, roleName, session.DoctorId, session.Sid, expires, session.AbsoluteExpiresAtUtc.Value);

            var rotated = await _context.WebHandoffSessions
                .Where(s => s.Sid == sid && s.CurrentJti == jti && s.RevokedAtUtc == null && s.AbsoluteExpiresAtUtc > now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.CurrentJti, newJti)
                    .SetProperty(x => x.LastActivityAtUtc, now));
            if (rotated != 1)
            {
                var fresh = await _context.WebHandoffSessions.AsNoTracking().FirstAsync(s => s.Sid == sid);
                if (!string.Equals(fresh.CurrentJti, jti, StringComparison.Ordinal))
                    return await ReplayAsync(fresh, jti, now);
                return WebHandoffHttpResult.Fail(401, "REVOKED", "This portal session has ended. Open it again from the Doctor app.");
            }

            await _context.WebHandoffJtis.Where(j => j.Jti == jti)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.ReplacedAtUtc, now));
            _context.WebHandoffJtis.Add(new WebHandoffJti
            {
                Jti = newJti,
                Sid = sid,
                ExpiresAtUtc = expires,
            });
            await _context.SaveChangesAsync();
            _denylist.Deny(jti, expires);

            var remaining = (int)Math.Max(0, (session.AbsoluteExpiresAtUtc.Value - now).TotalSeconds);
            await AuditAsync(SecurityAuditEvents.WebHandoffRenewed, session.DoctorUserId, "Doctor", $"sid={sid:D}");

            return WebHandoffHttpResult.Ok(new
            {
                success = true,
                token,
                expiresAt = expires,
                absoluteExpiresAt = session.AbsoluteExpiresAtUtc,
                absoluteRemainingSeconds = remaining,
                renewAfterSeconds = WebHandoffPolicy.RenewAfterSeconds,
                warning = remaining <= WebHandoffPolicy.AbsoluteWarningSeconds,
            });
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return NotInstalled();
        }
    }

    public async Task<WebHandoffHttpResult> RevokeAsync(ClaimsPrincipal user)
    {
        if (!IsHandoff(user, out var sid, out _))
        {
            var userId = user.GetUserId();
            if (userId > 0)
                await RevokeAllForUserAsync(userId, "logout");
            return WebHandoffHttpResult.Ok(new { success = true });
        }

        try
        {
            var session = await _context.WebHandoffSessions.FirstOrDefaultAsync(s => s.Sid == sid);
            if (session != null && session.RevokedAtUtc == null)
                await MarkRevokedAsync(session, "return");
            return WebHandoffHttpResult.Ok(new { success = true });
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return WebHandoffHttpResult.Ok(new { success = true });
        }
    }

    public async Task RevokeAllForUserAsync(long userId, string reason)
    {
        if (userId <= 0)
            return;
        try
        {
            var now = DateTime.UtcNow;
            var floor = now.AddHours(-6);
            var rows = await _context.WebHandoffSessions
                .Where(s => s.DoctorUserId == userId && s.RevokedAtUtc == null && s.EnteredDate >= floor)
                .ToListAsync();
            foreach (var row in rows)
                StampRevoked(row, reason, now);
            if (rows.Count > 0)
            {
                await _context.SaveChangesAsync();
                await AuditAsync(SecurityAuditEvents.WebHandoffRevoked, userId, "Doctor", $"reason={reason}; count={rows.Count}");
            }
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            // Logout must still succeed when the handoff tables are not installed.
        }
    }

    public async Task<string?> CheckAsync(string? sidRaw, string? jti)
    {
        if (!Guid.TryParse(sidRaw, out var sid) || string.IsNullOrWhiteSpace(jti))
            return "This portal session is not valid.";
        try
        {
            var session = await _context.WebHandoffSessions.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Sid == sid);
            if (session == null || session.RevokedAtUtc != null)
                return "This portal session was signed out.";
            if (session.AbsoluteExpiresAtUtc == null || session.AbsoluteExpiresAtUtc <= DateTime.UtcNow)
                return "This portal session has ended.";
            if (string.Equals(session.CurrentJti, jti, StringComparison.Ordinal))
                return null;

            var prior = await _context.WebHandoffJtis.AsNoTracking()
                .FirstOrDefaultAsync(j => j.Jti == jti && j.Sid == sid);
            var now = DateTime.UtcNow;
            if (prior?.ReplacedAtUtc != null && now - prior.ReplacedAtUtc.Value > WebHandoffPolicy.ReuseGrace)
            {
                var tracked = await _context.WebHandoffSessions.FirstOrDefaultAsync(s => s.Sid == sid);
                if (tracked != null && tracked.RevokedAtUtc == null)
                    await MarkRevokedAsync(tracked, "reuse");
            }
            return "This portal session was replaced.";
        }
        catch (Exception ex) when (IsMissingTable(ex))
        {
            return "This portal session is not valid.";
        }
    }

    private async Task<WebHandoffHttpResult> ReplayAsync(WebHandoffSession session, string presentedJti, DateTime now)
    {
        var prior = await _context.WebHandoffJtis.AsNoTracking()
            .FirstOrDefaultAsync(j => j.Jti == presentedJti && j.Sid == session.Sid);
        if (prior?.ReplacedAtUtc == null || now - prior.ReplacedAtUtc.Value <= WebHandoffPolicy.ReuseGrace)
            return WebHandoffHttpResult.Fail(409, "SUPERSEDED", "The portal session was just renewed.");

        await MarkRevokedAsync(session, "reuse");
        return WebHandoffHttpResult.Fail(401, "REUSE", "This portal session was used from two places. Open it again from the Doctor app.");
    }

    private async Task<WebHandoffHttpResult> EndAsync(WebHandoffSession session, string code, string message)
    {
        await MarkRevokedAsync(session, code.ToLowerInvariant());
        return WebHandoffHttpResult.Fail(401, code, message);
    }

    private async Task MarkRevokedAsync(WebHandoffSession session, string reason)
    {
        var tracked = _context.Entry(session).State == EntityState.Detached
            ? await _context.WebHandoffSessions.FirstOrDefaultAsync(s => s.Sid == session.Sid)
            : session;
        if (tracked == null || tracked.RevokedAtUtc != null)
            return;
        StampRevoked(tracked, reason, DateTime.UtcNow);
        await _context.SaveChangesAsync();
        await AuditAsync(SecurityAuditEvents.WebHandoffRevoked, session.DoctorUserId, "Doctor",
            $"sid={session.Sid:D}; reason={reason}");
    }

    private void StampRevoked(WebHandoffSession session, string reason, DateTime now)
    {
        session.RevokedAtUtc = now;
        session.RevokeReason = Trim40(reason);
        if (!string.IsNullOrEmpty(session.CurrentJti))
        {
            var until = session.AbsoluteExpiresAtUtc ?? now.Add(WebHandoffPolicy.AbsoluteLifetime);
            _denylist.Deny(session.CurrentJti, until);
        }
    }

    private async Task<(string Jti, DateTime Expires, DateTime Absolute, object Payload)?> IssueAsync(
        WebHandoffSession session, DateTime now, DateTime absolute)
    {
        var user = await _context.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == session.DoctorUserId && !u.DeleteStatus);
        if (user == null || user.IsUserActivated != true)
            return null;

        var roleName = await RoleNameAsync(user);
        if (!string.Equals(roleName, "Doctor", StringComparison.OrdinalIgnoreCase))
            return null;

        var expires = WebHandoffPolicy.CapExpiry(now, absolute);
        var (token, jti) = await _tokens.CreateWebHandoffToken(user, roleName, session.DoctorId, session.Sid, expires, absolute);

        var doctor = await _context.Doctors.AsNoTracking()
            .Where(d => d.DoctorId == session.DoctorId && !d.DeleteStatus)
            .Select(d => new { d.FirstName, d.MiddleName, d.LastName })
            .FirstOrDefaultAsync();
        var first = doctor?.FirstName ?? user.FirstName;
        var middle = doctor?.MiddleName;
        var last = doctor?.LastName ?? user.LastName;
        var full = string.Join(" ", new[] { first, middle, last }.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim()));

        var plan = await _subscription.GetForDoctorAsync(session.DoctorId);
        var landingPath = BuildLandingPath(session);

        var payload = new
        {
            userId = user.UserId,
            userName = full.Length > 0 ? full : user.UserName,
            firstName = first ?? "",
            middleName = string.IsNullOrWhiteSpace(middle) ? null : middle.Trim(),
            lastName = last ?? "",
            role = roleName,
            roleId = user.RoleId,
            doctorId = session.DoctorId,
            token,
            isPlanActive = plan.IsPlanActive,
            islastFiveDays = plan.IslastFiveDays,
            daysRemaining = plan.DaysRemaining,
            tokenUse = WebHandoffPolicy.TokenUseValue,
            sid = session.Sid,
            purpose = session.Purpose,
            expiresAt = expires,
            absoluteExpiresAt = absolute,
            renewAfterSeconds = WebHandoffPolicy.RenewAfterSeconds,
            idleTimeoutSeconds = (int)WebHandoffPolicy.IdleTimeout.TotalSeconds,
            returnUrl = session.ReturnUrl,
            landingPath,
            patientAppId = session.PatientAppId,
            patientId = session.PatientId,
            caseId = session.CaseId,
        };
        return (jti, expires, absolute, payload);
    }

    private async Task<string> RoleNameAsync(UserMaster user)
    {
        var name = await _context.RoleMasters.AsNoTracking()
            .Where(r => r.RoleId == user.RoleId && !r.DeleteStatus)
            .Select(r => r.RoleName)
            .FirstOrDefaultAsync();
        return string.IsNullOrWhiteSpace(name) ? "" : name.Trim();
    }

    private async Task<Doctor?> ResolveDoctorAsync(ClaimsPrincipal user, int userId)
    {
        var claimId = user.GetDoctorId();
        if (claimId is > 0)
        {
            var byClaim = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorId == claimId.Value && !d.DeleteStatus);
            if (byClaim?.UserId == userId)
                return byClaim;
        }

        return await _context.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId && !d.DeleteStatus);
    }

    private static string BuildLandingPath(WebHandoffSession session)
    {
        if (!string.Equals(session.Purpose, WebHandoffPolicy.PurposePatientBoard, StringComparison.Ordinal)
            || session.PatientId == null)
            return "/doctordashboard";

        var query = new List<string>
        {
            "patientId=" + session.PatientId.Value,
        };
        if (session.CaseId is > 0)
            query.Add("caseId=" + session.CaseId.Value);
        if (session.PatientAppId is > 0)
            query.Add("patientAppId=" + session.PatientAppId.Value);
        if (!string.IsNullOrWhiteSpace(session.AppointmentDate))
            query.Add("appointmentDate=" + Uri.EscapeDataString(session.AppointmentDate));
        if (!string.IsNullOrWhiteSpace(session.VisitType))
            query.Add("visitType=" + Uri.EscapeDataString(session.VisitType));
        if (!string.IsNullOrWhiteSpace(session.ConsultMode))
            query.Add("consultMode=" + Uri.EscapeDataString(session.ConsultMode));
        if (session.IsTele)
            query.Add("isTele=true");
        return "/doctor/patientboard?" + string.Join("&", query);
    }

    private static bool IsDoctor(ClaimsPrincipal user)
    {
        var role = DoctorOwnership.GetRoleName(user);
        return string.Equals(role, "Doctor", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHandoff(ClaimsPrincipal user, out Guid sid, out string jti)
    {
        sid = Guid.Empty;
        jti = "";
        var use = user.FindFirst(WebHandoffPolicy.TokenUseClaim)?.Value;
        if (!string.Equals(use, WebHandoffPolicy.TokenUseValue, StringComparison.Ordinal))
            return false;
        jti = user.FindFirst("jti")?.Value
            ?? user.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value
            ?? "";
        return Guid.TryParse(user.FindFirst(WebHandoffPolicy.SidClaim)?.Value, out sid)
            && !string.IsNullOrWhiteSpace(jti);
    }

    private Task AuditAsync(string eventType, long? userId, string? role, string detail)
        => _audit.WriteAsync(eventType, _http.HttpContext, "SUCCESS", userId, role, detail: detail);

    private static WebHandoffHttpResult Rejected()
        => WebHandoffHttpResult.Fail(401, "HANDOFF_EXPIRED", ExpiredMessage);

    private static WebHandoffHttpResult NotInstalled()
        => WebHandoffHttpResult.Fail(503, "HANDOFF_NOT_INSTALLED", "Portal handoff is not installed on this server.");

    private static bool IsMissingTable(Exception ex)
    {
        for (var cursor = ex; cursor != null; cursor = cursor.InnerException)
        {
            if (cursor is SqlException sql && (sql.Number == 208 || sql.Number == 207))
                return true;
        }
        return false;
    }

    private static string? Trim40(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;
        return text.Length <= 40 ? text : text[..40];
    }

    private static string? Trim64(string? value)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            return null;
        return text.Length <= 64 ? text : text[..64];
    }
}
