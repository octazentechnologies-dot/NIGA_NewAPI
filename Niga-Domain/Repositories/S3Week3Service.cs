using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;
using Niga_Domain.Master;

namespace Niga_Domain.Repositories
{
    /// <summary>
    /// S3 Week 3 HTTP behind the appointment service.
    /// No SMS, WhatsApp, Razorpay, or waitlist auto-offer.
    /// </summary>
    public class S3Week3Service : IS3Week3Service
    {
        private readonly NIGACentrumContext _context;
        private readonly IPatientAppointmentService _appointments;

        public S3Week3Service(NIGACentrumContext context, IPatientAppointmentService appointments)
        {
            _context = context;
            _appointments = appointments;
        }

        public async Task<S3ActionResult> JoinWaitlistAsync(JoinWaitlistRequest request)
        {
            if (request == null || request.DoctorId <= 0 || string.IsNullOrWhiteSpace(request.ContactMobile))
                return S3ActionResult.Fail(400, "DoctorId and ContactMobile are required.");

            var mode = S3AppointmentRules.NormalizeMode(request.ConsultMode);
            var day = request.RequestedDate.Date;
            var name = string.IsNullOrWhiteSpace(request.ContactName) ? "Patient" : request.ContactName.Trim();
            var mobile = request.ContactMobile.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.BookingWaitlist
                    (DoctorId, PatientId, RequestedDate, ConsultMode, ContactName, ContactMobile, Status, CreatedAt)
                VALUES
                    ({request.DoctorId}, {request.PatientId}, {day}, {mode}, {name}, {mobile}, N'JOINED', {now})");

            return S3ActionResult.Ok(new
            {
                success = true,
                message = "Joined the waitlist. The slot is not reserved and no offer is sent."
            });
        }

