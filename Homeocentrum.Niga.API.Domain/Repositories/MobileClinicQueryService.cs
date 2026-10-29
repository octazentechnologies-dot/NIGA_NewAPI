using System.Threading;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Realtime;
using Homeocentrum.Niga.API.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Homeocentrum.Niga.API.Domain.Repositories;

/// <summary>
/// Mobile clinic routes that read or write stored rows. Amounts come from doctor fees or payment orders.
/// Fields the database does not store are returned as null.
/// </summary>
public class MobileClinicQueryService
{
    private static int _schemaReady;
    private const int CareLinkExpiryHours = 48;
    private readonly NIGACentrumContext _context;
    private readonly IHttpContextAccessor _http;
    private readonly IConfiguration _config;
    private readonly IAppointmentRescheduleNotifier _notify;
    private readonly ISmsTemplateRenderer _templates;
    private readonly ISmsSender _sms;
    private readonly IClinicInboxFanout _clinicInbox;
    private readonly IClinicRealtimeNotifier _realtime;

    public MobileClinicQueryService(
        NIGACentrumContext context,
        IHttpContextAccessor http,
        IConfiguration config,
        IAppointmentRescheduleNotifier notify,
        ISmsTemplateRenderer templates,
        ISmsSender sms,
        IClinicInboxFanout clinicInbox,
        IClinicRealtimeNotifier realtime)
    {
        _context = context;
        _http = http;
        _config = config;
        _notify = notify;
        _templates = templates;
        _sms = sms;
        _clinicInbox = clinicInbox;
        _realtime = realtime;
    }

