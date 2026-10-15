using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Repositories;

/// <summary>
/// Mobile clinic routes that read or write stored rows. Amounts come from doctor fees or payment orders.
/// Fields the database does not store are returned as null.
/// </summary>
public class MobileClinicQueryService
{
    private static int _schemaReady;
    private readonly NIGACentrumContext _context;

    public MobileClinicQueryService(NIGACentrumContext context) => _context = context;

    public async Task<S4ActionResult> DoctorFollowUpsAsync(S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.Database.SqlQuery<FollowUpListRow>($@"
            SELECT t.FollowUpTaskId, t.Title, t.DueDate, t.Status, p.PatientId, p.PatientAppId, pt.PatientName
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            INNER JOIN dbo.Patient pt ON pt.PatientId = p.PatientId
            WHERE p.DoctorId = {caller.DoctorId.Value}
            ORDER BY t.DueDate").ToListAsync();
        return Ok(rows);
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
            punctuality = (decimal?)null
        });
    }

    public async Task<S4ActionResult> AppointmentsByDateRangeAsync(DateTime from, DateTime to, S4Caller caller)
    {
        if (to.Date < from.Date) return Fail(400, "VALIDATION", "to must be on or after from.");
        if ((to.Date - from.Date).TotalDays > 62) return Fail(400, "VALIDATION", "Date range cannot exceed 62 days.");
        var end = to.Date.AddDays(1);
        var rows = await _context.PatientAppointments.AsNoTracking()
            .Where(a => a.UserId == caller.UserId
                        && a.DeleteStatus != true
                        && a.AppointmentDate.HasValue
                        && a.AppointmentDate.Value >= from.Date
                        && a.AppointmentDate.Value < end)
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
        return Ok(rows);
    }

    public async Task<S4ActionResult> CheckInAsync(int patientAppId, S4Caller caller)
    {
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Only the patient can check in.");
        var appt = await _context.PatientAppointments
            .FirstOrDefaultAsync(a => a.PatientAppId == patientAppId && a.DeleteStatus != true);
        if (appt == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (appt.PatientId != caller.PatientId) return Fail(403, "FORBIDDEN", "This visit belongs to another patient.");
        if (IsCancelled(appt.Status) || IsCompleted(appt.Status))
            return Fail(400, "VALIDATION", "This visit can no longer be checked in.");
        if (appt.AppointmentDate?.Date != DateTime.Today)
            return Fail(400, "VALIDATION", "Check-in is only for today's visit.");
        appt.Status = "WAITING";
        await _context.SaveChangesAsync();
        return Ok(new { patientAppId, status = appt.Status });
    }

    public async Task<S4ActionResult> SwitchToTeleAsync(int patientAppId, S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var appt = await _context.PatientAppointments
            .FirstOrDefaultAsync(a => a.PatientAppId == patientAppId && a.DeleteStatus != true);
        if (appt == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (appt.DoctorId != caller.DoctorId && appt.UserId != caller.UserId)
            return Fail(403, "FORBIDDEN", "This visit belongs to another doctor.");
        if (IsCancelled(appt.Status) || IsCompleted(appt.Status))
            return Fail(400, "VALIDATION", "This visit can no longer change mode.");
        appt.IsTele = true;
        appt.ConsultMode = "Tele";
        appt.VisitType = "Tele";
        await _context.SaveChangesAsync();
        return Ok(new { patientAppId, consultMode = appt.ConsultMode, isTele = true });
    }

    public async Task<S4ActionResult> RunningLateAsync(int teleSessionId, int? minutes, S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var session = await _context.Database.SqlQuery<SessionRow>($@"
            SELECT TeleSessionId, PatientAppId FROM dbo.TeleSession WHERE TeleSessionId = {teleSessionId}").FirstOrDefaultAsync();
        if (session == null) return Fail(404, "NOT_FOUND", "Tele session not found.");
        var appt = await _context.PatientAppointments.AsNoTracking()
            .FirstOrDefaultAsync(a => a.PatientAppId == session.PatientAppId);
        if (appt == null || (appt.DoctorId != caller.DoctorId && appt.UserId != caller.UserId))
            return Fail(403, "FORBIDDEN", "This session belongs to another doctor.");
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
            SELECT PushEnabled, SmsEnabled, EmailEnabled, QuietStart, QuietEnd
            FROM dbo.UserNotificationPreference
            WHERE UserId = {caller.UserId}").FirstOrDefaultAsync();
        return Ok(row ?? new PreferenceRow());
    }

    public async Task<S4ActionResult> SavePreferencesAsync(PreferenceWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        request ??= new PreferenceWrite();
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.UserNotificationPreference
            SET PushEnabled = {request.PushEnabled},
                SmsEnabled = {request.SmsEnabled},
                EmailEnabled = {request.EmailEnabled},
                QuietStart = {request.QuietStart},
                QuietEnd = {request.QuietEnd}
            WHERE UserId = {caller.UserId}");
        if (updated == 0)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.UserNotificationPreference (UserId, PushEnabled, SmsEnabled, EmailEnabled, QuietStart, QuietEnd)
                VALUES ({caller.UserId}, {request.PushEnabled}, {request.SmsEnabled}, {request.EmailEnabled}, {request.QuietStart}, {request.QuietEnd})");
        }
        return await GetPreferencesAsync(caller);
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
            SELECT PatientReminderId, PatientId, Title, DueAt, IsDone
            FROM dbo.PatientReminder
            WHERE UserId = {caller.UserId}
            ORDER BY DueAt").ToListAsync();
        return Ok(rows);
    }

    public async Task<S4ActionResult> SaveReminderAsync(ReminderWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        if (request == null || string.IsNullOrWhiteSpace(request.Title) || request.DueAt == null)
            return Fail(400, "VALIDATION", "Title and dueAt are required.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.PatientReminder (UserId, PatientId, Title, DueAt, IsDone, CreatedAt)
            VALUES ({caller.UserId}, {caller.PatientId.Value}, {request.Title.Trim()}, {request.DueAt.Value}, 0, {DateTime.Now})");
        return await ListRemindersAsync(caller);
    }

    public async Task<S4ActionResult> UpdateReminderAsync(int id, ReminderWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PatientReminder
            SET Title = {request.Title.Trim()},
                DueAt = {request.DueAt},
                IsDone = {request.IsDone}
            WHERE PatientReminderId = {id} AND UserId = {caller.UserId}");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Reminder not found.");
        return await ListRemindersAsync(caller);
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
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.DoctorTimeOff (DoctorId, StartsAt, EndsAt, Reason)
            VALUES ({caller.DoctorId.Value}, {request.StartsAt.Value}, {request.EndsAt.Value}, {Trim(request.Reason)})");
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

    public async Task<S4ActionResult> ListAppointmentRequestsAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var rows = await _context.Database.SqlQuery<AppointmentRequestRow>($@"
            SELECT r.AppointmentRequestId, r.PatientId, pt.PatientName, r.RequestedDate, r.RequestedTime,
                   r.ConsultMode, r.Status, r.Reason, r.ProposedDate, r.ProposedTime, r.PatientAppId, r.CreatedAt
            FROM dbo.AppointmentRequest r
            INNER JOIN dbo.Patient pt ON pt.PatientId = r.PatientId
            WHERE r.DoctorId = {caller.DoctorId.Value}
            ORDER BY r.CreatedAt DESC").ToListAsync();
        return Ok(rows);
    }

    public async Task<S4ActionResult> CreateAppointmentRequestAsync(AppointmentRequestWrite request, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.IsPatient || !caller.PatientId.HasValue)
            return Fail(403, "FORBIDDEN", "Patient context is required.");
        if (request == null || request.DoctorId <= 0 || request.RequestedDate == null || string.IsNullOrWhiteSpace(request.RequestedTime))
            return Fail(400, "VALIDATION", "doctorId, requestedDate, and requestedTime are required.");
        var doctor = await _context.Doctors.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DoctorId == request.DoctorId && !d.DeleteStatus);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor not found.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.AppointmentRequest
                (DoctorId, PatientId, RequestedDate, RequestedTime, ConsultMode, Status, Reason, CreatedAt)
            VALUES ({request.DoctorId}, {caller.PatientId.Value}, {request.RequestedDate.Value.Date}, {request.RequestedTime.Trim()},
                    {Trim(request.ConsultMode) ?? "InClinic"}, N'PENDING', {Trim(request.Reason)}, {DateTime.Now})");
        return Ok(new { status = "PENDING" });
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
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest
            SET Status = N'ACCEPTED', PatientAppId = {appt.PatientAppId}
            WHERE AppointmentRequestId = {id}");
        return Ok(new { status = "ACCEPTED", patientAppId = appt.PatientAppId });
    }

    public async Task<S4ActionResult> DeclineAppointmentRequestAsync(int id, string? reason, S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        if (string.IsNullOrWhiteSpace(reason)) return Fail(400, "VALIDATION", "A decline reason is required.");
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.AppointmentRequest
            SET Status = N'DECLINED', Reason = {reason.Trim()}
            WHERE AppointmentRequestId = {id} AND DoctorId = {caller.DoctorId.Value} AND Status = N'PENDING'");
        if (updated == 0) return Fail(404, "NOT_FOUND", "Pending request not found.");
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
        return Ok(new { status = "PROPOSED" });
    }

    public async Task<S4ActionResult> ListCareLinksAsync(S4Caller caller)
    {
        await EnsureSchemaAsync();
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var id = caller.DoctorId.Value;
        var rows = await _context.Database.SqlQuery<CareLinkRow>($@"
            SELECT c.CareLinkRequestId, c.FromDoctorId, c.ToDoctorId, c.PatientId, pt.PatientName,
                   c.Note, c.FeeAmount, c.Status, c.CreatedAt
            FROM dbo.CareLinkRequest c
            INNER JOIN dbo.Patient pt ON pt.PatientId = c.PatientId
            WHERE c.FromDoctorId = {id} OR c.ToDoctorId = {id}
            ORDER BY c.CreatedAt DESC").ToListAsync();
        return Ok(rows);
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
        return Ok(new { status = next });
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
            SELECT r.AppointmentRequestId, r.PatientId, pt.PatientName, r.RequestedDate, r.RequestedTime,
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
                QuietEnd time NULL
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
            """);
        Volatile.Write(ref _schemaReady, 1);
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
    }

    private sealed class ReminderRow
    {
        public int PatientReminderId { get; set; }
        public int PatientId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueAt { get; set; }
        public bool IsDone { get; set; }
    }

    private sealed class TimeOffRow
    {
        public int DoctorTimeOffId { get; set; }
        public DateTime StartsAt { get; set; }
        public DateTime EndsAt { get; set; }
        public string? Reason { get; set; }
    }

    private sealed class AppointmentRequestRow
    {
        public int AppointmentRequestId { get; set; }
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

    private sealed class CareLinkRow
    {
        public int CareLinkRequestId { get; set; }
        public int FromDoctorId { get; set; }
        public int ToDoctorId { get; set; }
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? Note { get; set; }
        public decimal? FeeAmount { get; set; }
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

public class PreferenceWrite
{
    public bool PushEnabled { get; set; } = true;
    public bool SmsEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public TimeSpan? QuietStart { get; set; }
    public TimeSpan? QuietEnd { get; set; }
}

public class ReminderWrite
{
    public string Title { get; set; } = "";
    public DateTime? DueAt { get; set; }
    public bool IsDone { get; set; }
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

public class PharmacyQueryWrite
{
    public int PatientId { get; set; }
    public string Question { get; set; } = "";
}