        public async Task<S3ActionResult> GetWaitlistAsync(int doctorId)
        {
            var rows = await _context.Database.SqlQuery<WaitlistRow>($@"
                SELECT BookingWaitlistId, DoctorId, PatientId, RequestedDate, ConsultMode, ContactName, ContactMobile, Status, CreatedAt
                FROM dbo.BookingWaitlist
                WHERE DoctorId = {doctorId} AND Status = N'JOINED'
                ORDER BY CreatedAt").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> GetReceptionProfileAsync(int receptionStaffId)
        {
            var staff = await _context.DoctorReceptionStaffs.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ReceptionStaffId == receptionStaffId && !s.DeleteStatus);
            if (staff == null)
                return S3ActionResult.Fail(404, "Reception profile not found.");

            return S3ActionResult.Ok(new
            {
                success = true,
                data = new
                {
                    staff.ReceptionStaffId,
                    staff.DoctorId,
                    staff.FullName,
                    staff.ContactNumber,
                    staff.EmailId,
                    staff.Address,
                    staff.IsActive
                }
            });
        }

        public async Task<S3ActionResult> UpdateReceptionProfileAsync(int receptionStaffId, ReceptionProfileUpdate request)
        {
            var staff = await _context.DoctorReceptionStaffs
                .FirstOrDefaultAsync(s => s.ReceptionStaffId == receptionStaffId && !s.DeleteStatus);
            if (staff == null)
                return S3ActionResult.Fail(404, "Reception profile not found.");
            if (request == null)
                return S3ActionResult.Fail(400, "Body required.");

            var name = $"{request.FirstName} {request.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
                staff.FullName = name;
            if (!string.IsNullOrWhiteSpace(request.MobileNo))
                staff.ContactNumber = request.MobileNo.Trim();
            if (request.EmailId != null)
                staff.EmailId = request.EmailId.Trim();
            staff.ChangedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return await GetReceptionProfileAsync(receptionStaffId);
        }

        public async Task<S3ActionResult> SaveCasePaperAsync(CasePaperRequest request, int doctorId, long userId)
        {
            if (request == null || request.PatientId <= 0 || string.IsNullOrWhiteSpace(request.ChiefComplaint))
                return S3ActionResult.Fail(400, "PatientId and ChiefComplaint are required.");

            var text = request.ChiefComplaint.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.ReceptionCasePaper
                    (PatientAppId, PatientId, DoctorId, CaseId, ChiefComplaint, CreatedByRole, CreatedByUserId, CreatedAt)
                VALUES
                    ({request.PatientAppId}, {request.PatientId}, {doctorId}, {request.CaseId}, {text}, N'Reception', {userId}, {now})");

            if (request.CaseId.HasValue && request.CaseId.Value > 0)
            {
                _context.CaseEntryChiefComplaints.Add(new CaseEntryChiefComplaint
                {
                    CaseId = request.CaseId,
                    ChiefComplaintName = text,
                    CreatedByRole = "Reception"
                });
                await _context.SaveChangesAsync();
            }

            return S3ActionResult.Ok(new { success = true, message = "Chief complaint saved for the treating doctor." });
        }

        public async Task<S3ActionResult> GetCasePapersAsync(int doctorId, int patientId)
        {
            var rows = await _context.Database.SqlQuery<CasePaperRow>($@"
                SELECT ReceptionCasePaperId, PatientAppId, PatientId, DoctorId, CaseId, ChiefComplaint, CreatedByRole, CreatedAt
                FROM dbo.ReceptionCasePaper
                WHERE DoctorId = {doctorId} AND PatientId = {patientId}
                ORDER BY CreatedAt DESC").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> SetTeleAvailabilityAsync(int doctorId, bool isOnline)
        {
            await ExpireHeartbeatsAsync();
            var now = DateTime.Now;
            var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleAvailability
                SET IsOnline = {isOnline}, LastHeartbeat = {now}
                WHERE DoctorId = {doctorId}");
            if (updated == 0)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.TeleAvailability (DoctorId, IsOnline, LastHeartbeat)
                    VALUES ({doctorId}, {isOnline}, {now})");
            }

            return await GetTeleAvailabilityAsync(doctorId);
        }

        public async Task<S3ActionResult> GetTeleAvailabilityAsync(int doctorId)
        {
            await ExpireHeartbeatsAsync();
            var row = await _context.Database.SqlQuery<TeleAvailRow>($@"
                SELECT DoctorId, IsOnline, LastHeartbeat
                FROM dbo.TeleAvailability
                WHERE DoctorId = {doctorId}").FirstOrDefaultAsync();
            return S3ActionResult.Ok(new
            {
                success = true,
                data = row ?? new TeleAvailRow { DoctorId = doctorId, IsOnline = false }
            });
        }

        public async Task<S3ActionResult> GetTeleQueueAsync(int doctorId)
        {
            var today = DateTime.Today;
            var rows = await _context.PatientAppointments.AsNoTracking()
                .Include(x => x.Patient)
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.DeleteStatus != true &&
                    x.Status == "E-CONSULT" &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == today)
                .OrderBy(x => x.AppointmentTime)
                .Select(x => new
                {
                    x.PatientAppId,
                    x.PatientId,
                    PatientName = x.Patient.PatientName,
                    x.AppointmentTime,
                    x.PaymentStatus,
                    x.ConsultMode,
                    WaitMinutes = x.AppointmentTime.HasValue
                        ? (int)(DateTime.Now.TimeOfDay - x.AppointmentTime.Value.ToTimeSpan()).TotalMinutes
                        : 0
                })
                .ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> CreateSessionAsync(int patientAppId, int doctorId)
        {
            var appt = await OwnedAppointment(patientAppId, doctorId);
            if (appt == null)
                return S3ActionResult.Fail(404, "Appointment not found for this doctor.");

            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.TeleSession
                    (PatientAppId, DoctorId, PatientId, RoomId, Status, RecordAllowed, CreatedAt)
                VALUES
                    ({appt.PatientAppId}, {appt.DoctorId}, {appt.PatientId}, N'pending', N'Waiting', 0, {now})");

            var id = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 TeleSessionId AS Id
                FROM dbo.TeleSession
                WHERE PatientAppId = {patientAppId}
                ORDER BY TeleSessionId DESC").FirstAsync();
            var room = id.Id.ToString();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET RoomId = {room} WHERE TeleSessionId = {id.Id}");
            await LogEventAsync(id.Id, "Created", "Waiting room opened");
            return S3ActionResult.Ok(new { success = true, teleSessionId = id.Id, roomId = room, status = "Waiting" });
        }

        public async Task<S3ActionResult> StartSessionAsync(int sessionId, int doctorId)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null || session.DoctorId != doctorId)
                return S3ActionResult.Fail(404, "Session not found.");
            if (session.Status == "Ended")
                return S3ActionResult.Fail(409, "Session has ended.");

            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET Status = N'Active', StartedAt = {now} WHERE TeleSessionId = {sessionId}");
            await LogEventAsync(sessionId, "Started", null);
            return S3ActionResult.Ok(new { success = true, status = "Active", teleSessionId = sessionId });
        }

        public async Task<S3ActionResult> EndSessionAsync(int sessionId, int doctorId)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null || session.DoctorId != doctorId)
                return S3ActionResult.Fail(404, "Session not found.");
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET Status = N'Ended', EndedAt = {now} WHERE TeleSessionId = {sessionId}");
            await LogEventAsync(sessionId, "Ended", null);
            return S3ActionResult.Ok(new { success = true, status = "Ended", teleSessionId = sessionId });
        }