    public async Task<S4ActionResult> DoctorFollowUpsAsync(S4Caller caller, string? status, DateTime? from, DateTime? to)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.Database.SqlQuery<FollowUpListRow>($@"
            SELECT t.FollowUpTaskId, t.Title, t.DueDate, t.Status, p.PatientId, p.PatientAppId, pt.PatientName
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            INNER JOIN dbo.Patient pt ON pt.PatientId = p.PatientId
            WHERE p.DoctorId = {caller.DoctorId.Value}
            ORDER BY t.DueDate").ToListAsync();
        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(r => string.Equals(r.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (from.HasValue)
            rows = rows.Where(r => r.DueDate.Date >= from.Value.Date).ToList();
        if (to.HasValue)
            rows = rows.Where(r => r.DueDate.Date <= to.Value.Date).ToList();
        return Ok(rows);
    }

    public async Task<S4ActionResult> CompleteDoctorFollowUpAsync(int followUpTaskId, string? note, S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var row = await _context.Database.SqlQuery<FollowUpOwnerRow>($@"
            SELECT t.FollowUpTaskId, t.Status, p.DoctorId
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            WHERE t.FollowUpTaskId = {followUpTaskId}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Follow-up task not found.");
        if (row.DoctorId != caller.DoctorId.Value) return Fail(403, "FORBIDDEN", "This task belongs to another clinic.");
        if (string.Equals(row.Status, "DONE", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "This task is already complete.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.FollowUpTask
            SET Status = N'DONE', CompletedAt = {DateTime.Now}
            WHERE FollowUpTaskId = {followUpTaskId}");
        _ = note; // optional note is accepted but not stored until a note column exists
        return Ok(new { followUpTaskId, status = "DONE" });
    }

    public async Task<S4ActionResult> ReopenDoctorFollowUpAsync(int followUpTaskId, S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var row = await _context.Database.SqlQuery<FollowUpOwnerRow>($@"
            SELECT t.FollowUpTaskId, t.Status, p.DoctorId
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            WHERE t.FollowUpTaskId = {followUpTaskId}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Follow-up task not found.");
        if (row.DoctorId != caller.DoctorId.Value) return Fail(403, "FORBIDDEN", "This task belongs to another clinic.");
        if (string.Equals(row.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "This task is already open.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.FollowUpTask
            SET Status = N'OPEN', CompletedAt = NULL
            WHERE FollowUpTaskId = {followUpTaskId}");
        return Ok(new { followUpTaskId, status = "OPEN" });
    }

    public async Task<S4ActionResult> DoctorDirectoryAsync(string? search, int page, int pageSize, S4Caller caller)
    {
        if (!caller.DoctorId.HasValue && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;
        var selfId = caller.DoctorId ?? 0;
        var q = (search ?? "").Trim();
        var doctors = await _context.Doctors.AsNoTracking()
            .Where(d => !d.DeleteStatus
                        && d.DirectoryVisible
                        && d.DoctorId != selfId
                        && d.VerificationStatus == "Verified")
            .Select(d => new
            {
                d.DoctorId,
                d.FirstName,
                d.MiddleName,
                d.LastName,
                d.PassingCertNo,
                d.City,
                d.PhotoPath,
                d.PassingUniversity
            })
            .ToListAsync();
        if (q.Length > 0)
        {
            doctors = doctors.Where(d =>
            {
                var name = string.Join(" ", new[] { d.FirstName, d.MiddleName, d.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return name.Contains(q, StringComparison.OrdinalIgnoreCase)
                       || (!string.IsNullOrWhiteSpace(d.City) && d.City.Contains(q, StringComparison.OrdinalIgnoreCase))
                       || (!string.IsNullOrWhiteSpace(d.PassingCertNo) && d.PassingCertNo.Contains(q, StringComparison.OrdinalIgnoreCase));
            }).ToList();
        }
        var total = doctors.Count;
        var pageRows = doctors
            .OrderBy(d => d.FirstName).ThenBy(d => d.LastName).ThenBy(d => d.DoctorId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d =>
            {
                var displayName = string.Join(" ", new[] { d.FirstName, d.MiddleName, d.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
                if (!string.IsNullOrWhiteSpace(displayName) && !displayName.StartsWith("Dr", StringComparison.OrdinalIgnoreCase))
                    displayName = "Dr. " + displayName;
                return new
                {
                    doctorId = d.DoctorId,
                    displayName,
                    registrationNo = string.IsNullOrWhiteSpace(d.PassingCertNo) ? null : d.PassingCertNo,
                    city = string.IsNullOrWhiteSpace(d.City) ? null : d.City,
                    careLinkEligible = true,
                    photoUrl = AbsoluteDoctorPhotoUrl(d.DoctorId, d.PhotoPath),
                    specialityLabel = string.IsNullOrWhiteSpace(d.PassingUniversity) ? null : d.PassingUniversity
                };
            })
            .ToList();
        return Ok(new { totalRecords = total, page, pageSize, data = pageRows });
    }

    public async Task<S4ActionResult> PracticeStatsAsync(S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var doctorId = caller.DoctorId.Value;
        var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.DoctorId == doctorId);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor profile was not found.");

        var review = await _context.Database.SqlQuery<ReviewStatRow>($@"
            SELECT
                ISNULL(CAST(AVG(CAST(Rating AS float)) AS decimal(9,1)), 0) AS AverageRating,
                COUNT(1) AS ReviewCount
            FROM dbo.Review
            WHERE DoctorId = {doctorId} AND Status = N'APPROVED'").FirstAsync();

        var appointments = await _context.PatientAppointments.AsNoTracking()
            .Where(a => a.DoctorId == doctorId && a.DeleteStatus != true)
            .Select(a => new { a.PatientAppId, a.Status })
            .ToListAsync();
        var completed = appointments.Count(a => IsCompleted(a.Status));
        var cancelled = appointments.Count(a => IsCancelled(a.Status));
        var decided = completed + cancelled;
        decimal? completionRate = decided == 0 ? null : Math.Round((decimal)completed / decided, 3);

        var signedNotes = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(DISTINCT s.PatientAppId) AS Value
            FROM dbo.ConsultationSummary s
            INNER JOIN dbo.PatientAppointment a ON a.PatientAppId = s.PatientAppId
            WHERE a.DoctorId = {doctorId} AND ISNULL(a.DeleteStatus, 0) = 0").FirstAsync();

        int? accountAgeDays = doctor.EnteredDate.HasValue
            ? (int)(DateTime.Today - doctor.EnteredDate.Value.Date).TotalDays
            : null;

        return Ok(new
        {
            doctorId,
            averageRating = review.ReviewCount == 0 ? (decimal?)null : review.AverageRating,
            reviewCount = review.ReviewCount,
            completedVisits = completed,
            cancelledVisits = cancelled,
            completionRate,
            signedNoteCount = signedNotes?.Value ?? 0,
            accountAgeDays,
            yearsExperience = (int?)null,
            punctuality = (decimal?)null,
            lifetimeConsultCount = completed,
            attendedPct = completionRate,
            punctualityPct = (decimal?)null,
            completionPct = completionRate,
            noteSigningPct = appointments.Count == 0
                ? (decimal?)null
                : Math.Round((decimal)(signedNotes.Value) / appointments.Count, 3)
        });
    }

    public async Task<S4ActionResult> DoctorActivityAsync(S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.PatientAppointments.AsNoTracking()
            .Where(a => a.DoctorId == caller.DoctorId.Value && a.DeleteStatus != true && a.AppointmentDate != null)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenByDescending(a => a.PatientAppId)
            .Take(30)
            .Select(a => new
            {
                kind = "appointment",
                at = a.AppointmentDate,
                a.PatientAppId,
                a.PatientId,
                patientName = a.Patient.PatientName,
                a.Status,
                a.ConsultMode,
                a.PaymentStatus
            })
            .ToListAsync();
        return Ok(rows);
    }

    public async Task<S4ActionResult> CheckAvailabilityAsync(string? userName, string? emailId)
    {
        var name = string.IsNullOrWhiteSpace(userName) ? null : userName.Trim();
        var email = string.IsNullOrWhiteSpace(emailId) ? null : emailId.Trim();
        if (name == null && email == null)
            return Fail(400, "VALIDATION", "userName or emailId is required.");
        bool? userNameAvailable = null;
        bool? emailAvailable = null;
        if (name != null)
            userNameAvailable = !await _context.UserMasters.AsNoTracking().AnyAsync(u => u.UserName == name);
        if (email != null)
        {
            var userTaken = await _context.UserMasters.AsNoTracking().AnyAsync(u => u.EmailId == email);
            var doctorTaken = await _context.Doctors.AsNoTracking().AnyAsync(d => d.EmailId == email && !d.DeleteStatus);
            emailAvailable = !userTaken && !doctorTaken;
        }
        return Ok(new { userNameAvailable, emailAvailable });
    }

    public async Task<S4ActionResult> GetBookingRuleAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var row = await _context.Database.SqlQuery<BookingRuleRow>($@"
            SELECT GapMinutes, BookingHorizonDays
            FROM dbo.DoctorBookingRule
            WHERE DoctorId = {caller.DoctorId.Value}").FirstOrDefaultAsync();
        return Ok(row ?? new BookingRuleRow());
    }

    public async Task<S4ActionResult> SaveBookingRuleAsync(int? gapMinutes, int? horizonDays, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (gapMinutes is < 0 or > 240) return Fail(400, "VALIDATION", "gapMinutes must be from 0 to 240.");
        if (horizonDays is < 1 or > 90) return Fail(400, "VALIDATION", "bookingHorizonDays must be from 1 to 90.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.DoctorBookingRule
            SET GapMinutes = {gapMinutes}, BookingHorizonDays = {horizonDays}
            WHERE DoctorId = {caller.DoctorId.Value}");
        if (updated == 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.DoctorBookingRule (DoctorId, GapMinutes, BookingHorizonDays)
                VALUES ({caller.DoctorId.Value}, {gapMinutes}, {horizonDays})");
        }
        return await GetBookingRuleAsync(caller);
    }

    public async Task<S4ActionResult> AppointmentsByDateRangeAsync(DateTime from, DateTime to, S4Caller caller)
    {
        if (to.Date < from.Date) return Fail(400, "VALIDATION", "to must be on or after from.");
        if ((to.Date - from.Date).TotalDays > 62) return Fail(400, "VALIDATION", "Date range cannot exceed 62 days.");
        var end = to.Date.AddDays(1);

        // Prefer DoctorId (web dashboard / Queue parity). Fall back to UserId for legacy.
        var doctorId = caller.DoctorId;
        if (!doctorId.HasValue || doctorId.Value <= 0)
        {
            doctorId = await _context.Doctors.AsNoTracking()
                .Where(d => d.UserId == caller.UserId && !d.DeleteStatus)
                .Select(d => (int?)d.DoctorId)
                .FirstOrDefaultAsync();
        }

        var query = _context.PatientAppointments.AsNoTracking()
            .Where(a => a.DeleteStatus != true
                        && a.AppointmentDate.HasValue
                        && a.AppointmentDate.Value >= from.Date
                        && a.AppointmentDate.Value < end);
        query = doctorId.HasValue
            ? query.Where(a => a.DoctorId == doctorId.Value)
            : query.Where(a => a.UserId == caller.UserId);

        var rows = await query
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .Select(a => new
            {
                a.PatientAppId,
                a.PatientId,
                patientName = a.Patient.PatientName,
                mobileNo = a.Patient.MobileNo,
                a.DoctorId,
                a.AppointmentDate,
                a.AppointmentTime,
                a.Status,
                a.VisitType,
                a.ConsultMode,
                a.IsTele,
                a.PaymentStatus,
                a.BookingChannel,
                consultFee = a.IsTele == true
                    ? a.Doctor.ConsultFeeTele ?? a.Doctor.ConsultFeeInClinic
                    : a.Doctor.ConsultFeeInClinic ?? a.Doctor.ConsultFeeTele,
                currency = a.Doctor.FeeCurrency
            })
            .ToListAsync();
        var teleByApp = new Dictionary<int, int>();
        if (rows.Count > 0)
        {
            List<SessionMapRow> sessions;
            if (doctorId.HasValue)
            {
                var did = doctorId.Value;
                sessions = await _context.Database.SqlQuery<SessionMapRow>($@"
                    SELECT s.PatientAppId, MAX(s.TeleSessionId) AS TeleSessionId
                    FROM dbo.TeleSession s
                    INNER JOIN dbo.PatientAppointment a ON a.PatientAppId = s.PatientAppId
                    WHERE a.DoctorId = {did}
                      AND ISNULL(a.DeleteStatus, 0) = 0
                      AND a.AppointmentDate >= {from.Date} AND a.AppointmentDate < {end}
                      AND s.Status NOT IN (N'Ended', N'ENDED', N'Closed', N'CLOSED')
                    GROUP BY s.PatientAppId").ToListAsync();
            }
            else
            {
                sessions = await _context.Database.SqlQuery<SessionMapRow>($@"
                    SELECT s.PatientAppId, MAX(s.TeleSessionId) AS TeleSessionId
                    FROM dbo.TeleSession s
                    INNER JOIN dbo.PatientAppointment a ON a.PatientAppId = s.PatientAppId
                    WHERE a.UserId = {caller.UserId}
                      AND ISNULL(a.DeleteStatus, 0) = 0
                      AND a.AppointmentDate >= {from.Date} AND a.AppointmentDate < {end}
                      AND s.Status NOT IN (N'Ended', N'ENDED', N'Closed', N'CLOSED')
                    GROUP BY s.PatientAppId").ToListAsync();
            }
            foreach (var s in sessions)
                teleByApp[s.PatientAppId] = s.TeleSessionId;
        }
        return Ok(rows.Select(a => new
        {
            a.PatientAppId,
            a.PatientId,
            a.patientName,
            a.mobileNo,
            a.DoctorId,
            a.AppointmentDate,
            a.AppointmentTime,
            a.Status,
            a.VisitType,
            a.ConsultMode,
            a.IsTele,
            a.PaymentStatus,
            a.BookingChannel,
            a.consultFee,
            currency = string.IsNullOrWhiteSpace(a.currency) ? null : a.currency,
            teleSessionId = teleByApp.TryGetValue(a.PatientAppId, out var sid) ? sid : (int?)null
        }));
    }

    public async Task<S4ActionResult> CheckInAsync(int patientAppId, string? status, S4Caller caller)
    {
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Only the patient can check in.");
        var next = string.IsNullOrWhiteSpace(status) ? "WAITING" : status.Trim().ToUpperInvariant();
        if (next is not ("EARLY" or "WAITING" or "READY"))
            return Fail(400, "VALIDATION", "status must be EARLY, WAITING, or READY.");
        var appt = await _context.PatientAppointments
            .FirstOrDefaultAsync(a => a.PatientAppId == patientAppId && a.DeleteStatus != true);
        if (appt == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (appt.PatientId != caller.PatientId) return Fail(403, "FORBIDDEN", "This visit belongs to another patient.");
        if (appt.IsTele == true)
            return Fail(400, "VALIDATION", "Check-in is for a clinic visit.");
        if (IsCancelled(appt.Status) || IsCompleted(appt.Status))
            return Fail(400, "VALIDATION", "This visit can no longer be checked in.");
        if (appt.AppointmentDate?.Date != DateTime.Today)
            return Fail(400, "VALIDATION", "Check-in is only for today's visit.");
        var ahead = await _context.PatientAppointments.CountAsync(a =>
            a.DoctorId == appt.DoctorId
            && a.DeleteStatus != true
            && a.AppointmentDate.HasValue
            && a.AppointmentDate.Value.Date == DateTime.Today
            && a.PatientAppId != appt.PatientAppId
            && a.Status == "WAITING");
        appt.Status = next;
        appt.QueuePosition = ahead + 1;
        await _context.SaveChangesAsync();
        try
        {
            var mobile = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == appt.PatientId)
                .Select(p => p.MobileNo)
                .FirstOrDefaultAsync();
            var doctor = await DoctorLabelAsync(appt.DoctorId);
            var body = await _templates.RenderAsync("CHECK_IN", new Dictionary<string, string?>
            {
                ["doctor"] = doctor,
                ["status"] = appt.Status
            });
            if (!string.IsNullOrWhiteSpace(mobile))
                await _sms.SendAsync(mobile, body);
        }
        catch { /* check-in already saved */ }
        // readyAt / ETA not stored in clinic queue module yet (BE-007) — always null.
        return Ok(new
        {
            patientAppId,
            status = appt.Status,
            queuePosition = appt.QueuePosition,
            readyAt = (DateTime?)null,
            estimatedWaitMinutes = (int?)null
        });
    }

    public async Task<S4ActionResult> SwitchToTeleAsync(int patientAppId, S4Caller caller)
    {
        var appt = await _context.PatientAppointments
            .FirstOrDefaultAsync(a => a.PatientAppId == patientAppId && a.DeleteStatus != true);
        if (appt == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (!OwnsVisit(caller, appt))
            return Fail(403, "FORBIDDEN", "This visit belongs to another account.");
        if (IsCancelled(appt.Status) || IsCompleted(appt.Status))
            return Fail(400, "VALIDATION", "This visit can no longer change mode.");
        appt.IsTele = true;
        appt.ConsultMode = "Tele";
        appt.VisitType = "Tele";
        await _context.SaveChangesAsync();
        var teleSessionId = await EnsureTeleSessionForVisitAsync(appt.PatientAppId, appt.DoctorId, appt.PatientId);
        return Ok(new { patientAppId, consultMode = appt.ConsultMode, isTele = true, teleSessionId });
    }

    /// <summary>Creates a Waiting TeleSession when missing so patient Running Late / join can bind (BE-008).</summary>
    private async Task<int?> EnsureTeleSessionForVisitAsync(int patientAppId, int doctorId, int patientId)
    {
        if (doctorId <= 0) return null;
        var existing = await _context.Database.SqlQuery<SessionRow>($@"
            SELECT TOP 1 TeleSessionId, PatientAppId
            FROM dbo.TeleSession
            WHERE PatientAppId = {patientAppId} AND Status <> N'Ended'
            ORDER BY TeleSessionId DESC").FirstOrDefaultAsync();
        if (existing != null) return existing.TeleSessionId;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.TeleSession (PatientAppId, DoctorId, PatientId, RoomId, Status, RecordAllowed, CreatedAt)
            VALUES ({patientAppId}, {doctorId}, {patientId}, N'pending', N'Waiting', 0, {DateTime.Now})");
        var created = await _context.Database.SqlQuery<SessionRow>($@"
            SELECT TOP 1 TeleSessionId, PatientAppId
            FROM dbo.TeleSession
            WHERE PatientAppId = {patientAppId}
            ORDER BY TeleSessionId DESC").FirstOrDefaultAsync();
        return created?.TeleSessionId;
    }

    public async Task<S4ActionResult> RunningLateAsync(int teleSessionId, int? minutes, S4Caller caller)
    {
        var session = await _context.Database.SqlQuery<SessionRow>($@"
            SELECT TeleSessionId, PatientAppId FROM dbo.TeleSession WHERE TeleSessionId = {teleSessionId}").FirstOrDefaultAsync();
        if (session == null) return Fail(404, "NOT_FOUND", "Tele session not found.");
        var appt = await _context.PatientAppointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.PatientAppId == session.PatientAppId);
        if (appt == null || !OwnsVisit(caller, appt))
            return Fail(403, "FORBIDDEN", "This session belongs to another account.");
        var detail = minutes.HasValue && minutes.Value > 0 ? minutes.Value + " minutes" : null;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.TeleSessionEvent (TeleSessionId, Code, Detail, At)
            VALUES ({teleSessionId}, N'RUNNING_LATE', {detail}, {DateTime.Now})");
        return Ok(new { teleSessionId, code = "RUNNING_LATE", detail });
    }

    public async Task<S4ActionResult> GetPreferencesAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        var row = await _context.Database.SqlQuery<PreferenceRow>($@"
            SELECT PushEnabled, SmsEnabled, EmailEnabled, QuietStart, QuietEnd,
                   Appointments, Prescriptions, Medicines, Marketing, QuietHoursEnabled, ReminderOffsets
            FROM dbo.UserNotificationPreference
            WHERE UserId = {caller.UserId}").FirstOrDefaultAsync();
        return Ok(row ?? new PreferenceRow());
    }

    public async Task<S4ActionResult> SavePreferencesAsync(PreferenceWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        request ??= new PreferenceWrite();
        var quietStart = ParseQuietTime(request.QuietStart);
        var quietEnd = ParseQuietTime(request.QuietEnd);
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.UserNotificationPreference
            SET PushEnabled = {request.PushEnabled},
                SmsEnabled = {request.SmsEnabled},
                EmailEnabled = {request.EmailEnabled},
                QuietStart = {quietStart},
                QuietEnd = {quietEnd},
                Appointments = {request.Appointments},
                Prescriptions = {request.Prescriptions},
                Medicines = {request.Medicines},
                Marketing = {request.Marketing},
                QuietHoursEnabled = {request.QuietHoursEnabled},
                ReminderOffsets = {Trim(request.ReminderOffsets)}
            WHERE UserId = {caller.UserId}");
        if (updated == 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.UserNotificationPreference
                    (UserId, PushEnabled, SmsEnabled, EmailEnabled, QuietStart, QuietEnd,
                     Appointments, Prescriptions, Medicines, Marketing, QuietHoursEnabled, ReminderOffsets)
                VALUES ({caller.UserId}, {request.PushEnabled}, {request.SmsEnabled}, {request.EmailEnabled}, {quietStart}, {quietEnd},
                        {request.Appointments}, {request.Prescriptions}, {request.Medicines}, {request.Marketing}, {request.QuietHoursEnabled}, {Trim(request.ReminderOffsets)})");
        }
        return await GetPreferencesAsync(caller);
    }

    /// <summary>Accepts null, "", "22:00", or "22:00:00". Empty string must not 400 on TimeSpan bind.</summary>
    private static TimeSpan? ParseQuietTime(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var text = raw.Trim();
        if (TimeSpan.TryParse(text, out var span))
            return span;
        if (TimeOnly.TryParse(text, out var clock))
            return clock.ToTimeSpan();
        return null;
    }

    public async Task<S4ActionResult> MarkAllReadAsync(S4Caller caller)
    {
        var count = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppNotification SET IsRead = 1
            WHERE UserId = {caller.UserId} AND IsRead = 0");
        return Ok(new { marked = count });
    }

    public async Task<S4ActionResult> ListRemindersAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        var rows = await _context.Database.SqlQuery<ReminderRow>($@"
            SELECT PatientReminderId, PatientId, Title, DueAt, IsDone, Kind, TimeOfDay, Weekdays, MemberPatientId,
                   SnoozeMinutes, CAST(ISNULL(NotifyPush, 0) AS bit) AS NotifyPush
            FROM dbo.PatientReminder
            WHERE UserId = {caller.UserId}
            ORDER BY DueAt").ToListAsync();
        return Ok(rows.Select(r => new
        {
            r.PatientReminderId,
            r.PatientId,
            r.Title,
            r.DueAt,
            r.IsDone,
            r.Kind,
            r.TimeOfDay,
            r.Weekdays,
            r.MemberPatientId,
            snoozeMinutes = r.SnoozeMinutes,
            notifyPush = r.NotifyPush
        }));
    }

    public async Task<S4ActionResult> SaveReminderAsync(ReminderWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || request.DueAt == null)
            return Fail(400, "VALIDATION", "Title and dueAt are required.");
        if (request.SnoozeMinutes is < 0 or > 1440)
            return Fail(400, "VALIDATION", "snoozeMinutes must be from 0 to 1440.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.PatientReminder
                (UserId, PatientId, Title, DueAt, IsDone, CreatedAt, Kind, TimeOfDay, Weekdays, MemberPatientId, SnoozeMinutes, NotifyPush)
            VALUES ({caller.UserId}, {caller.PatientId.Value}, {request.Title.Trim()}, {request.DueAt.Value}, 0, {DateTime.Now},
                    {Trim(request.Kind)}, {Trim(request.TimeOfDay)}, {Trim(request.Weekdays)}, {request.MemberPatientId ?? caller.PatientId.Value},
                    {request.SnoozeMinutes}, {request.NotifyPush})");
        return await ListRemindersAsync(caller);
    }

    public async Task<S4ActionResult> UpdateReminderAsync(int id, ReminderWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || request.DueAt == null)
            return Fail(400, "VALIDATION", "Title and dueAt are required.");
        if (request.SnoozeMinutes is < 0 or > 1440)
            return Fail(400, "VALIDATION", "snoozeMinutes must be from 0 to 1440.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PatientReminder
            SET Title = {request.Title.Trim()},
                DueAt = {request.DueAt},
                IsDone = {request.IsDone},
                Kind = {Trim(request.Kind)},
                TimeOfDay = {Trim(request.TimeOfDay)},
                Weekdays = {Trim(request.Weekdays)},
                MemberPatientId = {request.MemberPatientId},
                SnoozeMinutes = {request.SnoozeMinutes},
                NotifyPush = {request.NotifyPush}
            WHERE PatientReminderId = {id} AND UserId = {caller.UserId}");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Reminder not found.");
        return await ListRemindersAsync(caller);
    }

    public async Task<S4ActionResult> ListDeliveryAddressesAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        var rows = await _context.Database.SqlQuery<DeliveryAddressRow>($@"
            SELECT DeliveryAddressId, Label, Line1, Line2, City, State, Pincode, Phone,
                   CAST(ISNULL(IsDefault, 0) AS bit) AS IsDefault
            FROM dbo.PatientDeliveryAddress
            WHERE PatientId = {caller.PatientId.Value}
            ORDER BY IsDefault DESC, DeliveryAddressId DESC").ToListAsync();
        return Ok(rows.Select(r => new
        {
            addressId = r.DeliveryAddressId,
            label = r.Label,
            line1 = r.Line1,
            line2 = r.Line2,
            city = r.City,
            state = r.State,
            pincode = r.Pincode,
            phone = r.Phone,
            isDefault = r.IsDefault
        }));
    }

    public async Task<S4ActionResult> SaveDeliveryAddressAsync(int? id, DeliveryAddressWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        if (request == null || string.IsNullOrWhiteSpace(request.Line1) || string.IsNullOrWhiteSpace(request.Pincode))
            return Fail(400, "VALIDATION", "line1 and pincode are required.");
        var pin = request.Pincode.Trim();
        // B-023 — pincode must be exactly 6 digits.
        if (pin.Length != 6 || !pin.All(char.IsDigit))
            return Fail(400, "VALIDATION", "pincode must be 6 digits.");
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        var patientId = caller.PatientId.Value;
        if (request.IsDefault)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientDeliveryAddress SET IsDefault = 0 WHERE PatientId = {patientId}");
        }
        if (id == null)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.PatientDeliveryAddress
                    (PatientId, Label, Line1, Line2, City, State, Pincode, Phone, IsDefault, CreatedAt)
                VALUES ({patientId}, {Trim(request.Label)}, {request.Line1.Trim()}, {Trim(request.Line2)},
                        {Trim(request.City)}, {Trim(request.State)}, {pin}, {phone}, {request.IsDefault}, {DateTime.Now})");
        }
        else
        {
            var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.PatientDeliveryAddress
                SET Label = {Trim(request.Label)}, Line1 = {request.Line1.Trim()}, Line2 = {Trim(request.Line2)},
                    City = {Trim(request.City)}, State = {Trim(request.State)}, Pincode = {pin},
                    Phone = {phone}, IsDefault = {request.IsDefault}
                WHERE DeliveryAddressId = {id.Value} AND PatientId = {patientId}");
            if (updated == 0) return Fail(404, "NOT_FOUND", "Address not found.");
        }
        return await ListDeliveryAddressesAsync(caller);
    }

    public async Task<S4ActionResult> DeleteDeliveryAddressAsync(int id, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM dbo.PatientDeliveryAddress
            WHERE DeliveryAddressId = {id} AND PatientId = {caller.PatientId.Value}");
        if (deleted == 0) return Fail(404, "NOT_FOUND", "Address not found.");
        return Ok(new { deleted = true, addressId = id });
    }

    public async Task<S4ActionResult> AcceptWaitlistOfferAsync(int bookingWaitlistId, S4Caller caller)
    {
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        if (bookingWaitlistId <= 0)
            return Fail(400, "VALIDATION", "Waitlist offer id is required.");
        var mobile = await _context.Patients.AsNoTracking()
            .Where(p => p.PatientId == caller.PatientId.Value)
            .Select(p => p.MobileNo)
            .FirstOrDefaultAsync();
        mobile = string.IsNullOrWhiteSpace(mobile) ? null : mobile.Trim();
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.BookingWaitlist
            SET Status = N'ACCEPTED'
            WHERE BookingWaitlistId = {bookingWaitlistId}
              AND Status = N'OFFERED'
              AND (PatientId = {caller.PatientId.Value}
                   OR ({mobile} IS NOT NULL AND ContactMobile = {mobile}))");
        if (updated == 0)
            return Fail(404, "NOT_FOUND", "No open offer matches this account.");
        return Ok(new { success = true, data = new { bookingWaitlistId, status = "ACCEPTED" } });
    }

    public async Task<S4ActionResult> DeleteReminderAsync(int id, S4Caller caller)
    {
        await EnsureSchemaAsync();
        var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM dbo.PatientReminder WHERE PatientReminderId = {id} AND UserId = {caller.UserId}");
        if (deleted == 0) return Fail(404, "NOT_FOUND", "Reminder not found.");
        return Ok(new { deleted = true });
    }

    public async Task<S4ActionResult> ListTimeOffAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.Database.SqlQuery<TimeOffRow>($@"
            SELECT DoctorTimeOffId, StartsAt, EndsAt, Reason
            FROM dbo.DoctorTimeOff
            WHERE DoctorId = {caller.DoctorId.Value}
            ORDER BY StartsAt").ToListAsync();
        return Ok(rows);
    }

    public async Task<S4ActionResult> SaveTimeOffAsync(TimeOffWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (request?.StartsAt == null || request.EndsAt == null || request.EndsAt <= request.StartsAt)
            return Fail(400, "VALIDATION", "startsAt and a later endsAt are required.");
        var doctorId = caller.DoctorId.Value;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.DoctorTimeOff (DoctorId, StartsAt, EndsAt, Reason)
            VALUES ({doctorId}, {request.StartsAt.Value}, {request.EndsAt.Value}, {Trim(request.Reason)})");

        // Active leave covering now → default Offline + hide from public directory (clinic closed).
        // Doctor may manually turn Online / Public Visibility back on for tele consultation.
        var now = DateTime.Now;
        if (request.StartsAt.Value <= now && request.EndsAt.Value >= now)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.Doctor
                SET IsOnline = 0, DirectoryVisible = 0, ChangedDate = {DateTime.UtcNow}
                WHERE DoctorId = {doctorId} AND DeleteStatus = 0");
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    IF OBJECT_ID(N'dbo.TeleAvailability', N'U') IS NOT NULL
                    UPDATE dbo.TeleAvailability SET IsOnline = 0, LastHeartbeat = {now}
                    WHERE DoctorId = {doctorId}");
            }
            catch
            {
                /* TeleAvailability is optional on some environments */
            }
        }

        return await ListTimeOffAsync(caller);
    }

    public async Task<S4ActionResult> DeleteTimeOffAsync(int id, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var deleted = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM dbo.DoctorTimeOff WHERE DoctorTimeOffId = {id} AND DoctorId = {caller.DoctorId.Value}");
        if (deleted == 0) return Fail(404, "NOT_FOUND", "Time off not found.");
        return Ok(new { deleted = true });
    }

    public async Task<S4ActionResult> ListAppointmentRequestsAsync(S4Caller caller, string? status)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.Database.SqlQuery<AppointmentRequestRow>($@"
            SELECT r.AppointmentRequestId, r.DoctorId, r.PatientId, pt.PatientName, r.RequestedDate, r.RequestedTime,
                   r.ConsultMode, r.Status, r.Reason, r.ProposedDate, r.ProposedTime, r.PatientAppId, r.CreatedAt
            FROM dbo.AppointmentRequest r
            INNER JOIN dbo.Patient pt ON pt.PatientId = r.PatientId
            WHERE r.DoctorId = {caller.DoctorId.Value}
            ORDER BY r.CreatedAt DESC").ToListAsync();
        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(r => string.Equals(r.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        return Ok(rows);
    }

    public async Task<S4ActionResult> CountPendingAppointmentRequestsForDoctorAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var pending = await CountPendingAppointmentRequestsAsync(caller.DoctorId.Value);
        return Ok(new { pendingCount = pending });
    }

    public async Task<S4ActionResult> CreateAppointmentRequestAsync(AppointmentRequestWrite request, S4Caller caller, string? idempotencyKey = null)
    {
        await EnsureSchemaAsync();
        await EnsureAppointmentRequestIdempotencyAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        if (request == null || request.DoctorId <= 0 || request.RequestedDate == null || string.IsNullOrWhiteSpace(request.RequestedTime))
            return Fail(400, "VALIDATION", "doctorId, requestedDate, and requestedTime are required.");
        var doctor = await _context.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId && !d.DeleteStatus);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor not found.");

        var key = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim();
        if (key != null && key.Length > 80) key = key[..80];
        if (!string.IsNullOrEmpty(key))
        {
            var prior = await _context.Database.SqlQuery<AppointmentRequestRow>($@"
                SELECT TOP 1 r.AppointmentRequestId, r.DoctorId, r.PatientId, CAST(NULL AS nvarchar(200)) AS PatientName,
                       r.RequestedDate, r.RequestedTime, r.ConsultMode, r.Status, r.Reason,
                       r.ProposedDate, r.ProposedTime, r.PatientAppId, r.CreatedAt
                FROM dbo.AppointmentRequest r
                WHERE r.PatientId = {caller.PatientId.Value}
                  AND r.IdempotencyKey = {key}
                  AND r.CreatedAt >= DATEADD(hour, -24, {DateTime.Now})
                ORDER BY r.AppointmentRequestId DESC").FirstOrDefaultAsync();
            if (prior != null)
                return Ok(new { appointmentRequestId = prior.AppointmentRequestId, status = prior.Status, idempotent = true });
        }

        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.AppointmentRequest
                (DoctorId, PatientId, RequestedDate, RequestedTime, ConsultMode, Status, Reason, CreatedAt, IdempotencyKey)
            VALUES ({request.DoctorId}, {caller.PatientId.Value}, {request.RequestedDate.Value.Date}, {request.RequestedTime.Trim()},
                    {Trim(request.ConsultMode) ?? "InClinic"}, N'PENDING', {Trim(request.Reason)}, {DateTime.Now}, {key})");
        var created = await _context.Database.SqlQuery<AppointmentRequestRow>($@"
            SELECT TOP 1 r.AppointmentRequestId, r.DoctorId, r.PatientId, CAST(NULL AS nvarchar(200)) AS PatientName,
                   r.RequestedDate, r.RequestedTime, r.ConsultMode, r.Status, r.Reason,
                   r.ProposedDate, r.ProposedTime, r.PatientAppId, r.CreatedAt
            FROM dbo.AppointmentRequest r
            WHERE r.PatientId = {caller.PatientId.Value}
            ORDER BY r.AppointmentRequestId DESC").FirstAsync();
        try
        {
            var pname = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == caller.PatientId.Value)
                .Select(p => p.PatientName)
                .FirstOrDefaultAsync();
            created.PatientName = pname;
            created.DoctorId = request.DoctorId;
            await PublishAppointmentRequestAsync(request.DoctorId, "created", created.AppointmentRequestId, "PENDING", created);
        }
        catch { /* realtime best-effort */ }
        try
        {
            var mobile = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == caller.PatientId.Value)
                .Select(p => p.MobileNo)
                .FirstOrDefaultAsync();
            var body = await _templates.RenderAsync("APPT_REQUEST", new Dictionary<string, string?>
            {
                ["date"] = request.RequestedDate!.Value.ToString("dd MMM yyyy"),
                ["time"] = request.RequestedTime!.Trim()
            });
            if (!string.IsNullOrWhiteSpace(mobile))
                await _sms.SendAsync(mobile, body);
            var patientName = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == caller.PatientId.Value)
                .Select(p => p.PatientName)
                .FirstOrDefaultAsync();
            var patientLabel = string.IsNullOrWhiteSpace(patientName) ? "A patient" : patientName.Trim();
            await _clinicInbox.NotifyDoctorAndReceptionAsync(
                request.DoctorId,
                "Appointment request",
                $"{patientLabel} requested a visit on {request.RequestedDate!.Value:dd MMM yyyy} {request.RequestedTime!.Trim()}.",
                "appointment_request",
                "/doctor/appointment-requests",
                "/reception/appointments");
        }
        catch { /* request already saved */ }
        return Ok(new { appointmentRequestId = created.AppointmentRequestId, status = "PENDING" });
    }

    public async Task<S4ActionResult> ListPatientAppointmentRequestsAsync(
        S4Caller caller,
        string? status = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        var rows = await _context.Database.SqlQuery<PatientAppointmentRequestRow>($@"
            SELECT r.AppointmentRequestId, r.DoctorId,
                   LTRIM(RTRIM(CONCAT(N'Dr. ', d.FirstName, N' ', d.LastName))) AS DoctorName,
                   r.RequestedDate, r.RequestedTime, r.ConsultMode, r.Reason, r.Status,
                   r.ProposedDate, r.ProposedTime, CAST(NULL AS nvarchar(400)) AS DoctorMessage,
                   r.PatientAppId, r.CreatedAt, r.CreatedAt AS UpdatedAt
            FROM dbo.AppointmentRequest r
            LEFT JOIN dbo.Doctor d ON d.DoctorID = r.DoctorId
            WHERE r.PatientId = {caller.PatientId.Value}
            ORDER BY r.CreatedAt DESC").ToListAsync();
        if (!string.IsNullOrWhiteSpace(status))
            rows = rows.Where(r => string.Equals(r.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        object Map(PatientAppointmentRequestRow r) => new
        {
            appointmentRequestId = r.AppointmentRequestId,
            doctorId = r.DoctorId,
            doctorName = r.DoctorName,
            requestedDate = r.RequestedDate.ToString("yyyy-MM-dd"),
            requestedTime = r.RequestedTime,
            consultMode = r.ConsultMode,
            reason = r.Reason,
            status = r.Status,
            proposedDate = string.Equals(r.Status, "PROPOSED", StringComparison.OrdinalIgnoreCase)
                ? r.ProposedDate?.ToString("yyyy-MM-dd")
                : null,
            proposedTime = string.Equals(r.Status, "PROPOSED", StringComparison.OrdinalIgnoreCase)
                ? r.ProposedTime
                : null,
            doctorMessage = r.DoctorMessage,
            patientAppId = r.PatientAppId,
            createdAt = r.CreatedAt,
            updatedAt = r.UpdatedAt
        };

        // Paginate only when the client asks (B-005). Otherwise return the full array.
        if (pageNumber == null && pageSize == null)
            return Ok(rows.Select(Map));

        var page = pageNumber is null or < 1 ? 1 : pageNumber.Value;
        var size = pageSize is null or < 1 ? 20 : Math.Min(pageSize.Value, 50);
        var total = rows.Count;
        var data = rows.Skip((page - 1) * size).Take(size).Select(Map);
        return Ok(new { totalRecords = total, pageNumber = page, pageSize = size, data });
    }

    public async Task<S4ActionResult> CancelPatientAppointmentRequestAsync(int id, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        var row = await _context.Database.SqlQuery<AppointmentRequestRow>($@"
            SELECT AppointmentRequestId, DoctorId, PatientId, CAST(NULL AS nvarchar(200)) AS PatientName,
                   RequestedDate, RequestedTime, ConsultMode, Status, Reason, ProposedDate, ProposedTime, PatientAppId, CreatedAt
            FROM dbo.AppointmentRequest
            WHERE AppointmentRequestId = {id}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Request not found.");
        if (row.PatientId != caller.PatientId.Value) return Fail(403, "FORBIDDEN", "This request belongs to another patient.");
        if (!string.Equals(row.Status, "PENDING", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(row.Status, "PROPOSED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Only a pending or proposed request can be cancelled.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest SET Status = N'CANCELLED'
            WHERE AppointmentRequestId = {id}");
        try { await PublishAppointmentRequestAsync(row.DoctorId, "cancelled", id, "CANCELLED", row); }
        catch { /* realtime best-effort */ }
        return Ok(new { appointmentRequestId = id, status = "CANCELLED" });
    }

    public async Task<S4ActionResult> RespondPatientAppointmentRequestAsync(int id, string? action, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        // B-005 — ACCEPT / DECLINE aliases for ACCEPT_PROPOSAL / DECLINE_PROPOSAL.
        var act = (action ?? "").Trim().ToUpperInvariant();
        if (act is "ACCEPT" or "ACCEPT_PROPOSAL") act = "ACCEPT_PROPOSAL";
        else if (act is "DECLINE" or "DECLINE_PROPOSAL") act = "DECLINE_PROPOSAL";
        else
            return Fail(400, "VALIDATION", "action must be ACCEPT, DECLINE, ACCEPT_PROPOSAL, or DECLINE_PROPOSAL.");
        var row = await _context.Database.SqlQuery<PatientAppointmentRequestRow>($@"
            SELECT r.AppointmentRequestId, r.DoctorId,
                   LTRIM(RTRIM(CONCAT(N'Dr. ', d.FirstName, N' ', d.LastName))) AS DoctorName,
                   r.RequestedDate, r.RequestedTime, r.ConsultMode, r.Reason, r.Status,
                   r.ProposedDate, r.ProposedTime, CAST(NULL AS nvarchar(400)) AS DoctorMessage,
                   r.PatientAppId, r.CreatedAt, r.CreatedAt AS UpdatedAt
            FROM dbo.AppointmentRequest r
            LEFT JOIN dbo.Doctor d ON d.DoctorID = r.DoctorId
            WHERE r.AppointmentRequestId = {id} AND r.PatientId = {caller.PatientId.Value}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Request not found.");
        if (string.Equals(row.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(row.Status, "DECLINED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "This proposal was already answered.");
        if (!string.Equals(row.Status, "PROPOSED", StringComparison.OrdinalIgnoreCase))
            return Fail(400, "INVALID_STATE", "Only a proposed request can be answered.");
        if (act == "DECLINE_PROPOSAL")
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.AppointmentRequest SET Status = N'DECLINED'
                WHERE AppointmentRequestId = {id}");
            try
            {
                await _clinicInbox.NotifyDoctorAndReceptionAsync(
                    row.DoctorId,
                    "Appointment request declined",
                    "The patient declined the proposed time.",
                    "appointment_request",
                    "/doctor/appointment-requests",
                    "/reception/appointments");
            }
            catch { /* decline is saved */ }
            try { await PublishAppointmentRequestAsync(row.DoctorId, "patient_responded", id, "DECLINED"); }
            catch { /* realtime best-effort */ }
            return Ok(new { appointmentRequestId = id, status = "DECLINED", patientAppId = (int?)null });
        }

        if (row.ProposedDate == null || string.IsNullOrWhiteSpace(row.ProposedTime)
            || !TimeOnly.TryParse(row.ProposedTime, out var time))
            return Fail(400, "VALIDATION", "Proposed date/time is missing.");
        var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.DoctorId == row.DoctorId);
        if (doctor?.UserId == null) return Fail(400, "VALIDATION", "This doctor has no login user, so the visit cannot be booked.");
        var isTele = string.Equals(row.ConsultMode, "Tele", StringComparison.OrdinalIgnoreCase);
        var appt = new Master.PatientAppointment
        {
            PatientId = caller.PatientId.Value,
            DoctorId = row.DoctorId,
            UserId = doctor.UserId.Value,
            AppointmentDate = row.ProposedDate.Value.Date,
            AppointmentTime = time,
            Status = "NOT ARRIVED",
            DeleteStatus = false,
            VisitType = isTele ? "Tele" : "InClinic",
            ConsultMode = isTele ? "Tele" : "InClinic",
            IsTele = isTele,
            PaymentStatus = "UNPAID",
            BookingChannel = "Request"
        };
        _context.PatientAppointments.Add(appt);
        await _context.SaveChangesAsync();
        if (isTele)
            await EnsureTeleSessionForVisitAsync(appt.PatientAppId, row.DoctorId, caller.PatientId.Value);
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest
            SET Status = N'ACCEPTED', PatientAppId = {appt.PatientAppId},
                RequestedDate = {row.ProposedDate.Value.Date}, RequestedTime = {row.ProposedTime}
            WHERE AppointmentRequestId = {id}");
        try
        {
            var mobile = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == caller.PatientId.Value)
                .Select(p => p.MobileNo)
                .FirstOrDefaultAsync();
            await _notify.NotifyConfirmAsync(
                mobile,
                false,
                row.DoctorName ?? "your doctor",
                row.ProposedDate.Value.ToString("dd MMM yyyy"),
                NotificationClock.Format12Hour(time));
            var patientName = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == caller.PatientId.Value)
                .Select(p => p.PatientName)
                .FirstOrDefaultAsync();
            var patientLabel = string.IsNullOrWhiteSpace(patientName) ? "A patient" : patientName.Trim();
            await _clinicInbox.NotifyDoctorAndReceptionAsync(
                row.DoctorId,
                "New appointment",
                $"{patientLabel} booked for {row.ProposedDate.Value:dd MMM yyyy} {NotificationClock.Format12Hour(time)}.",
                "appointment_confirm",
                AppNotificationInbox.DoctorAppointmentLink(row.ProposedDate, appt.PatientAppId));
        }
        catch { /* booking already saved */ }
        try { await PublishAppointmentRequestAsync(row.DoctorId, "patient_responded", id, "ACCEPTED"); }
        catch { /* realtime best-effort */ }
        return Ok(new { appointmentRequestId = id, status = "ACCEPTED", patientAppId = appt.PatientAppId });
    }

    private async Task EnsureAppointmentRequestIdempotencyAsync()
    {
        await _context.Database.ExecuteSqlRawAsync("""
            IF COL_LENGTH(N'dbo.AppointmentRequest', N'IdempotencyKey') IS NULL
                ALTER TABLE dbo.AppointmentRequest ADD IdempotencyKey nvarchar(80) NULL;
            """);
    }

    public async Task<S4ActionResult> AcceptAppointmentRequestAsync(int id, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var row = await LoadRequestAsync(id, caller.DoctorId.Value);
        if (row == null) return Fail(404, "NOT_FOUND", "Request not found.");
        if (!string.Equals(row.Status, "PENDING", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(row.Status, "PROPOSED", StringComparison.OrdinalIgnoreCase))
            return Fail(400, "VALIDATION", "Only a pending or proposed request can be accepted.");
        var doctor = await _context.Doctors.AsNoTracking().FirstAsync(d => d.DoctorId == caller.DoctorId.Value);
        if (doctor.UserId == null) return Fail(400, "VALIDATION", "This doctor has no login user, so the visit cannot be booked.");
        if (!TimeOnly.TryParse(row.RequestedTime, out var time))
            return Fail(400, "VALIDATION", "Requested time is not a valid clock time.");
        var isTele = string.Equals(row.ConsultMode, "Tele", StringComparison.OrdinalIgnoreCase);
        var appt = new Master.PatientAppointment
        {
            PatientId = row.PatientId,
            DoctorId = caller.DoctorId.Value,
            UserId = doctor.UserId.Value,
            AppointmentDate = row.RequestedDate,
            AppointmentTime = time,
            Status = "NOT ARRIVED",
            DeleteStatus = false,
            VisitType = isTele ? "Tele" : "InClinic",
            ConsultMode = isTele ? "Tele" : "InClinic",
            IsTele = isTele,
            PaymentStatus = "UNPAID",
            BookingChannel = "Request"
        };
        _context.PatientAppointments.Add(appt);
        await _context.SaveChangesAsync();
        if (isTele)
            await EnsureTeleSessionForVisitAsync(appt.PatientAppId, caller.DoctorId.Value, row.PatientId);
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest
            SET Status = N'ACCEPTED', PatientAppId = {appt.PatientAppId}
            WHERE AppointmentRequestId = {id}");
        try
        {
            var mobile = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == row.PatientId)
                .Select(p => p.MobileNo)
                .FirstOrDefaultAsync();
            var doctorLabel = await DoctorLabelAsync(caller.DoctorId.Value);
            await _notify.NotifyConfirmAsync(
                mobile,
                false,
                doctorLabel,
                row.RequestedDate.ToString("dd MMM yyyy"),
                NotificationClock.Format12Hour(time));
            var patientName = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == row.PatientId)
                .Select(p => p.PatientName)
                .FirstOrDefaultAsync();
            var patientLabel = string.IsNullOrWhiteSpace(patientName) ? "A patient" : patientName.Trim();
            await _clinicInbox.NotifyDoctorAndReceptionAsync(
                caller.DoctorId.Value,
                "New appointment",
                $"{patientLabel} booked for {row.RequestedDate:dd MMM yyyy} {NotificationClock.Format12Hour(time)}.",
                "appointment_confirm",
                AppNotificationInbox.DoctorAppointmentLink(row.RequestedDate, appt.PatientAppId));
        }
        catch { /* booking already saved */ }
        try { await PublishAppointmentRequestAsync(caller.DoctorId.Value, "accepted", id, "ACCEPTED", row); }
        catch { /* realtime best-effort */ }
        return Ok(new { status = "ACCEPTED", patientAppId = appt.PatientAppId });
    }

    public async Task<S4ActionResult> DeclineAppointmentRequestAsync(int id, string? reason, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (string.IsNullOrWhiteSpace(reason)) return Fail(400, "VALIDATION", "A decline reason is required.");
        var pending = await LoadRequestAsync(id, caller.DoctorId.Value);
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest
            SET Status = N'DECLINED', Reason = {reason.Trim()}
            WHERE AppointmentRequestId = {id} AND DoctorId = {caller.DoctorId.Value} AND Status = N'PENDING'");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Pending request not found.");
        if (pending != null)
        {
            try
            {
                await _clinicInbox.NotifyPatientAsync(
                    pending.PatientId,
                    "Appointment request declined",
                    "The clinic declined your appointment request" + (string.IsNullOrWhiteSpace(reason) ? "." : ": " + reason.Trim()),
                    "appointment_request",
                    "/patient/appointment-requests");
            }
            catch { /* decline is saved */ }
        }
        try { await PublishAppointmentRequestAsync(caller.DoctorId.Value, "declined", id, "DECLINED"); }
        catch { /* realtime best-effort */ }
        return Ok(new { status = "DECLINED" });
    }

    public async Task<S4ActionResult> ProposeAppointmentRequestAsync(int id, DateTime? date, string? time, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (date == null || string.IsNullOrWhiteSpace(time))
            return Fail(400, "VALIDATION", "proposedDate and proposedTime are required.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest
            SET Status = N'PROPOSED', ProposedDate = {date.Value.Date}, ProposedTime = {time.Trim()}
            WHERE AppointmentRequestId = {id} AND DoctorId = {caller.DoctorId.Value}
              AND Status IN (N'PENDING', N'PROPOSED')");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Open request not found.");
        try
        {
            var req = await LoadRequestAsync(id, caller.DoctorId.Value);
            if (req != null)
            {
                var mobile = await _context.Patients.AsNoTracking()
                    .Where(p => p.PatientId == req.PatientId)
                    .Select(p => p.MobileNo)
                    .FirstOrDefaultAsync();
                var body = await _templates.RenderAsync("APPT_PROPOSED", new Dictionary<string, string?>
                {
                    ["date"] = date.Value.ToString("dd MMM yyyy"),
                    ["time"] = time.Trim()
                });
                if (!string.IsNullOrWhiteSpace(mobile))
                    await _sms.SendAsync(mobile, body);
                await _clinicInbox.NotifyPatientAsync(
                    req.PatientId,
                    "New appointment time proposed",
                    body,
                    "appointment_request",
                    "/patient/appointment-requests");
            }
        }
        catch { /* proposal already saved */ }
        try
        {
            var proposedRow = await LoadRequestAsync(id, caller.DoctorId.Value);
            await PublishAppointmentRequestAsync(caller.DoctorId.Value, "proposed", id, "PROPOSED", proposedRow);
        }
        catch { /* realtime best-effort */ }
        return Ok(new { status = "PROPOSED" });
    }


    private async Task<int> CountPendingAppointmentRequestsAsync(int doctorId)
    {
        var n = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value
            FROM dbo.AppointmentRequest
            WHERE DoctorId = {doctorId} AND Status IN (N'PENDING', N'PROPOSED')").FirstOrDefaultAsync();
        return n?.Value ?? 0;
    }

    private async Task PublishAppointmentRequestAsync(
        int doctorId,
        string eventType,
        int appointmentRequestId,
        string status,
        AppointmentRequestRow? row = null)
    {
        if (doctorId <= 0) return;
        var pending = await CountPendingAppointmentRequestsAsync(doctorId);
        await _realtime.AppointmentRequestChangedAsync(doctorId, new
        {
            eventType,
            appointmentRequestId,
            status,
            pendingCount = pending,
            patientId = row?.PatientId,
            patientName = row?.PatientName,
            requestedDate = row?.RequestedDate,
            requestedTime = row?.RequestedTime,
            consultMode = row?.ConsultMode,
            reason = row?.Reason,
            proposedDate = row?.ProposedDate,
            proposedTime = row?.ProposedTime,
            createdAt = row?.CreatedAt
        });
    }


    private async Task<string> DoctorLabelAsync(int doctorId)
    {
        var d = await _context.Doctors.AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .Select(x => new { x.FirstName, x.MiddleName, x.LastName })
            .FirstOrDefaultAsync();
        if (d == null) return "your doctor";
        var label = string.Join(" ", new[] { d.FirstName, d.MiddleName, d.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();
        return string.IsNullOrWhiteSpace(label) ? "your doctor" : label;
    }

    public async Task<S4ActionResult> ListCareLinksAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var id = caller.DoctorId.Value;
        var rows = await _context.Database.SqlQuery<CareLinkRow>($@"
            SELECT c.CareLinkRequestId, c.FromDoctorId, c.ToDoctorId, c.PatientId, pt.PatientName,
                   c.Note, c.FeeAmount, c.Opinion, c.Status, c.CreatedAt
            FROM dbo.CareLinkRequest c
            INNER JOIN dbo.Patient pt ON pt.PatientId = c.PatientId
            WHERE c.FromDoctorId = {id} OR c.ToDoctorId = {id}
            ORDER BY c.CreatedAt DESC").ToListAsync();
        return Ok(rows.Select(r => new
        {
            careLinkRequestId = r.CareLinkRequestId,
            fromDoctorId = r.FromDoctorId,
            toDoctorId = r.ToDoctorId,
            patientId = r.PatientId,
            patientName = r.PatientName,
            note = r.Note,
            feeAmount = r.FeeAmount,
            feeInr = r.FeeAmount,
            opinion = r.Opinion,
            expiry = r.CreatedAt.AddHours(CareLinkExpiryHours),
            status = r.Status,
            createdAt = r.CreatedAt
        }));
    }

    public async Task<S4ActionResult> CreateCareLinkAsync(CareLinkWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (request == null || request.ToDoctorId <= 0 || request.PatientId <= 0)
            return Fail(400, "VALIDATION", "toDoctorId and patientId are required.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.CareLinkRequest (FromDoctorId, ToDoctorId, PatientId, Note, FeeAmount, Status, CreatedAt)
            VALUES ({caller.DoctorId.Value}, {request.ToDoctorId}, {request.PatientId}, {Trim(request.Note)}, {request.FeeAmount}, N'OPEN', {DateTime.Now})");
        try
        {
            await _clinicInbox.NotifyDoctorUserAsync(
                request.ToDoctorId,
                "CareLink request",
                "Another doctor sent you a CareLink consult request.",
                "carelink",
                "/doctor/carelink");
        }
        catch { /* request already saved */ }
        return Ok(new { status = "OPEN" });
    }

    public async Task<S4ActionResult> RespondCareLinkAsync(int id, string? status, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var next = string.Equals(status, "ACCEPTED", StringComparison.OrdinalIgnoreCase) ? "ACCEPTED"
            : string.Equals(status, "DECLINED", StringComparison.OrdinalIgnoreCase) ? "DECLINED"
            : null;
        if (next == null) return Fail(400, "VALIDATION", "status must be ACCEPTED or DECLINED.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.CareLinkRequest
            SET Status = {next}
            WHERE CareLinkRequestId = {id} AND ToDoctorId = {caller.DoctorId.Value} AND Status = N'OPEN'");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Open care link not found.");
        try
        {
            var link = await _context.Database.SqlQuery<CareLinkRow>($@"
                SELECT c.CareLinkRequestId, c.FromDoctorId, c.ToDoctorId, c.PatientId, pt.PatientName,
                       c.Note, c.FeeAmount, c.Opinion, c.Status, c.CreatedAt
                FROM dbo.CareLinkRequest c
                INNER JOIN dbo.Patient pt ON pt.PatientId = c.PatientId
                WHERE c.CareLinkRequestId = {id}").FirstOrDefaultAsync();
            if (link != null)
            {
                await _clinicInbox.NotifyDoctorUserAsync(
                    link.FromDoctorId,
                    "CareLink " + next.ToLowerInvariant(),
                    "The receiving doctor " + next.ToLowerInvariant() + " your CareLink request.",
                    "carelink",
                    "/doctor/carelink");
            }
        }
        catch { /* response already saved */ }
        return Ok(new { status = next });
    }

    public async Task<S4ActionResult> SaveCareLinkOpinionAsync(int id, string? opinion, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (string.IsNullOrWhiteSpace(opinion)) return Fail(400, "VALIDATION", "opinion is required.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.CareLinkRequest
            SET Opinion = {opinion.Trim()}
            WHERE CareLinkRequestId = {id}
              AND (FromDoctorId = {caller.DoctorId.Value} OR ToDoctorId = {caller.DoctorId.Value})");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Care link not found.");
        return Ok(new { careLinkRequestId = id, opinion = opinion.Trim() });
    }

    public async Task<S4ActionResult> CareLinkPackageAsync(int id, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var row = await _context.Database.SqlQuery<CareLinkRow>($@"
            SELECT c.CareLinkRequestId, c.FromDoctorId, c.ToDoctorId, c.PatientId, pt.PatientName,
                   c.Note, c.FeeAmount, c.Opinion, c.Status, c.CreatedAt
            FROM dbo.CareLinkRequest c
            INNER JOIN dbo.Patient pt ON pt.PatientId = c.PatientId
            WHERE c.CareLinkRequestId = {id}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Care link not found.");
        var doctorId = caller.DoctorId.Value;
        var isSender = row.FromDoctorId == doctorId;
        var isRecipient = row.ToDoctorId == doctorId;
        if (!isSender && !isRecipient) return Fail(403, "FORBIDDEN", "This care link belongs to another clinic.");
        if (isRecipient && !string.Equals(row.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
            return Fail(403, "FORBIDDEN", "Accept the care link before opening the package.");

        var fromName = await DoctorDisplayNameAsync(row.FromDoctorId);
        return Ok(new
        {
            careLinkId = row.CareLinkRequestId,
            fromDoctorName = fromName,
            patientName = row.PatientName,
            patientId = row.PatientId,
            note = row.Note,
            feeInr = row.FeeAmount,
            status = row.Status,
            requestedOutput = "written_opinion",
            sections = Array.Empty<object>()
        });
    }

    public async Task<S4ActionResult> CareLinkInfoRequestAsync(int id, CareLinkInfoRequestWrite? request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var options = request?.Options?.Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                      ?? new List<string>();
        var note = Trim(request?.Note);
        if (options.Count == 0 && note == null)
            return Fail(400, "VALIDATION", "options or note is required.");
        var row = await _context.Database.SqlQuery<CareLinkRow>($@"
            SELECT c.CareLinkRequestId, c.FromDoctorId, c.ToDoctorId, c.PatientId, pt.PatientName,
                   c.Note, c.FeeAmount, c.Opinion, c.Status, c.CreatedAt
            FROM dbo.CareLinkRequest c
            INNER JOIN dbo.Patient pt ON pt.PatientId = c.PatientId
            WHERE c.CareLinkRequestId = {id}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Care link not found.");
        if (row.FromDoctorId != caller.DoctorId.Value && row.ToDoctorId != caller.DoctorId.Value)
            return Fail(403, "FORBIDDEN", "Only a care-link participant can ask for info.");
        var optionsJson = JsonSerializer.Serialize(options);
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.CareLinkInfoRequest (CareLinkRequestId, FromDoctorId, OptionsJson, Note, CreatedAt)
            VALUES ({id}, {caller.DoctorId.Value}, {optionsJson}, {note}, {DateTime.Now})");
        var created = await _context.Database.SqlQuery<IdRow>($@"
            SELECT TOP 1 CareLinkInfoRequestId AS Id
            FROM dbo.CareLinkInfoRequest
            WHERE CareLinkRequestId = {id}
            ORDER BY CareLinkInfoRequestId DESC").FirstAsync();
        return Ok(new { careLinkId = id, infoRequestId = created.Id });
    }

    public async Task<S4ActionResult> ListPharmacyQueriesAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.Database.SqlQuery<PharmacyQueryRow>($@"
            SELECT PharmacyQueryId, PatientId, Question, Reply, Status, CreatedAt
            FROM dbo.PharmacyQuery
            WHERE DoctorId = {caller.DoctorId.Value}
            ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(rows);
    }

    public async Task<S4ActionResult> CreatePharmacyQueryAsync(PharmacyQueryWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (request == null || request.PatientId <= 0 || string.IsNullOrWhiteSpace(request.Question))
            return Fail(400, "VALIDATION", "patientId and question are required.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.PharmacyQuery (DoctorId, PatientId, Question, Status, CreatedAt)
            VALUES ({caller.DoctorId.Value}, {request.PatientId}, {request.Question.Trim()}, N'OPEN', {DateTime.Now})");
        return Ok(new { status = "OPEN" });
    }

    public async Task<S4ActionResult> ReplyPharmacyQueryAsync(int id, string? reply, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (string.IsNullOrWhiteSpace(reply)) return Fail(400, "VALIDATION", "reply is required.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PharmacyQuery
            SET Reply = {reply.Trim()}, Status = N'REPLIED'
            WHERE PharmacyQueryId = {id} AND DoctorId = {caller.DoctorId.Value}");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Query not found.");
        return Ok(new { status = "REPLIED" });
    }

    private async Task<AppointmentRequestRow?> LoadRequestAsync(int id, int doctorId)
        => await _context.Database.SqlQuery<AppointmentRequestRow>($@"
            SELECT r.AppointmentRequestId, r.DoctorId, r.PatientId, pt.PatientName, r.RequestedDate, r.RequestedTime,
                   r.ConsultMode, r.Status, r.Reason, r.ProposedDate, r.ProposedTime, r.PatientAppId, r.CreatedAt
            FROM dbo.AppointmentRequest r
            INNER JOIN dbo.Patient pt ON pt.PatientId = r.PatientId
            WHERE r.AppointmentRequestId = {id} AND r.DoctorId = {doctorId}").FirstOrDefaultAsync();

    private async Task EnsureSchemaAsync()
    {
        if (Volatile.Read(ref _schemaReady) == 1) return;
        await _context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.UserNotificationPreference', N'U') IS NULL
            CREATE TABLE dbo.UserNotificationPreference (
                UserNotificationPreferenceId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                UserId bigint NOT NULL,
                PushEnabled bit NOT NULL CONSTRAINT DF_Unp_Push DEFAULT (1),
                SmsEnabled bit NOT NULL CONSTRAINT DF_Unp_Sms DEFAULT (0),
                EmailEnabled bit NOT NULL CONSTRAINT DF_Unp_Email DEFAULT (0),
                QuietStart time NULL,
                QuietEnd time NULL,
                Appointments bit NOT NULL CONSTRAINT DF_Unp_Appt DEFAULT (1),
                Prescriptions bit NOT NULL CONSTRAINT DF_Unp_Rx DEFAULT (1),
                Medicines bit NOT NULL CONSTRAINT DF_Unp_Med DEFAULT (1),
                Marketing bit NOT NULL CONSTRAINT DF_Unp_Mkt DEFAULT (0),
                QuietHoursEnabled bit NOT NULL CONSTRAINT DF_Unp_Quiet DEFAULT (0),
                ReminderOffsets nvarchar(100) NULL
            );
            IF OBJECT_ID(N'dbo.PatientReminder', N'U') IS NULL
            CREATE TABLE dbo.PatientReminder (
                PatientReminderId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                UserId bigint NOT NULL,
                PatientId int NOT NULL,
                Title nvarchar(200) NOT NULL,
                DueAt datetime NOT NULL,
                IsDone bit NOT NULL CONSTRAINT DF_Pr_Done DEFAULT (0),
                CreatedAt datetime NOT NULL
            );
            IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL
            CREATE TABLE dbo.DoctorTimeOff (
                DoctorTimeOffId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                DoctorId int NOT NULL,
                StartsAt datetime NOT NULL,
                EndsAt datetime NOT NULL,
                Reason nvarchar(300) NULL
            );
            IF OBJECT_ID(N'dbo.AppointmentRequest', N'U') IS NULL
            CREATE TABLE dbo.AppointmentRequest (
                AppointmentRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                DoctorId int NOT NULL,
                PatientId int NOT NULL,
                RequestedDate datetime NOT NULL,
                RequestedTime nvarchar(8) NOT NULL,
                ConsultMode nvarchar(20) NULL,
                Status nvarchar(20) NOT NULL,
                Reason nvarchar(500) NULL,
                ProposedDate datetime NULL,
                ProposedTime nvarchar(8) NULL,
                PatientAppId int NULL,
                CreatedAt datetime NOT NULL
            );
            IF OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL
            CREATE TABLE dbo.CareLinkRequest (
                CareLinkRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                FromDoctorId int NOT NULL,
                ToDoctorId int NOT NULL,
                PatientId int NOT NULL,
                Note nvarchar(500) NULL,
                FeeAmount decimal(18,2) NULL,
                Status nvarchar(20) NOT NULL,
                CreatedAt datetime NOT NULL
            );
            IF OBJECT_ID(N'dbo.PharmacyQuery', N'U') IS NULL
            CREATE TABLE dbo.PharmacyQuery (
                PharmacyQueryId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                DoctorId int NOT NULL,
                PatientId int NOT NULL,
                Question nvarchar(1000) NOT NULL,
                Reply nvarchar(1000) NULL,
                Status nvarchar(20) NOT NULL,
                CreatedAt datetime NOT NULL
            );
            IF OBJECT_ID(N'dbo.DoctorBookingRule', N'U') IS NULL
            CREATE TABLE dbo.DoctorBookingRule (
                DoctorId int NOT NULL PRIMARY KEY,
                GapMinutes int NULL,
                BookingHorizonDays int NULL
            );
            IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Appointments') IS NULL
                ALTER TABLE dbo.UserNotificationPreference ADD Appointments bit NOT NULL CONSTRAINT DF_Unp_Appt DEFAULT (1);
            IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Prescriptions') IS NULL
                ALTER TABLE dbo.UserNotificationPreference ADD Prescriptions bit NOT NULL CONSTRAINT DF_Unp_Rx DEFAULT (1);
            IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Medicines') IS NULL
                ALTER TABLE dbo.UserNotificationPreference ADD Medicines bit NOT NULL CONSTRAINT DF_Unp_Med DEFAULT (1);
            IF COL_LENGTH(N'dbo.UserNotificationPreference', N'Marketing') IS NULL
                ALTER TABLE dbo.UserNotificationPreference ADD Marketing bit NOT NULL CONSTRAINT DF_Unp_Mkt DEFAULT (0);
            IF COL_LENGTH(N'dbo.UserNotificationPreference', N'QuietHoursEnabled') IS NULL
                ALTER TABLE dbo.UserNotificationPreference ADD QuietHoursEnabled bit NOT NULL CONSTRAINT DF_Unp_Quiet DEFAULT (0);
            IF COL_LENGTH(N'dbo.UserNotificationPreference', N'ReminderOffsets') IS NULL
                ALTER TABLE dbo.UserNotificationPreference ADD ReminderOffsets nvarchar(100) NULL;
            IF COL_LENGTH(N'dbo.PatientReminder', N'Kind') IS NULL
                ALTER TABLE dbo.PatientReminder ADD Kind nvarchar(40) NULL;
            IF COL_LENGTH(N'dbo.PatientReminder', N'TimeOfDay') IS NULL
                ALTER TABLE dbo.PatientReminder ADD TimeOfDay nvarchar(8) NULL;
            IF COL_LENGTH(N'dbo.PatientReminder', N'Weekdays') IS NULL
                ALTER TABLE dbo.PatientReminder ADD Weekdays nvarchar(40) NULL;
            IF COL_LENGTH(N'dbo.PatientReminder', N'MemberPatientId') IS NULL
                ALTER TABLE dbo.PatientReminder ADD MemberPatientId int NULL;
            IF COL_LENGTH(N'dbo.PatientReminder', N'SnoozeMinutes') IS NULL
                ALTER TABLE dbo.PatientReminder ADD SnoozeMinutes int NULL;
            IF COL_LENGTH(N'dbo.PatientReminder', N'NotifyPush') IS NULL
                ALTER TABLE dbo.PatientReminder ADD NotifyPush bit NOT NULL CONSTRAINT DF_Pr_NotifyPush DEFAULT (0);
            IF OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL
            CREATE TABLE dbo.PatientDeliveryAddress (
                DeliveryAddressId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                PatientId int NOT NULL,
                Label nvarchar(80) NULL,
                Line1 nvarchar(200) NOT NULL,
                Line2 nvarchar(200) NULL,
                City nvarchar(80) NULL,
                State nvarchar(80) NULL,
                Pincode nvarchar(12) NOT NULL,
                Phone nvarchar(30) NULL,
                IsDefault bit NOT NULL CONSTRAINT DF_Pda_Default DEFAULT (0),
                CreatedAt datetime NOT NULL
            );
            IF COL_LENGTH(N'dbo.PatientDeliveryAddress', N'Phone') IS NULL
                ALTER TABLE dbo.PatientDeliveryAddress ADD Phone nvarchar(30) NULL;
            IF COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL
                ALTER TABLE dbo.CareLinkRequest ADD Opinion nvarchar(1000) NULL;
            IF OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NULL
            CREATE TABLE dbo.CareLinkInfoRequest (
                CareLinkInfoRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                CareLinkRequestId int NOT NULL,
                FromDoctorId int NOT NULL,
                OptionsJson nvarchar(1000) NULL,
                Note nvarchar(1000) NULL,
                CreatedAt datetime NOT NULL
            );
            IF COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL
                ALTER TABLE dbo.AppNotification ADD Type nvarchar(40) NULL;
            IF COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL
                ALTER TABLE dbo.AppNotification ADD DeepLink nvarchar(300) NULL;
            """);
        Volatile.Write(ref _schemaReady, 1);
    }

    private async Task<string?> DoctorDisplayNameAsync(int doctorId)
    {
        var d = await _context.Doctors.AsNoTracking()
            .Where(x => x.DoctorId == doctorId)
            .Select(x => new { x.FirstName, x.MiddleName, x.LastName })
            .FirstOrDefaultAsync();
        if (d == null) return null;
        var name = string.Join(" ", new[] { d.FirstName, d.MiddleName, d.LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
        if (string.IsNullOrWhiteSpace(name)) return null;
        return name.StartsWith("Dr", StringComparison.OrdinalIgnoreCase) ? name : "Dr. " + name;
    }

    private string? AbsoluteDoctorPhotoUrl(int doctorId, string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath) || doctorId <= 0) return null;
        var request = _http.HttpContext?.Request;
        if (request != null)
            return PublicApiUrl.For(request, $"/api/Profile/Photo/{doctorId}");
        var site = (_config["ConfigurationModel:SiteUrl"] ?? "").TrimEnd('/');
        return string.IsNullOrWhiteSpace(site) ? $"/api/Profile/Photo/{doctorId}" : $"{site}/api/Profile/Photo/{doctorId}";
    }

    private static S4ActionResult Ok(object data) => S4ActionResult.Ok(new { success = true, data });
    private static S4ActionResult Fail(int status, string code, string message)
        => new() { StatusCode = status, Body = new { success = false, code, message } };
    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool IsCancelled(string? status) => string.Equals(status, "CANCELLED", StringComparison.OrdinalIgnoreCase);
    private static bool IsCompleted(string? status)
        => status != null && (status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Closed", StringComparison.OrdinalIgnoreCase)
            || status.Equals("Done", StringComparison.OrdinalIgnoreCase));

    private static bool OwnsVisit(S4Caller caller, Master.PatientAppointment appt)
        => caller.IsAdmin
           || (caller.DoctorId.HasValue && caller.DoctorId.Value == appt.DoctorId)
           || appt.UserId == caller.UserId
           || (caller.IsPatient && caller.PatientId.HasValue && caller.PatientId.Value == appt.PatientId);

    private sealed class FollowUpListRow
    {
        public int FollowUpTaskId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public int PatientId { get; set; }
        public int PatientAppId { get; set; }
        public string? PatientName { get; set; }
    }

    private sealed class FollowUpOwnerRow
    {
        public int FollowUpTaskId { get; set; }
        public string Status { get; set; } = "";
        public int DoctorId { get; set; }
    }

    private sealed class SessionMapRow
    {
        public int PatientAppId { get; set; }
        public int TeleSessionId { get; set; }
    }

    private sealed class IdRow
    {
        public int Id { get; set; }
    }

    private sealed class ReviewStatRow
    {
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    private sealed class CountRow
    {
        public int Value { get; set; }
    }

    private sealed class SessionRow
    {
        public int TeleSessionId { get; set; }
        public int PatientAppId { get; set; }
    }

    public sealed class PreferenceRow
    {
        public bool PushEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; }
        public bool EmailEnabled { get; set; }
        public TimeSpan? QuietStart { get; set; }
        public TimeSpan? QuietEnd { get; set; }
        public bool Appointments { get; set; } = true;
        public bool Prescriptions { get; set; } = true;
        public bool Medicines { get; set; } = true;
        public bool Marketing { get; set; }
        public bool QuietHoursEnabled { get; set; }
        public string? ReminderOffsets { get; set; }
    }

    private sealed class ReminderRow
    {
        public int PatientReminderId { get; set; }
        public int PatientId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueAt { get; set; }
        public bool IsDone { get; set; }
        public string? Kind { get; set; }
        public string? TimeOfDay { get; set; }
        public string? Weekdays { get; set; }
        public int? MemberPatientId { get; set; }
        public int? SnoozeMinutes { get; set; }
        public bool NotifyPush { get; set; }
    }

    private sealed class DeliveryAddressRow
    {
        public int DeliveryAddressId { get; set; }
        public string? Label { get; set; }
        public string Line1 { get; set; } = "";
        public string? Line2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Pincode { get; set; } = "";
        public string? Phone { get; set; }
        public bool IsDefault { get; set; }
    }

    private sealed class TimeOffRow
    {
        public int DoctorTimeOffId { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class BookingRuleRow
    {
        public int? GapMinutes { get; set; }
        public int? BookingHorizonDays { get; set; }
    }

    private sealed class AppointmentRequestRow
    {
        public int AppointmentRequestId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public DateTime RequestedDate { get; set; }
        public string RequestedTime { get; set; } = "";
        public string? ConsultMode { get; set; }
        public string Status { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime? ProposedDate { get; set; }
        public string? ProposedTime { get; set; }
        public int? PatientAppId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class PatientAppointmentRequestRow
    {
        public int AppointmentRequestId { get; set; }
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public DateTime RequestedDate { get; set; }
        public string RequestedTime { get; set; } = "";
        public string? ConsultMode { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "";
        public DateTime? ProposedDate { get; set; }
        public string? ProposedTime { get; set; }
        public string? DoctorMessage { get; set; }
        public int? PatientAppId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class CareLinkRow
    {
        public int CareLinkRequestId { get; set; }
        public int FromDoctorId { get; set; }
        public int ToDoctorId { get; set; }
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? Note { get; set; }
        public decimal? FeeAmount { get; set; }
        public string? Opinion { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    private sealed class PharmacyQueryRow
    {
        public int PharmacyQueryId { get; set; }
        public int PatientId { get; set; }
        public string Question { get; set; } = "";
        public string? Reply { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}

/// <summary>Notification switches for the signed-in user.</summary>
public class PreferenceWrite
{
    /// <summary>Push notifications. Default true before the first save.</summary>
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    /// <summary>Quiet-hours start as "HH:mm" or "HH:mm:ss". Null or "" when unset.</summary>
    public string? QuietStart { get; set; }
    /// <summary>Quiet-hours end as "HH:mm" or "HH:mm:ss". Null or "" when unset.</summary>
    public string? QuietEnd { get; set; }
    public bool Appointments { get; set; } = true;
    public bool Prescriptions { get; set; } = true;
    public bool Medicines { get; set; } = true;
    public bool Marketing { get; set; }
    public bool QuietHoursEnabled { get; set; }
    /// <summary>Comma-separated minute offsets, for example 60,1440. Null when unset.</summary>
    public string? ReminderOffsets { get; set; }
}

/// <summary>A patient reminder. Title and dueAt are required.</summary>
public class ReminderWrite
{
    public string Title { get; set; } = "";
    public DateTime? DueAt { get; set; }
    public bool IsDone { get; set; }
    /// <summary>For example MEDICINE. Null when the app does not send a kind.</summary>
    public string? Kind { get; set; }
    /// <summary>Clock time HH:mm. Null when unset.</summary>
    public string? TimeOfDay { get; set; }
    /// <summary>Comma-separated weekdays, for example Mon,Wed. Null when unset.</summary>
    public string? Weekdays { get; set; }
    /// <summary>Family member this reminder is for. Null means the signed-in patient.</summary>
    public int? MemberPatientId { get; set; }
    /// <summary>Snooze offset in minutes (BE-024). Null when unset.</summary>
    public int? SnoozeMinutes { get; set; }
    /// <summary>When true, server may schedule a push at DueAt (BE-024).</summary>
    public bool NotifyPush { get; set; }
}

public class DeliveryAddressWrite
{
    public string? Label { get; set; }
    public string Line1 { get; set; } = "";
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string Pincode { get; set; } = "";
    /// <summary>Optional contact phone for delivery (B-023).</summary>
    public string? Phone { get; set; }
    public bool IsDefault { get; set; }
}

public class AppointmentRequestRespondBody
{
    public string? Action { get; set; }
}

public class TimeOffWrite
{
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public string? Reason { get; set; }
}

public class AppointmentRequestWrite
{
    public int DoctorId { get; set; }
    public DateTime? RequestedDate { get; set; }
    public string RequestedTime { get; set; } = "";
    public string? ConsultMode { get; set; }
    public string? Reason { get; set; }
}

public class CareLinkWrite
{
    public int ToDoctorId { get; set; }
    public int PatientId { get; set; }
    public string? Note { get; set; }
    public decimal? FeeAmount { get; set; }
}

public class CareLinkInfoRequestWrite
{
    public List<string>? Options { get; set; }
    public string? Note { get; set; }
}

public class FollowUpNoteBody
{
    public string? Note { get; set; }
}

public class PharmacyQueryWrite
{
    public int PatientId { get; set; }
    public string Question { get; set; } = "";
}

public class OpinionBody
{
    public string? Opinion { get; set; }
}

public class ReplyBody
{
    public string? Reply { get; set; }
}

public class ReasonBody
{
    public string? Reason { get; set; }
}

public class ProposeBody
{
    public DateTime? ProposedDate { get; set; }
    public string? ProposedTime { get; set; }
}

public class StatusBody
{
    public string? Status { get; set; }
}

public class CheckInBody
{
    public string? Status { get; set; }
}

public class SwitchTeleBody
{
    public int PatientAppId { get; set; }
}

public class LateBody
{
    public int? Minutes { get; set; }
}
