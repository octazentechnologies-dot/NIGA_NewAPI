using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Services;
using Homeocentrum.Niga.NewAPI.Domain.Services.Tele;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories
{
    /// <summary>
    /// S3 Week 3 HTTP behind the appointment service.
    /// Tele Token/Rejoin uses <see cref="ITeleVideoVendor"/> (Stub until TeleVideo keys).
    /// Waiting room / device check / chat / summary stay poll-based (no SignalR).
    /// </summary>
    public class S3Week3Service : IS3Week3Service
    {
        private readonly NIGACentrumContext _context;
        private readonly IPatientAppointmentService _appointments;
        private readonly ITeleVideoVendor _teleVideo;
        private readonly IAppointmentRescheduleNotifier _notifier;
        private readonly ILogger<S3Week3Service> _logger;

        public S3Week3Service(
            NIGACentrumContext context,
            IPatientAppointmentService appointments,
            ITeleVideoVendor teleVideo,
            IAppointmentRescheduleNotifier notifier,
            ILogger<S3Week3Service> logger)
        {
            _context = context;
            _appointments = appointments;
            _teleVideo = teleVideo;
            _notifier = notifier;
            _logger = logger;
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

            var id = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 BookingWaitlistId AS Id
                FROM dbo.BookingWaitlist
                WHERE DoctorId = {request.DoctorId}
                  AND ContactMobile = {mobile}
                ORDER BY BookingWaitlistId DESC").FirstAsync();

            return S3ActionResult.Ok(new
            {
                success = true,
                bookingWaitlistId = id.Id,
                status = "JOINED",
                // PAT-23.02 — join does not reserve; offer arrives when a slot frees (cancel → OFFERED).
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

        /// <summary>
        /// PAT-23.02 — patient app polls waitlist offer status (JOINED / OFFERED).
        /// Offer is created when clinic cancels and frees a slot (APT-06.04); no auto-booking / no fake paid state.
        /// </summary>
        public async Task<S3ActionResult> GetWaitlistOffersAsync(string? contactMobile, int? doctorId, int? patientId)
        {
            var mobile = string.IsNullOrWhiteSpace(contactMobile) ? null : contactMobile.Trim();
            if (mobile == null && (patientId == null || patientId <= 0))
                return S3ActionResult.Fail(400, "contactMobile or patientId is required.");

            List<WaitlistRow> rows;
            if (mobile != null && doctorId is > 0)
            {
                rows = await _context.Database.SqlQuery<WaitlistRow>($@"
                    SELECT BookingWaitlistId, DoctorId, PatientId, RequestedDate, ConsultMode, ContactName, ContactMobile, Status, CreatedAt
                    FROM dbo.BookingWaitlist
                    WHERE ContactMobile = {mobile}
                      AND DoctorId = {doctorId.Value}
                      AND Status IN (N'JOINED', N'OFFERED')
                    ORDER BY CreatedAt DESC").ToListAsync();
            }
            else if (mobile != null)
            {
                rows = await _context.Database.SqlQuery<WaitlistRow>($@"
                    SELECT BookingWaitlistId, DoctorId, PatientId, RequestedDate, ConsultMode, ContactName, ContactMobile, Status, CreatedAt
                    FROM dbo.BookingWaitlist
                    WHERE ContactMobile = {mobile}
                      AND Status IN (N'JOINED', N'OFFERED')
                    ORDER BY CreatedAt DESC").ToListAsync();
            }
            else
            {
                rows = await _context.Database.SqlQuery<WaitlistRow>($@"
                    SELECT BookingWaitlistId, DoctorId, PatientId, RequestedDate, ConsultMode, ContactName, ContactMobile, Status, CreatedAt
                    FROM dbo.BookingWaitlist
                    WHERE PatientId = {patientId!.Value}
                      AND Status IN (N'JOINED', N'OFFERED')
                    ORDER BY CreatedAt DESC").ToListAsync();
            }

            var offered = rows.Where(r => string.Equals(r.Status, "OFFERED", StringComparison.OrdinalIgnoreCase)).ToList();
            return S3ActionResult.Ok(new
            {
                success = true,
                hasOffer = offered.Count > 0,
                data = rows
            });
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

        /// <summary>
        /// REC-12.02 — reception case paper is a subset of SaveComplaints:
        /// writes CaseEntryChiefComplaint with CreatedByRole=Reception for the clinic doctor.
        /// </summary>
        public async Task<S3ActionResult> SaveCasePaperAsync(CasePaperRequest request, int doctorId, long userId)
        {
            if (request == null || request.PatientId <= 0 || string.IsNullOrWhiteSpace(request.ChiefComplaint))
                return S3ActionResult.Fail(400, "PatientId and ChiefComplaint are required.");

            var parts = request.ChiefComplaint
                .Split([',', '\n', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => x.Length > 0)
                .ToList();
            if (parts.Count == 0)
                return S3ActionResult.Fail(400, "PatientId and ChiefComplaint are required.");

            CaseEntryDetail? caseEntry = null;
            if (request.CaseId is > 0)
            {
                caseEntry = await _context.CaseEntryDetails
                    .FirstOrDefaultAsync(c =>
                        c.CaseId == request.CaseId.Value &&
                        c.PatientId == request.PatientId &&
                        c.DoctorId == doctorId &&
                        c.DeleteStatus != true);
            }

            caseEntry ??= await _context.CaseEntryDetails
                .Where(c =>
                    c.PatientId == request.PatientId &&
                    c.DoctorId == doctorId &&
                    c.DeleteStatus != true)
                .OrderByDescending(c => c.CaseId)
                .FirstOrDefaultAsync();

            if (caseEntry == null)
                return S3ActionResult.Fail(404, "Case not found for this patient under the treating doctor.");

            var saved = new List<object>();
            foreach (var text in parts)
            {
                var row = new CaseEntryChiefComplaint
                {
                    CaseId = caseEntry.CaseId,
                    ChiefComplaintName = text,
                    CreatedByRole = "Reception"
                };
                _context.CaseEntryChiefComplaints.Add(row);
                await _context.SaveChangesAsync();
                saved.Add(new
                {
                    caseChiefComplaintId = row.CaseChiefComplaintId,
                    caseId = row.CaseId,
                    chiefComplaintName = row.ChiefComplaintName,
                    createdByRole = row.CreatedByRole
                });
            }

            return S3ActionResult.Ok(new
            {
                success = true,
                message = "Chief complaint saved for the treating doctor.",
                caseId = caseEntry.CaseId,
                patientId = request.PatientId,
                data = saved
            });
        }

        /// <summary>
        /// REC-12.02 — GET complaints for board / reception from CaseEntryChiefComplaint (same table as SaveComplaints).
        /// </summary>
        public async Task<S3ActionResult> GetCasePapersAsync(int doctorId, int patientId)
        {
            if (patientId <= 0)
                return S3ActionResult.Fail(400, "PatientId is required.");

            var caseIds = await _context.CaseEntryDetails.AsNoTracking()
                .Where(c => c.PatientId == patientId && c.DoctorId == doctorId && c.DeleteStatus != true)
                .Select(c => c.CaseId)
                .ToListAsync();

            if (caseIds.Count == 0)
                return S3ActionResult.Ok(new { success = true, data = Array.Empty<object>() });

            var rows = await _context.CaseEntryChiefComplaints.AsNoTracking()
                .Where(c => c.CaseId.HasValue && caseIds.Contains(c.CaseId.Value))
                .OrderByDescending(c => c.CaseChiefComplaintId)
                .Select(c => new
                {
                    caseChiefComplaintId = c.CaseChiefComplaintId,
                    caseId = c.CaseId,
                    patientId,
                    doctorId,
                    chiefComplaintName = c.ChiefComplaintName,
                    chiefComplaint = c.ChiefComplaintName,
                    createdByRole = c.CreatedByRole
                })
                .ToListAsync();

            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        /// <summary>REC-05.02 — a search row opens case paper or the appointment, never repertory.</summary>
        public async Task<S3ActionResult> OpenPatientRowAsync(int doctorId, int patientId)
        {
            if (patientId <= 0)
                return S3ActionResult.Fail(400, "PatientId is required.");

            var appointment = await _context.PatientAppointments
                .AsNoTracking()
                .Where(row => row.DoctorId == doctorId && row.PatientId == patientId && row.DeleteStatus != true)
                .OrderByDescending(row => row.AppointmentDate)
                .ThenByDescending(row => row.PatientAppId)
                .Select(row => new { row.PatientAppId })
                .FirstOrDefaultAsync();

            if (appointment != null)
            {
                return S3ActionResult.Ok(new
                {
                    success = true,
                    destination = "appointment",
                    patientId,
                    patientAppId = appointment.PatientAppId,
                    path = $"/doctordashboard?openAppointment={appointment.PatientAppId}",
                    repertory = false
                });
            }

            return S3ActionResult.Ok(new
            {
                success = true,
                destination = "casePaper",
                patientId,
                patientAppId = (int?)null,
                path = $"/reception/case-paper?patientId={patientId}",
                repertory = false
            });
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

        /// <summary>
        /// TEL-02.02 / TEL-02.04 — today's E-CONSULT queue with payment + wait/join time.
        /// Mirrors dbo.vw_TeleWaitingQueue from TEL-02.01 (DB). Client poll; not SignalR in S3.
        /// </summary>
        public async Task<S3ActionResult> GetTeleQueueAsync(int doctorId)
        {
            var today = DateTime.Today;
            var now = DateTime.Now;
            var rows = await _context.PatientAppointments.AsNoTracking()
                .Include(x => x.Patient)
                .Where(x =>
                    x.DoctorId == doctorId &&
                    x.DeleteStatus != true &&
                    x.Status == "E-CONSULT" &&
                    x.AppointmentDate.HasValue &&
                    x.AppointmentDate.Value.Date == today)
                .OrderBy(x => x.AppointmentTime)
                .ThenBy(x => x.PatientAppId)
                .ToListAsync();

            var data = rows.Select(x =>
            {
                var joinAt = x.CalledAt
                    ?? (x.AppointmentDate.HasValue && x.AppointmentTime.HasValue
                        ? x.AppointmentDate.Value.Date.Add(x.AppointmentTime.Value.ToTimeSpan())
                        : (DateTime?)null);
                var waitMinutes = joinAt.HasValue
                    ? (int)Math.Floor((now - joinAt.Value).TotalMinutes)
                    : 0;
                return new
                {
                    x.PatientAppId,
                    x.PatientId,
                    PatientName = x.Patient?.PatientName,
                    x.AppointmentDate,
                    x.AppointmentTime,
                    JoinTime = joinAt,
                    WaitMinutes = waitMinutes,
                    PaymentStatus = string.IsNullOrWhiteSpace(x.PaymentStatus) ? "UNPAID" : x.PaymentStatus,
                    x.ConsultMode,
                    x.Status
                };
            }).ToList();

            // polledAt lets UI/mobile show freshness when they interval-poll (TEL-02.04).
            return S3ActionResult.Ok(new { success = true, polledAt = now, data });
        }

        /// <summary>TEL-02.02 — create Waiting TeleSession for an owned appointment.</summary>
        public async Task<S3ActionResult> CreateSessionAsync(int patientAppId, int doctorId)
        {
            if (patientAppId <= 0)
                return S3ActionResult.Fail(400, "PatientAppId is required.");

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
            var room = "room-" + id.Id.ToString();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET RoomId = {room} WHERE TeleSessionId = {id.Id}");
            await LogEventAsync(id.Id, "Created", "Waiting room opened");
            return S3ActionResult.Ok(new { success = true, teleSessionId = id.Id, roomId = room, status = "Waiting", patientAppId });
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

            // Best-effort SMS/WhatsApp — patient still polls GetSession / queue (no SignalR).
            try
            {
                var patient = await _context.Patients.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PatientId == session.PatientId);
                if (patient != null && !string.IsNullOrWhiteSpace(patient.MobileNo))
                {
                    await _notifier.NotifyTeleReadyAsync(
                        patient.MobileNo,
                        patient.IsWhatsAppOptIn == true,
                        session.RoomId ?? ("room-" + sessionId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tele ready notice failed. Session {SessionId} is Active.", sessionId);
            }

            return S3ActionResult.Ok(new { success = true, status = "Active", teleSessionId = sessionId });
        }

        /// <summary>TEL-03.02 — end session for owning doctor.</summary>
        public async Task<S3ActionResult> EndSessionAsync(int sessionId, int doctorId)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null || session.DoctorId != doctorId)
                return S3ActionResult.Fail(404, "Session not found.");
            if (string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase))
                return S3ActionResult.Fail(409, "Session has already ended.");

            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET Status = N'Ended', EndedAt = {now} WHERE TeleSessionId = {sessionId}");
            await LogEventAsync(sessionId, "Ended", null);
            return S3ActionResult.Ok(new { success = true, status = "Ended", teleSessionId = sessionId });
        }

        /// <summary>
        /// TEL-03.02 / TEL-04.01 — vendor token (Stub until TeleVideo keys). Same payload for web and mobile.
        /// Device check / waiting room / rejoin / join-failure remain orchestration APIs; media plane is vendor SDK.
        /// </summary>
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

            var role = string.Equals(caller.Role, "Doctor", StringComparison.OrdinalIgnoreCase) ? "doctor" : "patient";
            var issued = await _teleVideo.IssueTokenAsync(new TeleVideoIssueRequest
            {
                AppointmentId = Guid.Empty,
                RoomId = session.RoomId ?? ("room-" + session.TeleSessionId),
                Role = role,
                UserAccountId = caller.UserId,
                TtlMinutes = 60
            });

            var vendorLower = (issued.Vendor ?? "Stub").ToLowerInvariant();
            await LogEventAsync(
                sessionId,
                rejoin ? "RejoinToken" : "Token",
                "vendor=" + vendorLower + ";clients=web,mobile");

            return S3ActionResult.Ok(new
            {
                success = true,
                vendor = vendorLower,
                isStub = vendorLower == "stub"
                    || (issued.ClientConfig != null
                        && issued.ClientConfig.TryGetValue("ready", out var ready)
                        && ready == "false"),
                // TEL-04.01 — one contract for Patient App + Doctor App + Doctor Web
                clients = new[] { "web", "mobile" },
                teleSessionId = session.TeleSessionId,
                roomId = issued.RoomId,
                token = issued.Token,
                expiresAt = issued.ExpiresAtUtc,
                clientConfig = issued.ClientConfig,
                recordAllowed = session.RecordAllowed,
                status = session.Status
            });
        }

        /// <summary>TEL-06.02 — GET session status for patient (or doctor/admin) waiting-room poll.</summary>
        public async Task<S3ActionResult> GetSessionAsync(int sessionId, S3Caller caller)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");
            return S3ActionResult.Ok(new { success = true, data = session });
        }

        /// <summary>
        /// TEL-07.02 — capture recording consent; set TeleSession.RecordAllowed only when both sides accept.
        /// Decline still writes TeleConsentLog + ConsentRecord (audit); RecordAllowed stays/clears false.
        /// </summary>
        public async Task<S3ActionResult> CaptureConsentAsync(TeleConsentRequest request, S3Caller caller)
        {
            if (request == null || request.TeleSessionId <= 0)
                return S3ActionResult.Fail(400, "TeleSessionId is required.");

            var session = await LoadSessionAsync(request.TeleSessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");
            if (string.Equals(session.Status, "Ended", StringComparison.OrdinalIgnoreCase))
                return S3ActionResult.Fail(409, "Session has ended.");

            var role = string.IsNullOrWhiteSpace(caller.Role) ? "Doctor" : caller.Role.Trim();
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

            // Both sides must accept (latest Doctor + latest non-Doctor). Decline → RecordAllowed = 0.
            var doctorAccepted = await _context.Database.SqlQuery<BitRow>($@"
                SELECT TOP 1 CAST(Accepted AS bit) AS Value
                FROM dbo.TeleConsentLog
                WHERE TeleSessionId = {session.TeleSessionId}
                  AND ByRole = N'Doctor'
                ORDER BY TeleConsentLogId DESC").FirstOrDefaultAsync();
            var patientAccepted = await _context.Database.SqlQuery<BitRow>($@"
                SELECT TOP 1 CAST(Accepted AS bit) AS Value
                FROM dbo.TeleConsentLog
                WHERE TeleSessionId = {session.TeleSessionId}
                  AND ByRole <> N'Doctor'
                ORDER BY TeleConsentLogId DESC").FirstOrDefaultAsync();
            var recordAllowed = doctorAccepted != null && doctorAccepted.Value
                && patientAccepted != null && patientAccepted.Value;

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.TeleSession SET RecordAllowed = {recordAllowed} WHERE TeleSessionId = {session.TeleSessionId}");
            await LogEventAsync(session.TeleSessionId, request.Accepted ? "ConsentAccepted" : "ConsentDeclined",
                "by=" + role + ";recordAllowed=" + (recordAllowed ? "1" : "0"));

            return S3ActionResult.Ok(new
            {
                success = true,
                teleSessionId = session.TeleSessionId,
                accepted = request.Accepted,
                byRole = role,
                recordAllowed
            });
        }

        /// <summary>
        /// TEL-09.01 / PAT-30.02 — standardise join error codes; log TeleSessionEvent for fallback UI
        /// (retry / rejoin / support). Patient and doctor share the same endpoint.
        /// </summary>
        public async Task<S3ActionResult> LogJoinFailureAsync(int sessionId, string? code, S3Caller caller)
        {
            if (sessionId <= 0)
                return S3ActionResult.Fail(400, "SessionId is required.");

            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessSessionAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var canonical = TeleJoinFailureCodes.Normalize(code, out var raw);
            var detail = raw == null || string.Equals(raw, canonical, StringComparison.OrdinalIgnoreCase)
                ? "Join failed"
                : "Join failed; raw=" + raw;
            await LogEventAsync(sessionId, canonical, detail);

            var active = string.Equals(session.Status, "Active", StringComparison.OrdinalIgnoreCase);
            var retry = TeleJoinFailureCodes.SuggestRetry(canonical);
            var isPatient = string.Equals(caller.Role, "Patient", StringComparison.OrdinalIgnoreCase);
            return S3ActionResult.Ok(new
            {
                success = false,
                code = canonical,
                message = TeleJoinFailureCodes.Message(canonical),
                retry,
                rejoin = active && retry,
                // PAT-30.02 — patient app uses patient support; clinic uses doctor support
                supportPath = isPatient ? "/support" : "/doctor/support",
                allowedCodes = TeleJoinFailureCodes.All
            });
        }

        /// <summary>TEL-10.02 — post chat message (doctor or mapped patient only).</summary>
        public async Task<S3ActionResult> PostChatAsync(TeleChatRequest request, S3Caller caller)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Body is required.");
            var session = await LoadSessionAsync(request.SessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessChatAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var role = string.IsNullOrWhiteSpace(caller.Role) ? "Doctor" : caller.Role;
            var body = request.Body.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.TeleChatMessage (SessionId, PatientAppId, SenderRole, Body, At)
                VALUES ({session.TeleSessionId}, {session.PatientAppId}, {role}, {body}, {now})");
            return S3ActionResult.Ok(new { success = true });
        }

        /// <summary>TEL-10.02 — list chat for session (doctor or mapped patient only).</summary>
        public async Task<S3ActionResult> ListChatAsync(int sessionId, S3Caller caller)
        {
            var session = await LoadSessionAsync(sessionId);
            if (session == null)
                return S3ActionResult.Fail(404, "Session not found.");
            if (!await CanAccessChatAsync(session, caller))
                return S3ActionResult.Fail(403, "Not allowed on this session.");

            var rows = await _context.Database.SqlQuery<ChatRow>($@"
                SELECT TeleChatMessageId, SessionId, PatientAppId, SenderRole, Body, At
                FROM dbo.TeleChatMessage
                WHERE SessionId = {sessionId}
                ORDER BY At").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        /// <summary>TEL-11.02 — treating doctor PUT consultation summary for PatientAppId.</summary>
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

        /// <summary>TEL-11.02 — patient GET (also treating doctor/admin) consultation summaries for an appointment.</summary>
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

        /// <summary>
        /// TEL-12.02 — patient requests instant consult: queue position, match online doctor, create offer.
        /// No online doctor → status NO_DOCTOR (no push). Match → DoctorOffer OFFERED.
        /// </summary>
        public async Task<S3ActionResult> RequestInstantAsync(InstantConsultRequestBody request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ContactMobile))
                return S3ActionResult.Fail(400, "ContactMobile is required.");

            await ExpireHeartbeatsAsync();
            var open = await _context.Database.SqlQuery<IdRow>($@"
                SELECT COUNT(1) AS Id
                FROM dbo.InstantConsultRequest
                WHERE Status IN (N'OPEN', N'NO_DOCTOR', N'OFFERED')").FirstAsync();
            var position = open.Id + 1;
            var name = string.IsNullOrWhiteSpace(request.ContactName) ? "Patient" : request.ContactName.Trim();
            var mobile = request.ContactMobile.Trim();
            var now = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.InstantConsultRequest
                    (PatientId, ContactName, ContactMobile, Status, QueuePosition, CreatedAt)
                VALUES
                    ({request.PatientId}, {name}, {mobile}, N'OPEN', {position}, {now})");

            var created = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 InstantConsultRequestId AS Id
                FROM dbo.InstantConsultRequest
                ORDER BY InstantConsultRequestId DESC").FirstAsync();

            // Match: most recently heartbeating online doctor (TeleAvailability).
            var online = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 DoctorId AS Id
                FROM dbo.TeleAvailability
                WHERE IsOnline = 1
                ORDER BY LastHeartbeat DESC").FirstOrDefaultAsync();
            if (online == null)
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE dbo.InstantConsultRequest
                    SET Status = N'NO_DOCTOR'
                    WHERE InstantConsultRequestId = {created.Id}");
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
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.InstantConsultRequest
                SET Status = N'OFFERED'
                WHERE InstantConsultRequestId = {created.Id}");
            return S3ActionResult.Ok(new
            {
                success = true,
                instantConsultRequestId = created.Id,
                queuePosition = position,
                status = "OFFERED",
                doctorId = online.Id
            });
        }

        /// <summary>TEL-12.02 — offers waiting for this doctor (Status=OFFERED).</summary>
        public async Task<S3ActionResult> ListInstantOffersAsync(int doctorId)
        {
            var rows = await _context.Database.SqlQuery<OfferRow>($@"
                SELECT o.DoctorOfferId, o.InstantConsultRequestId, o.DoctorId, o.Status, r.ContactName, r.QueuePosition
                FROM dbo.DoctorOffer o
                INNER JOIN dbo.InstantConsultRequest r ON r.InstantConsultRequestId = o.InstantConsultRequestId
                WHERE o.DoctorId = {doctorId} AND o.Status = N'OFFERED'
                ORDER BY r.QueuePosition, o.DoctorOfferId").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        /// <summary>TEL-12.02 — doctor accepts an OFFERED instant request.</summary>
        public async Task<S3ActionResult> AcceptInstantAsync(int requestId, int doctorId)
        {
            if (requestId <= 0)
                return S3ActionResult.Fail(400, "RequestId is required.");

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
            return S3ActionResult.Ok(new
            {
                success = true,
                instantConsultRequestId = requestId,
                doctorOfferId = offer.Id,
                status = "ACCEPTED"
            });
        }

        /// <summary>SUP-01.02 — create support ticket for the authenticated reporter (patient Mine flow).</summary>
        public async Task<S3ActionResult> CreateTicketAsync(SupportTicketCreate request, long userId, string role)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Subject and Body are required.");

            var category = NormalizeCategory(request.Category);
            var ticketType = category.Equals("AssistedRequest", StringComparison.OrdinalIgnoreCase)
                ? "AssistedRequest"
                : "General";
            var reporter = string.IsNullOrWhiteSpace(request.ReporterRole) ? role : request.ReporterRole.Trim();
            if (reporter.Equals("Doctor", StringComparison.OrdinalIgnoreCase) && !role.Equals("Doctor", StringComparison.OrdinalIgnoreCase) && !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                reporter = role;
            var now = DateTime.Now;
            var due = now.AddHours(24);
            var subject = request.Subject.Trim();
            var body = request.Body.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SupportTicket
                    (ReporterUserId, ReporterRole, Category, TicketType, Subject, Body, Status, Priority, SlaDueAt, CreatedAt)
                VALUES
                    ({userId}, {reporter}, {category}, {ticketType}, {subject}, {body}, N'OPEN', N'NORMAL', {due}, {now})");
            var id = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 SupportTicketId AS Id FROM dbo.SupportTicket WHERE ReporterUserId = {userId} ORDER BY SupportTicketId DESC").FirstAsync();
            return S3ActionResult.Ok(new { success = true, supportTicketId = id.Id });
        }

        /// <summary>SUP-01.02 — list tickets for the current user (GET /api/Support/Tickets/Mine).</summary>
        public async Task<S3ActionResult> ListMyTicketsAsync(long userId)
        {
            var rows = await _context.Database.SqlQuery<TicketRow>($@"
                SELECT SupportTicketId, ReporterUserId, ReporterRole, Category, Subject, Status, Priority, AssigneeUserId, SlaDueAt, CreatedAt
                FROM dbo.SupportTicket
                WHERE ReporterUserId = {userId}
                ORDER BY SupportTicketId DESC").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        /// <summary>
        /// SUP-03.02 — admin issue queue: list all tickets with optional status/priority filters.
        /// Returns priority, assignee, SLA for the admin portal.
        /// </summary>
        public async Task<S3ActionResult> ListAdminTicketsAsync(string? status, string? priority)
        {
            var rows = await _context.Database.SqlQuery<TicketRow>($@"
                SELECT SupportTicketId, ReporterUserId, ReporterRole, Category, Subject, Status, Priority, AssigneeUserId, SlaDueAt, CreatedAt
                FROM dbo.SupportTicket
                ORDER BY SupportTicketId DESC").ToListAsync();

            if (!string.IsNullOrWhiteSpace(status))
            {
                var want = NormalizeTicketStatus(status);
                rows = rows.Where(r => NormalizeTicketStatus(r.Status).Equals(want, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (!string.IsNullOrWhiteSpace(priority))
            {
                var want = NormalizeTicketPriority(priority);
                rows = rows.Where(r => NormalizeTicketPriority(r.Priority).Equals(want, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Queue order: urgent/high first, then soonest SLA, then newest.
            rows = rows
                .OrderBy(r => PriorityRank(r.Priority))
                .ThenBy(r => r.SlaDueAt ?? DateTime.MaxValue)
                .ThenByDescending(r => r.SupportTicketId)
                .ToList();

            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        /// <summary>
        /// SUP-03.02 — admin assign / set status / set priority on a ticket (partial update).
        /// </summary>
        public async Task<S3ActionResult> UpdateTicketAsync(int ticketId, SupportTicketUpdate request)
        {
            if (ticketId <= 0)
                return S3ActionResult.Fail(400, "TicketId is required.");
            if (request == null)
                return S3ActionResult.Fail(400, "Body required.");

            var current = await _context.Database.SqlQuery<TicketRow>($@"
                SELECT SupportTicketId, ReporterUserId, ReporterRole, Category, Subject, Status, Priority, AssigneeUserId, SlaDueAt, CreatedAt
                FROM dbo.SupportTicket
                WHERE SupportTicketId = {ticketId}").FirstOrDefaultAsync();
            if (current == null)
                return S3ActionResult.Fail(404, "Ticket not found.");

            var status = string.IsNullOrWhiteSpace(request.Status)
                ? NormalizeTicketStatus(current.Status)
                : NormalizeTicketStatus(request.Status);
            var priority = string.IsNullOrWhiteSpace(request.Priority)
                ? NormalizeTicketPriority(current.Priority)
                : NormalizeTicketPriority(request.Priority);

            // assigneeUserId omitted/null → keep; 0 → clear assignment.
            long? assignee = current.AssigneeUserId;
            if (request.AssigneeUserId.HasValue)
                assignee = request.AssigneeUserId.Value <= 0 ? null : request.AssigneeUserId;

            if (!IsAllowedTicketStatus(status))
                return S3ActionResult.Fail(400, "Status must be OPEN, IN_PROGRESS, RESOLVED, or CLOSED.");
            if (!IsAllowedTicketPriority(priority))
                return S3ActionResult.Fail(400, "Priority must be LOW, NORMAL, HIGH, or URGENT.");

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.SupportTicket
                SET Status = {status}, Priority = {priority}, AssigneeUserId = {assignee}
                WHERE SupportTicketId = {ticketId}");

            return S3ActionResult.Ok(new
            {
                success = true,
                supportTicketId = ticketId,
                status,
                priority,
                assigneeUserId = assignee
            });
        }

        /// <summary>
        /// SUP-04.01 — create thread message; optional fileName → SupportTicketAttachment linked to the message.
        /// </summary>
        public async Task<S3ActionResult> AddMessageAsync(int ticketId, SupportMessageCreate request, long userId, string role, bool isAdmin)
        {
            if (ticketId <= 0)
                return S3ActionResult.Fail(400, "TicketId is required.");
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Body is required.");
            if (!await CanSeeTicketAsync(ticketId, userId, isAdmin))
                return S3ActionResult.Fail(403, "Not allowed on this ticket.");

            var now = DateTime.Now;
            var body = request.Body.Trim();
            var authorRole = string.IsNullOrWhiteSpace(role) ? "Patient" : role.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SupportTicketMessage (SupportTicketId, AuthorUserId, AuthorRole, Body, At)
                VALUES ({ticketId}, {userId}, {authorRole}, {body}, {now})");

            var msg = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 SupportTicketMessageId AS Id
                FROM dbo.SupportTicketMessage
                WHERE SupportTicketId = {ticketId} AND AuthorUserId = {userId}
                ORDER BY SupportTicketMessageId DESC").FirstAsync();

            int? attachmentId = null;
            string? storageKey = null;
            if (!string.IsNullOrWhiteSpace(request.FileName))
            {
                var key = "support/" + ticketId + "/" + Guid.NewGuid().ToString("N");
                var file = request.FileName.Trim();
                await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    INSERT INTO dbo.SupportTicketAttachment (SupportTicketId, SupportTicketMessageId, FileName, StorageKey, At)
                    VALUES ({ticketId}, {msg.Id}, {file}, {key}, {now})");
                var att = await _context.Database.SqlQuery<IdRow>($@"
                    SELECT TOP 1 SupportTicketAttachmentId AS Id
                    FROM dbo.SupportTicketAttachment
                    WHERE SupportTicketId = {ticketId} AND StorageKey = {key}
                    ORDER BY SupportTicketAttachmentId DESC").FirstOrDefaultAsync();
                attachmentId = att?.Id;
                storageKey = key;
            }

            return S3ActionResult.Ok(new
            {
                success = true,
                supportTicketId = ticketId,
                supportTicketMessageId = msg.Id,
                supportTicketAttachmentId = attachmentId,
                storageKey
            });
        }

        /// <summary>SUP-04.01 — list messages + attachments for a ticket (reporter or admin).</summary>
        public async Task<S3ActionResult> ListMessagesAsync(int ticketId, long userId, bool isAdmin)
        {
            if (ticketId <= 0)
                return S3ActionResult.Fail(400, "TicketId is required.");
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

        /// <summary>SUP-04.01 — update message body (author or admin).</summary>
        public async Task<S3ActionResult> UpdateMessageAsync(int ticketId, int messageId, SupportMessageCreate request, long userId, bool isAdmin)
        {
            if (ticketId <= 0 || messageId <= 0)
                return S3ActionResult.Fail(400, "TicketId and MessageId are required.");
            if (request == null || string.IsNullOrWhiteSpace(request.Body))
                return S3ActionResult.Fail(400, "Body is required.");
            if (!await CanSeeTicketAsync(ticketId, userId, isAdmin))
                return S3ActionResult.Fail(403, "Not allowed on this ticket.");

            var row = await _context.Database.SqlQuery<MessageRow>($@"
                SELECT SupportTicketMessageId, SupportTicketId, AuthorUserId, AuthorRole, Body, At
                FROM dbo.SupportTicketMessage
                WHERE SupportTicketMessageId = {messageId} AND SupportTicketId = {ticketId}").FirstOrDefaultAsync();
            if (row == null)
                return S3ActionResult.Fail(404, "Message not found.");
            if (!isAdmin && row.AuthorUserId != userId)
                return S3ActionResult.Fail(403, "Only the author or admin can edit this message.");

            var body = request.Body.Trim();
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.SupportTicketMessage SET Body = {body}
                WHERE SupportTicketMessageId = {messageId}");

            return S3ActionResult.Ok(new { success = true, supportTicketMessageId = messageId, supportTicketId = ticketId });
        }

        /// <summary>SUP-04.01 — delete message and its linked attachments (author or admin).</summary>
        public async Task<S3ActionResult> DeleteMessageAsync(int ticketId, int messageId, long userId, bool isAdmin)
        {
            if (ticketId <= 0 || messageId <= 0)
                return S3ActionResult.Fail(400, "TicketId and MessageId are required.");
            if (!await CanSeeTicketAsync(ticketId, userId, isAdmin))
                return S3ActionResult.Fail(403, "Not allowed on this ticket.");

            var row = await _context.Database.SqlQuery<MessageRow>($@"
                SELECT SupportTicketMessageId, SupportTicketId, AuthorUserId, AuthorRole, Body, At
                FROM dbo.SupportTicketMessage
                WHERE SupportTicketMessageId = {messageId} AND SupportTicketId = {ticketId}").FirstOrDefaultAsync();
            if (row == null)
                return S3ActionResult.Fail(404, "Message not found.");
            if (!isAdmin && row.AuthorUserId != userId)
                return S3ActionResult.Fail(403, "Only the author or admin can delete this message.");

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE FROM dbo.SupportTicketAttachment
                WHERE SupportTicketId = {ticketId} AND SupportTicketMessageId = {messageId}");
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                DELETE FROM dbo.SupportTicketMessage
                WHERE SupportTicketMessageId = {messageId} AND SupportTicketId = {ticketId}");

            return S3ActionResult.Ok(new { success = true, supportTicketMessageId = messageId, deleted = true });
        }

        /// <summary>
        /// SUP-06.02 — public list of published help articles (unpublished hidden).
        /// </summary>
        public async Task<S3ActionResult> ListHelpAsync(bool includeUnpublished)
        {
            List<HelpRow> rows;
            if (includeUnpublished)
            {
                rows = await _context.Database.SqlQuery<HelpRow>($@"
                    SELECT HelpArticleId, Title, Slug, Body, IsPublished
                    FROM dbo.HelpArticle
                    ORDER BY Title, HelpArticleId").ToListAsync();
            }
            else
            {
                rows = await _context.Database.SqlQuery<HelpRow>($@"
                    SELECT HelpArticleId, Title, Slug, Body, IsPublished
                    FROM dbo.HelpArticle
                    WHERE IsPublished = 1
                    ORDER BY Title, HelpArticleId").ToListAsync();
            }

            return S3ActionResult.Ok(new { success = true, data = rows });
        }

        /// <summary>
        /// SUP-06.02 — public GET one help article by slug. Unpublished → 404.
        /// </summary>
        public async Task<S3ActionResult> GetHelpAsync(string slug, bool includeUnpublished)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return S3ActionResult.Fail(400, "Slug is required.");

            var key = slug.Trim().ToLowerInvariant();
            var row = await _context.Database.SqlQuery<HelpRow>($@"
                SELECT HelpArticleId, Title, Slug, Body, IsPublished
                FROM dbo.HelpArticle
                WHERE Slug = {key}").FirstOrDefaultAsync();
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

        /// <summary>
        /// SUP-07.02 — staff completes WEB-04 create on behalf of a patient (BookingChannel=Assisted).
        /// </summary>
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

        /// <summary>
        /// SUP-07.02 — patient requests booking assistance (SupportTicket Category=AssistedRequest).
        /// Staff then completes booking via AssistedBook.
        /// </summary>
        public async Task<S3ActionResult> RequestAssistanceAsync(AssistanceRequestBody request, long userId, string role)
        {
            if (!role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
                return S3ActionResult.Fail(403, "Only a patient can request booking assistance.");

            var notes = string.IsNullOrWhiteSpace(request?.Notes)
                ? "Patient requested help booking an appointment."
                : request!.Notes.Trim();
            var parts = new List<string> { notes };
            if (request?.DoctorId is > 0)
                parts.Add("DoctorId=" + request.DoctorId);
            if (request?.PatientId is > 0)
                parts.Add("PatientId=" + request.PatientId);
            if (!string.IsNullOrWhiteSpace(request?.PreferredDate))
                parts.Add("PreferredDate=" + request.PreferredDate.Trim());
            if (!string.IsNullOrWhiteSpace(request?.ContactMobile))
                parts.Add("ContactMobile=" + request.ContactMobile.Trim());

            var body = string.Join(" | ", parts);
            var now = DateTime.Now;
            var due = now.AddHours(24);
            const string category = "AssistedRequest";
            const string subject = "Assisted booking request";
            const string reporter = "Patient";

            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SupportTicket
                    (ReporterUserId, ReporterRole, Category, Subject, Body, Status, Priority, SlaDueAt, CreatedAt)
                VALUES
                    ({userId}, {reporter}, {category}, {subject}, {body}, N'OPEN', N'NORMAL', {due}, {now})");

            var id = await _context.Database.SqlQuery<IdRow>($@"
                SELECT TOP 1 SupportTicketId AS Id
                FROM dbo.SupportTicket
                WHERE ReporterUserId = {userId} AND Category = N'AssistedRequest'
                ORDER BY SupportTicketId DESC").FirstAsync();

            return S3ActionResult.Ok(new
            {
                success = true,
                supportTicketId = id.Id,
                category,
                status = "OPEN",
                nextStep = "Staff completes booking via POST /api/Support/AssistedBook"
            });
        }

        /// <summary>SUP-07.02 — open AssistedRequest tickets for clinic staff to work.</summary>
        public async Task<S3ActionResult> ListAssistanceRequestsAsync()
        {
            var rows = await _context.Database.SqlQuery<AssistanceTicketRow>($@"
                SELECT SupportTicketId, ReporterUserId, ReporterRole, Category, Subject, Body, Status, Priority, AssigneeUserId, SlaDueAt, CreatedAt
                FROM dbo.SupportTicket
                WHERE Category = N'AssistedRequest'
                ORDER BY SupportTicketId DESC").ToListAsync();
            return S3ActionResult.Ok(new { success = true, data = rows });
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

            var caseIdsForPatient = await _context.CaseEntryDetails.AsNoTracking()
                .Where(c => c.PatientId == appt.PatientId && c.DoctorId == appt.DoctorId && c.DeleteStatus != true)
                .Select(c => c.CaseId)
                .ToListAsync();
            var complaint = caseIdsForPatient.Count == 0
                ? null
                : await _context.CaseEntryChiefComplaints.AsNoTracking()
                    .Where(c => c.CaseId.HasValue && caseIdsForPatient.Contains(c.CaseId.Value))
                    .OrderByDescending(c => c.CaseChiefComplaintId)
                    .Select(c => new TextRow { Text = c.ChiefComplaintName })
                    .FirstOrDefaultAsync();

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

        /// <summary>TEL-10.02 — case-linked chat is treating doctor + mapped patient only (no admin/reception).</summary>
        private async Task<bool> CanAccessChatAsync(SessionRow session, S3Caller caller)
        {
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

        /// <summary>SUP-07.01 — includes AssistedRequest ticket type for assisted booking help.</summary>
        private static string NormalizeCategory(string? category)
        {
            var value = (category ?? "other").Trim();
            var lower = value.ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
            if (lower is "assistedrequest" or "assisted-request" or "assisted")
                return "AssistedRequest";
            var allowed = new[] { "billing", "booking", "technical", "clinical", "other" };
            return allowed.Contains(value.ToLowerInvariant()) ? value.ToLowerInvariant() : "other";
        }

        /// <summary>SUP-03.02 — canonical ticket status (Open → OPEN).</summary>
        private static string NormalizeTicketStatus(string? status)
        {
            var key = (status ?? "OPEN").Trim().ToUpperInvariant().Replace(' ', '_').Replace('-', '_');
            return key switch
            {
                "OPEN" or "NEW" => "OPEN",
                "IN_PROGRESS" or "INPROGRESS" or "PROGRESS" or "WORKING" => "IN_PROGRESS",
                "RESOLVED" or "DONE" => "RESOLVED",
                "CLOSED" or "CLOSE" => "CLOSED",
                _ => key
            };
        }

        private static string NormalizeTicketPriority(string? priority)
        {
            var key = (priority ?? "NORMAL").Trim().ToUpperInvariant();
            return key switch
            {
                "LOW" => "LOW",
                "NORMAL" or "MEDIUM" or "MED" => "NORMAL",
                "HIGH" => "HIGH",
                "URGENT" or "CRITICAL" => "URGENT",
                _ => key
            };
        }

        private static bool IsAllowedTicketStatus(string status)
            => status is "OPEN" or "IN_PROGRESS" or "RESOLVED" or "CLOSED";

        private static bool IsAllowedTicketPriority(string priority)
            => priority is "LOW" or "NORMAL" or "HIGH" or "URGENT";

        private static int PriorityRank(string? priority) => NormalizeTicketPriority(priority) switch
        {
            "URGENT" => 0,
            "HIGH" => 1,
            "NORMAL" => 2,
            "LOW" => 3,
            _ => 4
        };

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

        /// <summary>SUP-07.02 / SUP-07.03 — AssistedRequest queue includes Body for staff prefill.</summary>
        private sealed class AssistanceTicketRow
        {
            public int SupportTicketId { get; set; }
            public long ReporterUserId { get; set; }
            public string ReporterRole { get; set; } = "";
            public string Category { get; set; } = "";
            public string Subject { get; set; } = "";
            public string Body { get; set; } = "";
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