        public async Task<S3ActionResult> IssueTokenAsync(int sessionId, S3Caller caller, bool rejoin)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");
            if (rejoin && !string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase))
                return S3ActionResult.Fail(409, "Rejoin is only available while the session is Active.");
            if (string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase))
                return S3ActionResult.Fail(409, "Session has ended.");

            return S3ActionResult.Ok(new
            {
                success = true,
                vendor = "stub",
                teleSessionId = session.TeleSessionId,
                roomId = session.RoomId,
                token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                expiresAt = DateTime.Now.AddMinutes(60),
                recordAllowed = session.RecordAllowed,
                status = session.Status
            });
        }

        public async Task<S3ActionResult> GetSessionAsync(int sessionId, S3Caller caller)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");
            return S3ActionResult.Ok(new { success = true, data = session });
        }

        public async Task<S3ActionResult> CaptureConsentAsync(TeleConsentRequest request, S3Caller caller)
        {
            var session = await LoadSessionAsync(request.TeleSessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var role = string.IsNullOrWhiteSpace(caller.Role) ? "Doctor" : caller.Role;
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.TeleConsentLog (TeleSessionId, PatientAppId, Accepted, ByRole, At)
                VALUES ({session.TeleSessionId}, {session.PatientAppId}, {request.Accepted}, {role}, {now})");

            var type = await _context.ConsentTypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Code == "TeleRecording" && t.IsActive);
            if (type != null)
            {
                _context.ConsentRecords.Add(new ConsentRecord
                {
                    ConsentTypeId = type.ConsentTypeId,
                    SubjectType = "Patient",
                    SubjectId = session.PatientId,
                    GrantedByUserId = caller.UserId,
                    GrantedAt = now,
                    WithdrawnAt = request.Accepted ? null : now,
                    Notes = "TeleSession " + session.TeleSessionId
                });
                await _context.SaveChangesAsync();
            }

            var otherAccepted = await _context.Database.SqlQuery<BitRow>($@"
                SELECT TOP 1 CAST(Accepted AS bit) AS Value
                FROM dbo.TeleConsentLog
                WHERE TeleSessionId = {session.TeleSessionId}
                  AND ByRole <> {role}
                ORDER BY TeleConsentLogId DESC").FirstOrDefaultAsync();
            var recordAllowed = request.Accepted && otherAccepted != null && otherAccepted.Value;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET RecordAllowed = {recordAllowed} WHERE TeleSessionId = {session.TeleSessionId}");

            return S3ActionResult.Ok(new { success = true, recordAllowed, accepted = request.Accepted });
        }

        public async Task<S3ActionResult> LogJoinFailureAsync(int sessionId, string? code, S3Caller caller)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var errorCode = string.IsNullOrWhiteSpace(code) ? "JOIN_FAILED" : code.Trim();
            await LogEventAsync(sessionId, errorCode, "Join failed");
            return S3ActionResult.Ok(new
            {
                success = false,
                code = errorCode,
                retry = true,
                rejoin = string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase),
                supportPath = "/doctor/support"
            });
        }

        public async Task<S3ActionResult> PostChatAsync(TeleChatRequest request, S3Caller caller)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Body is required.");
            var session = await LoadSessionAsync(request.SessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var role = string.IsNullOrWhiteSpace(caller.Role) ? "Doctor" : caller.Role;
            var body = request.Body.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.TeleChatMessage (SessionId, PatientAppId, SenderRole, Body, At)
                VALUES ({session.TeleSessionId}, {session.PatientAppId}, {role}, {body}, {now})");
            return S3ActionResult.Ok(new { success = true });
        }

        public async Task<S3ActionResult> ListChatAsync(int sessionId, S3Caller caller)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var rows = await _context.Database.SqlQuery<ChatRow>($@"
                SELECT TeleChatMessageId, SessionId, PatientAppId, SenderRole, Body, At
                FROM dbo.TeleChatMessage
                WHERE SessionId = {sessionId}
                ORDER BY At").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> SaveSummaryAsync(ConsultationSummaryRequest request, int doctorId)
        {
            if (request == null || request.PatientAppId <= 0 || string.IsNullOrWhiteSpace(request.Text))
                return S3ActionResult.Fail(400, "PatientAppId and Text are required.");
            var appt = await OwnedAppointment(request.PatientAppId, doctorId);
            if (appt == null)
                return S3ActionResult.Fail(404, "Appointment not found for this doctor.");

            var text = request.Text.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.ConsultationSummary (PatientAppId, DoctorId, Text, At)
                VALUES ({request.PatientAppId}, {doctorId}, {text}, {now})");
            return S3ActionResult.Ok(new { success = true, message = "Summary saved for the patient." });
        }

        public async Task<S3ActionResult> GetSummaryAsync(int patientAppId, S3Caller caller)
        {
            var appt = await _context.PatientAppointments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PatientAppId == patientAppId && x.DeleteStatus != true);
            if (appt == null)
                return S3ActionResult.Fail(404, "Appointment not found.");
            var treatingDoctor = string.Equals(caller.Role, "Doctor", StringComparison.OrdinalIgnoreCase)
                && caller.DoctorId == appt.DoctorId;
            if (!caller.IsAdmin && !treatingDoctor && !await PatientOwnsAsync(caller.UserId, appt.PatientId))
                return S3ActionResult.Fail(403, "Not allowed to read this summary.");

            var rows = await _context.Database.SqlQuery<SummaryRow>($@"
                SELECT ConsultationSummaryId, PatientAppId, DoctorId, Text, At
                FROM dbo.ConsultationSummary
                WHERE PatientAppId = {patientAppId}
                ORDER BY At DESC").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> RequestInstantAsync(InstantConsultRequestBody request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ContactMobile))
                return S3ActionResult.Fail(400, "ContactMobile is required.");

            await ExpireHeartbeatsAsync();
            var open = await _context.Database.SqlQuery<IdRow>($@"
                SELECT COUNT(1) AS Id FROM dbo.InstantConsultRequest WHERE Status = N'OPEN'").FirstAsync();
            var position = open.Id + 1;
            var name = string.IsNullOrWhiteSpace(request.ContactName) ? "Patient" : request.ContactName.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.InstantConsultRequest
                    (PatientId, ContactName, ContactMobile, Status, QueuePosition, CreatedAt)
                VALUES
                    ({request.PatientId}, {name}, {request.ContactMobile.Trim()}, N'OPEN', {position}, {now})");

            var created = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 InstantConsultRequestId AS Id
                FROM dbo.InstantConsultRequest
                ORDER BY InstantConsultRequestId DESC").FirstAsync();

            var online = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 DoctorId AS Id FROM dbo.TeleAvailability WHERE IsOnline = 1").FirstOrDefaultAsync();
            if (online == null)
            {
                return S3ActionResult.Ok(new
                {
                    success = true,
                    instantConsultRequestId = created.Id,
                    queuePosition = position,
                    status = "NO_DOCTOR",
                    message = "No doctor is online."
                });
            }

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.DoctorOffer (InstantConsultRequestId, DoctorId, Status, At)
                VALUES ({created.Id}, {online.Id}, N'OFFERED', {now})");
            return S3ActionResult.Ok(new
            {
                success = true,
                instantConsultRequestId = created.Id,
                queuePosition = position,
                status = "OFFERED",
                doctorId = online.Id
            });
        }

        public async Task<S3ActionResult> ListInstantOffersAsync(int doctorId)
        {
            var rows = await _context.Database.SqlQuery<OfferRow>($@"
                SELECT o.DoctorOfferId, o.InstantConsultRequestId, o.DoctorId, o.Status, r.ContactName, r.QueuePosition
                FROM dbo.DoctorOffer o
                INNER JOIN dbo.InstantConsultRequest r ON r.InstantConsultRequestId = o.InstantConsultRequestId
                WHERE o.DoctorId = {doctorId} AND o.Status = N'OFFERED'
                ORDER BY o.DoctorOfferId DESC").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> AcceptInstantAsync(int requestId, int doctorId)
        {
            var offer = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 DoctorOfferId AS Id
                FROM dbo.DoctorOffer
                WHERE InstantConsultRequestId = {requestId} AND DoctorId = {doctorId} AND Status = N'OFFERED'").FirstOrDefaultAsync();
            if (offer == null)
                return S3ActionResult.Fail(404, "No open offer for this doctor.");

            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.DoctorOffer SET Status = N'ACCEPTED', At = {now} WHERE DoctorOfferId = {offer.Id}");
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.InstantConsultRequest SET Status = N'ACCEPTED' WHERE InstantConsultRequestId = {requestId}");
            return S3ActionResult.Ok(new { success = true, instantConsultRequestId = requestId, status = "ACCEPTED" });
        }

        public async Task<S3ActionResult> CreateTicketAsync(SupportTicketCreate request, long userId, string role)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Subject and Body are required.");

            var category = NormalizeCategory(request.Category);
            var reporter = string.IsNullOrWhiteSpace(request.ReporterRole) ? role : request.ReporterRole.Trim();
            if (reporter.Equals("Doctor", StringComparison.OrdinalIgnoreCase) && !role.Equals("Doctor", StringComparison.OrdinalIgnoreCase) && !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                reporter = role;
            var now = DateTime.Now;
            var due = now.AddHours(24);
            var subject = request.Subject.Trim();
            var body = request.Body.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SupportTicket
                    (ReporterUserId, ReporterRole, Category, Subject, Body, Status, Priority, SlaDueAt, CreatedAt)
                VALUES
                    ({userId}, {reporter}, {category}, {subject}, {body}, N'OPEN', N'NORMAL', {due}, {now})");
            var id = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 SupportTicketId AS Id FROM dbo.SupportTicket WHERE ReporterUserId = {userId} ORDER BY SupportTicketId DESC").FirstAsync();
            return S3ActionResult.Ok(new { success = true, supportTicketId = id.Id });
        }

        public async Task<S3ActionResult> ListMyTicketsAsync(long userId)
        {
            var rows = await _context.Database.SqlQuery<TicketRow>($@"
                SELECT SupportTicketId, ReporterUserId, ReporterRole, Category, Subject, Status, Priority, AssigneeUserId, SlaDueAt, CreatedAt
                FROM dbo.SupportTicket
                WHERE ReporterUserId = {userId}
                ORDER BY SupportTicketId DESC").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> ListAdminTicketsAsync(string? status, string? priority)
        {
            var rows = await _context.Database.SqlQuery<TicketRow>($@"
                SELECT SupportTicketId, ReporterUserId, ReporterRole, Category, Subject, Status, Priority, AssigneeUserId, SlaDueAt, CreatedAt
                FROM dbo.SupportTicket
                ORDER BY SupportTicketId DESC").ToListAsync();
            if (!string.IsNullOrWhiteSpace(status))
                rows = rows.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrWhiteSpace(priority))
                rows = rows.Where(r => r.Priority.Equals(priority, StringComparison.OrdinalIgnoreCase)).ToList();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> UpdateTicketAsync(int ticketId, SupportTicketUpdate request)
        {
            if (request == null)
                return S3ActionResult.Fail(400, "Body required.");
            var exists = await _context.Database.SqlQuery<IdRow>($@"
                SELECT SupportTicketId AS Id FROM dbo.SupportTicket WHERE SupportTicketId = {ticketId}").FirstOrDefaultAsync();
            if (exists == null)
                return S3ActionResult.Fail(404, "Ticket not found.");

            var status = string.IsNullOrWhiteSpace(request.Status) ? "OPEN" : request.Status.Trim();
            var priority = string.IsNullOrWhiteSpace(request.Priority) ? "NORMAL" : request.Priority.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.SupportTicket
                SET Status = {status}, Priority = {priority}, AssigneeUserId = {request.AssigneeUserId}
                WHERE SupportTicketId = {ticketId}");
            return S3ActionResult.Ok(new { success = true, supportTicketId = ticketId });
        }

        public async Task<S3ActionResult> AddMessageAsync(int ticketId, SupportMessageCreate request, long userId, string role, bool isAdmin)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Body is required.");
            if (!await CanSeeTicketAsync(ticketId, userId, isAdmin))
                return S3ActionResult.Fail(403, "Not allowed on this ticket.");

            var now = DateTime.Now;
            var body = request.Body.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SupportTicketMessage (SupportTicketId, AuthorUserId, AuthorRole, Body, At)
                VALUES ({ticketId}, {userId}, {role}, {body}, {now})");

            if (!string.IsNullOrWhiteSpace(request.FileName))
            {
                var key = "support/" + ticketId + "/" + Guid.NewGuid().ToString("N");
                var file = request.FileName.Trim();
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.SupportTicketAttachment (SupportTicketId, FileName, StorageKey, At)
                    VALUES ({ticketId}, {file}, {key}, {now})");
            }

            return S3ActionResult.Ok(new { success = true });
        }

        public async Task<S3ActionResult> ListMessagesAsync(int ticketId, long userId, bool isAdmin)
        {
            if (!await CanSeeTicketAsync(ticketId, userId, isAdmin))
                return S3ActionResult.Fail(403, "Not allowed on this ticket.");
            var rows = await _context.Database.SqlQuery<MessageRow>($@"
                SELECT SupportTicketMessageId, SupportTicketId, AuthorUserId, AuthorRole, Body, At
                FROM dbo.SupportTicketMessage
                WHERE SupportTicketId = {ticketId}
                ORDER BY At").ToListAsync();
            var files = await _context.Database.SqlQuery<AttachmentRow>($@"
                SELECT SupportTicketAttachmentId, SupportTicketId, FileName, StorageKey, At
                FROM dbo.SupportTicketAttachment
                WHERE SupportTicketId = {ticketId}
                ORDER BY At").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows, attachments = files });
        }

        public async Task<S3ActionResult> ListHelpAsync(bool includeUnpublished)
        {
            var rows = await _context.Database.SqlQuery<HelpRow>($@"
                SELECT HelpArticleId, Title, Slug, Body, IsPublished
                FROM dbo.HelpArticle
                ORDER BY HelpArticleId").ToListAsync();
            if (!includeUnpublished)
                rows = rows.Where(r => r.IsPublished).ToList();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        public async Task<S3ActionResult> GetHelpAsync(string slug, bool includeUnpublished)
        {
            var row = await _context.Database.SqlQuery<HelpRow>($@"
                SELECT HelpArticleId, Title, Slug, Body, IsPublished
                FROM dbo.HelpArticle
                WHERE Slug = {slug}").FirstOrDefaultAsync();
            if (row == null || (!row.IsPublished && !includeUnpublished))
                return S3ActionResult.Fail(404, "Help article not found.");
            return S3ActionResult.Ok(new { success = true, data = row });
        }

        public async Task<S3ActionResult> SaveHelpAsync(HelpArticleWrite request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Slug))
                return S3ActionResult.Fail(400, "Title and Slug are required.");
            var title = request.Title.Trim();
            var slug = request.Slug.Trim().ToLowerInvariant();
            var body = request.Body ?? "";
            var now = DateTime.Now;
            var existing = await _context.Database.SqlQuery<IdRow>($@"
                SELECT HelpArticleId AS Id FROM dbo.HelpArticle WHERE Slug = {slug}").FirstOrDefaultAsync();
            if (existing == null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished, CreatedAt)
                    VALUES ({title}, {slug}, {body}, {request.IsPublished}, {now})");
            }
            else
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE dbo.HelpArticle SET Title = {title}, Body = {body}, IsPublished = {request.IsPublished}
                    WHERE HelpArticleId = {existing.Id}");
            }

            return await GetHelpAsync(slug, true);
        }

        public async Task<S3ActionResult> AssistedBookAsync(AssistedBookRequest request, long userId)
        {
            if (request == null || request.DoctorId <= 0 || request.PatientId <= 0)
                return S3ActionResult.Fail(400, "DoctorId and PatientId are required.");

            var doctorUserId = await _context.Doctors.AsNoTracking()
                .Where(d => d.DoctorId == request.DoctorId && d.DeleteStatus != true)
                .Select(d => d.UserId)
                .FirstOrDefaultAsync();
            if (doctorUserId == null || doctorUserId <= 0)
                return S3ActionResult.Fail(404, "Doctor not found.");

            var error = new ErrorResponseModel();
            var saved = _appointments.SavePatientApp(new PatientAppointmentModel
            {
                PatientId = request.PatientId,
                DoctorId = request.DoctorId,
                UserId = doctorUserId.Value,
                AppointmentDate = request.AppointmentDate.Date,
                AppointmentTime = request.AppointmentTime,
                Status = "WAITING",
                ConsultMode = request.ConsultMode,
                VisitType = request.ConsultMode,
                BookingChannel = "Assisted",
                DeleteStatus = false
            }, ref error);

            if (string.IsNullOrEmpty(saved))
                return S3ActionResult.Fail((int)(error.StatusCode == 0 ? System.Net.HttpStatusCode.BadRequest : error.StatusCode), error.Message ?? "Booking failed.");

            return S3ActionResult.Ok(new { success = true, message = saved, bookingChannel = "Assisted", bookedByUserId = userId });
        }

        public async Task<S3ActionResult> GetDoctorContextAsync(int patientAppId, S3Caller caller)
        {
            var appt = await _context.PatientAppointments.AsNoTracking()
                .Include(x => x.Patient)
                .FirstOrDefaultAsync(x => x.PatientAppId == patientAppId && x.DeleteStatus != true);
            if (appt == null)
                return S3ActionResult.Fail(404, "Appointment not found.");
            if (!caller.IsAdmin && caller.DoctorId != appt.DoctorId)
                return S3ActionResult.Fail(403, "Not allowed to read this patient.");

            var complaint = await _context.Database.SqlQuery<TextRow>($@"
                SELECT TOP 1 ChiefComplaint AS Text
                FROM dbo.ReceptionCasePaper
                WHERE PatientId = {appt.PatientId} AND DoctorId = {appt.DoctorId}
                ORDER BY ReceptionCasePaperId DESC").FirstOrDefaultAsync();
            if (complaint == null || string.IsNullOrWhiteSpace(complaint.Text))
            {
                complaint = await _context.CaseEntryChiefComplaints.AsNoTracking()
                    .Where(c => c.Case != null && c.Case.PatientId == appt.PatientId && c.Case.DoctorId == appt.DoctorId)
                    .OrderByDescending(c => c.CaseChiefComplaintId)
                    .Select(c => new TextRow { Text = c.ChiefComplaintName })
                    .FirstOrDefaultAsync();
            }

            var last = await _context.PatientAppointments.AsNoTracking()
                .Where(x => x.PatientId == appt.PatientId && x.PatientAppId != appt.PatientAppId && x.DeleteStatus != true)
                .OrderByDescending(x => x.AppointmentDate)
                .Select(x => x.AppointmentDate)
                .FirstOrDefaultAsync();

            var tele = await _context.Database.SqlQuery<TextRow>($@"
                SELECT TOP 1 Status AS Text
                FROM dbo.TeleSession
                WHERE PatientAppId = {patientAppId}
                ORDER BY TeleSessionId DESC").FirstOrDefaultAsync();

            var age = appt.Patient?.Age;
            if (age == null && appt.Patient?.DateOfBirth != null)
                age = (int)((DateTime.Today - appt.Patient.DateOfBirth.Value.Date).TotalDays / 365.25);

            return S3ActionResult.Ok(new
            {
                success = true,
                data = new
                {
                    appt.PatientAppId,
                    appt.PatientId,
                    name = appt.Patient?.PatientName,
                    age,
                    chiefComplaint = complaint?.Text,
                    lastVisit = last,
                    paymentStatus = appt.PaymentStatus,
                    consultMode = appt.ConsultMode,
                    teleStatus = tele?.Text
                }
            });
        }

        public Task<S3ActionResult> ListRefillsAsync()
            => Task.FromResult(S3ActionResult.Ok(new { success = true, data = Array.Empty<object>(), message = "No refill requests until prescriptions are in place." }));

        public Task<S3ActionResult> DecideRefillAsync(int refillId, bool approve, string? reason)
        {
            if (!approve && string.IsNullOrWhiteSpace(reason))
                return Task.FromResult(S3ActionResult.Fail(400, "Reject reason is required."));
            return Task.FromResult(S3ActionResult.Fail(404, "Refill request " + refillId + " was not found."));
        }

        private async Task ExpireHeartbeatsAsync()
        {
            var cutoff = DateTime.Now.AddMinutes(-2);
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleAvailability
                SET IsOnline = 0
                WHERE IsOnline = 1 AND (LastHeartbeat IS NULL OR LastHeartbeat < {cutoff})");
        }

        private async Task<PatientAppointment?> OwnedAppointment(int patientAppId, int doctorId)
            => await _context.PatientAppointments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PatientAppId == patientAppId && x.DoctorId == doctorId && x.DeleteStatus != true);

        private async Task<SessionRow?> LoadSessionAsync(int sessionId)
            => await _context.Database.SqlQuery<SessionRow>($@"
                SELECT TeleSessionId, PatientAppId, DoctorId, PatientId, RoomId, Status, RecordAllowed
                FROM dbo.TeleSession
                WHERE TeleSessionId = {sessionId}").FirstOrDefaultAsync();

        private async Task<bool> CanAccessSessionAsync(SessionRow session, S3Caller caller)
        {
            if (caller.IsAdmin)
                return true;
            // Reception carries the doctor's id, but the video room is the treating doctor and the patient.
            if (caller.DoctorId.HasValue
                && caller.DoctorId.Value == session.DoctorId
                && string.Equals(caller.Role, "Doctor", StringComparison.OrdinalIgnoreCase))
                return true;
            return await PatientOwnsAsync(caller.UserId, session.PatientId);
        }

        private Task<bool> PatientOwnsAsync(long userId, int patientId)
            => _context.PatientUserMaps.AsNoTracking().AnyAsync(m =>
                m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus);

        private async Task<bool> CanSeeTicketAsync(int ticketId, long userId, bool isAdmin)
        {
            if (isAdmin)
                return true;
            var row = await _context.Database.SqlQuery<LongIdRow>($@"
                SELECT ReporterUserId AS Id FROM dbo.SupportTicket WHERE SupportTicketId = {ticketId}").FirstOrDefaultAsync();
            return row != null && row.Id == userId;
        }

        private async Task LogEventAsync(int sessionId, string code, string? detail)
        {
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.TeleSessionEvent (TeleSessionId, Code, Detail, At)
                VALUES ({sessionId}, {code}, {detail}, {now})");
        }

        private static string NormalizeCategory(string? category)
        {
            var allowed = new[] { "billing", "booking", "technical", "clinical", "other" };
            var value = (category ?? "other").Trim().ToLowerInvariant();
            return allowed.Contains(value) ? value : "other";
        }

        private sealed class WaitlistRow
        {
            public int BookingWaitlistId { get; set; }
            public int DoctorId { get; set; }
            public int? PatientId { get; set; }
            public DateTime RequestedDate { get; set; }
            public string ConsultMode { get; set; } = "";
            public string ContactName { get; set; } = "";
            public string ContactMobile { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        private sealed class CasePaperRow
        {
            public int ReceptionCasePaperId { get; set; }
            public int? PatientAppId { get; set; }
            public int PatientId { get; set; }
            public int DoctorId { get; set; }
            public int? CaseId { get; set; }
            public string ChiefComplaint { get; set; } = "";
            public string CreatedByRole { get; set; } = "";
            public DateTime CreatedAt { get; set; }
        }

        private sealed class TeleAvailRow
        {
            public int DoctorId { get; set; }
            public bool IsOnline { get; set; }
            public DateTime? LastHeartbeat { get; set; }
        }

        private sealed class IdRow
        {
            public int Id { get; set; }
        }

        private sealed class LongIdRow
        {
            public long Id { get; set; }
        }

        private sealed class BitRow
        {
            public bool Value { get; set; }
        }

        private sealed class TextRow
        {
            public string? Text { get; set; }
        }

        private sealed class SessionRow
        {
            public int TeleSessionId { get; set; }
            public int PatientAppId { get; set; }
            public int DoctorId { get; set; }
            public int PatientId { get; set; }
            public string RoomId { get; set; } = "";
            public string Status { get; set; } = "";
            public bool RecordAllowed { get; set; }
        }

        private sealed class ChatRow
        {
            public int TeleChatMessageId { get; set; }
            public int SessionId { get; set; }
            public int PatientAppId { get; set; }
            public string SenderRole { get; set; } = "";
            public string Body { get; set; } = "";
            public DateTime At { get; set; }
        }

        private sealed class SummaryRow
        {
            public int ConsultationSummaryId { get; set; }
            public int PatientAppId { get; set; }
            public int DoctorId { get; set; }
            public string Text { get; set; } = "";
            public DateTime At { get; set; }
        }

        private sealed class OfferRow
        {
            public int DoctorOfferId { get; set; }
            public int InstantConsultRequestId { get; set; }
            public int DoctorId { get; set; }
            public string Status { get; set; } = "";
            public string ContactName { get; set; } = "";
            public int QueuePosition { get; set; }
        }

        private sealed class TicketRow
        {
            public int SupportTicketId { get; set; }
            public long ReporterUserId { get; set; }
            public string ReporterRole { get; set; } = "";
            public string Category { get; set; } = "";
            public string Subject { get; set; } = "";
            public string Status { get; set; } = "";
            public string Priority { get; set; } = "";
            public long? AssigneeUserId { get; set; }
            public DateTime? SlaDueAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private sealed class AttachmentRow
        {
            public int SupportTicketAttachmentId { get; set; }
            public int SupportTicketId { get; set; }
            public string FileName { get; set; } = "";
            public string StorageKey { get; set; } = "";
            public DateTime At { get; set; }
        }

        private sealed class MessageRow
        {
            public int SupportTicketMessageId { get; set; }
            public int SupportTicketId { get; set; }
            public long AuthorUserId { get; set; }
            public string AuthorRole { get; set; } = "";
            public string Body { get; set; } = "";
            public DateTime At { get; set; }
        }

        private sealed class HelpRow
        {
            public int HelpArticleId { get; set; }
            public string Title { get; set; } = "";
            public string Slug { get; set; } = "";
            public string Body { get; set; } = "";
            public bool IsPublished { get; set; }
        }
    }
}
