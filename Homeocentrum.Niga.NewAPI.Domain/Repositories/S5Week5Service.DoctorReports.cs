using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

/// <summary>
/// Doctor report pages (practice, follow-ups, earnings). Every query is scoped to the calling doctor;
/// an admin without a doctor id sees the whole clinic.
/// </summary>
public partial class S5Week5Service
{
    private const int MaxReportDays = 366;

    public Task<S4ActionResult> PracticeReportAsync(DateTime? from, DateTime? to, S4Caller caller) => Guard(async () =>
    {
        var scope = ReportScope(caller, out var doctorId);
        if (scope != null) return scope;
        ReportRange(from, to, out var start, out var end);
        var span = end - start;
        var prevStart = start - span;

        var rows = await _context.Database.SqlQuery<PracticeVisitRow>($@"
            SELECT a.PatientAppId, a.PatientId, a.AppointmentDate, a.VisitType, a.ConsultMode, a.IsTele,
                   CAST(CASE WHEN EXISTS (
                       SELECT 1 FROM dbo.PatientAppointment e
                       WHERE e.PatientId = a.PatientId AND e.DoctorId = a.DoctorId AND ISNULL(e.DeleteStatus,0)=0
                         AND UPPER(ISNULL(e.Status,'')) <> N'CANCELLED'
                         AND (e.AppointmentDate < a.AppointmentDate OR (e.AppointmentDate = a.AppointmentDate AND e.PatientAppId < a.PatientAppId))
                   ) THEN 0 ELSE 1 END AS bit) AS IsNew,
                   CAST(ISNULL((SELECT SUM(po.Amount) FROM dbo.PaymentOrder po
                                WHERE po.PatientAppId = a.PatientAppId AND po.Status IN (N'CAPTURED', N'COLLECTED', N'PAID')), 0) AS decimal(18,2)) AS Paid
            FROM dbo.PatientAppointment a
            WHERE ISNULL(a.DeleteStatus,0)=0 AND UPPER(ISNULL(a.Status,'')) <> N'CANCELLED'
              AND a.AppointmentDate >= {prevStart} AND a.AppointmentDate < {end}
              AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})").ToListAsync();

        var current = rows.Where(r => r.AppointmentDate >= start).ToList();
        var previous = rows.Where(r => r.AppointmentDate < start).ToList();

        var daily = current
            .GroupBy(r => r.AppointmentDate!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), newCount = g.Count(r => r.IsNew), followUpCount = g.Count(r => !r.IsNew) })
            .ToList();
        var types = current
            .GroupBy(r => IsTeleVisit(r.ConsultMode, r.IsTele) ? "Video" : "In-clinic")
            .Select(g => new { name = g.Key, cnt = g.Count() })
            .OrderByDescending(x => x.cnt)
            .ToList();
        var services = current
            .GroupBy(r => VisitLabel(r.VisitType, r.ConsultMode, r.IsTele, r.IsNew))
            .Select(g => new { name = g.Key, consultations = g.Count(), revenue = g.Sum(r => r.Paid), patients = g.Select(r => r.PatientId).Distinct().Count() })
            .OrderByDescending(x => x.consultations)
            .ToList();

        var rating = await _context.Database.SqlQuery<RatingRow>($@"
            SELECT CAST(AVG(CAST(Rating AS decimal(5,2))) AS decimal(5,2)) AS Average, COUNT(1) AS Cnt
            FROM dbo.Review
            WHERE Status = N'APPROVED' AND ({doctorId} IS NULL OR DoctorId = {doctorId})").FirstAsync();

        return S4ActionResult.Ok(new
        {
            success = true,
            from = start,
            to = end.AddDays(-1),
            totals = new
            {
                consultations = current.Count,
                newPatients = current.Count(r => r.IsNew),
                followUps = current.Count(r => !r.IsNew),
                revenue = current.Sum(r => r.Paid)
            },
            previous = new
            {
                consultations = previous.Count,
                newPatients = previous.Count(r => r.IsNew),
                followUps = previous.Count(r => !r.IsNew)
            },
            rating = new { average = rating.Average, count = rating.Cnt },
            daily,
            types,
            services
        });
    });

    public Task<S4ActionResult> FollowUpReportAsync(DateTime? from, DateTime? to, S4Caller caller) => Guard(async () =>
    {
        var scope = ReportScope(caller, out var doctorId);
        if (scope != null) return scope;
        ReportRange(from, to, out var start, out var end);
        var today = DateTime.Today;

        var rows = await _context.Database.SqlQuery<FollowReportRow>($@"
            SELECT t.FollowUpTaskId, t.Title, t.DueDate, t.Status, t.CompletedAt,
                   pl.PatientAppId, pl.PatientId, pl.Note, p.PatientName, p.MobileNo,
                   a.ConsultMode, a.IsTele,
                   (SELECT MAX(v.AppointmentDate) FROM dbo.PatientAppointment v
                     WHERE v.PatientId = pl.PatientId AND v.DoctorId = pl.DoctorId AND ISNULL(v.DeleteStatus,0)=0
                       AND UPPER(ISNULL(v.Status,'')) <> N'CANCELLED' AND v.AppointmentDate < DATEADD(day, 1, {today})) AS LastVisit
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan pl ON pl.FollowUpPlanId = t.FollowUpPlanId
            LEFT JOIN dbo.Patient p ON p.PatientID = pl.PatientId
            LEFT JOIN dbo.PatientAppointment a ON a.PatientAppId = pl.PatientAppId
            WHERE ({doctorId} IS NULL OR pl.DoctorId = {doctorId})
              AND t.Status <> N'CANCELLED'
              AND ((t.DueDate >= {start} AND t.DueDate < {end}) OR (t.Status = N'OPEN' AND t.DueDate < {start}))
            ORDER BY t.DueDate").ToListAsync();

        string Bucket(FollowReportRow r) =>
            r.Status == "DONE" ? "completed" : (r.DueDate.Date < today ? "overdue" : "due");

        var daily = rows
            .Where(r => r.DueDate >= start)
            .GroupBy(r => r.DueDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                completed = g.Count(r => Bucket(r) == "completed"),
                due = g.Count(r => Bucket(r) == "due"),
                overdue = g.Count(r => Bucket(r) == "overdue")
            })
            .ToList();

        return S4ActionResult.Ok(new
        {
            success = true,
            from = start,
            to = end.AddDays(-1),
            totals = new
            {
                total = rows.Count,
                completed = rows.Count(r => Bucket(r) == "completed"),
                due = rows.Count(r => Bucket(r) == "due"),
                overdue = rows.Count(r => Bucket(r) == "overdue")
            },
            daily,
            data = rows.Select(r => new
            {
                r.FollowUpTaskId,
                r.Title,
                dueDate = r.DueDate,
                r.Status,
                r.CompletedAt,
                r.PatientAppId,
                r.PatientId,
                patientName = r.PatientName,
                mobileNo = r.MobileNo,
                note = r.Note,
                lastVisit = r.LastVisit,
                mode = IsTeleVisit(r.ConsultMode, r.IsTele) ? "Video" : "In-clinic",
                bucket = Bucket(r)
            })
        });
    });

    public Task<S4ActionResult> UpdateFollowUpTaskAsync(int taskId, FollowUpTaskWrite request, S4Caller caller) => Guard(async () =>
    {
        var scope = ReportScope(caller, out var doctorId);
        if (scope != null) return scope;
        var status = (request?.Status ?? "OPEN").Trim().ToUpperInvariant();
        if (status is not ("OPEN" or "DONE" or "CANCELLED"))
            return S4ActionResult.Fail(400, "VALIDATION", "Status must be OPEN, DONE or CANCELLED.");
        var title = string.IsNullOrWhiteSpace(request!.Title) ? null : request.Title.Trim();
        if (title != null && title.Length > 200)
            return S4ActionResult.Fail(400, "VALIDATION", "Title must be 200 characters or fewer.");
        DateTime? due = request.DueDate?.Date;
        if (status == "OPEN" && due.HasValue && due.Value < DateTime.Today)
            return S4ActionResult.Fail(400, "VALIDATION", "Follow-up date cannot be in the past.");
        DateTime? completedAt = status == "DONE" ? DateTime.Now : null;

        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE t
            SET DueDate = ISNULL({due}, t.DueDate),
                Title = ISNULL({title}, t.Title),
                Status = {status},
                CompletedAt = CASE WHEN {status} = N'DONE' THEN ISNULL(t.CompletedAt, {completedAt}) ELSE NULL END
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan pl ON pl.FollowUpPlanId = t.FollowUpPlanId
            WHERE t.FollowUpTaskId = {taskId} AND ({doctorId} IS NULL OR pl.DoctorId = {doctorId})");
        if (updated == 0)
            return S4ActionResult.Fail(404, "NOT_FOUND", "Follow-up not found.");
        return S4ActionResult.Ok(new { success = true, followUpTaskId = taskId, status });
    });

    public Task<S4ActionResult> EarningsReportAsync(DateTime? from, DateTime? to, S4Caller caller) => Guard(async () =>
    {
        var scope = ReportScope(caller, out var doctorId);
        if (scope != null) return scope;
        ReportRange(from, to, out var start, out var end);

        var rows = await _context.Database.SqlQuery<EarningRow>($@"
            SELECT TOP 1000 po.PaymentOrderId, po.Stream, po.PatientAppId, po.MedicineOrderId, po.PatientId,
                   p.PatientName, po.Amount, po.Status, po.Method, po.GatewayPaymentId, po.CreatedAt
            FROM dbo.PaymentOrder po
            LEFT JOIN dbo.Patient p ON p.PatientID = po.PatientId
            WHERE po.CreatedAt >= {start} AND po.CreatedAt < {end}
              AND ({doctorId} IS NULL OR po.DoctorId = {doctorId})
            ORDER BY po.CreatedAt DESC").ToListAsync();

        var pendingPayout = await _context.Database.SqlQuery<MoneyRow>($@"
            SELECT CAST(ISNULL(SUM(Amount), 0) AS decimal(18,2)) AS Amount, COUNT(1) AS Cnt
            FROM dbo.Payout
            WHERE Status = N'PENDING' AND PayeeType = N'Doctor'
              AND ({doctorId} IS NULL OR PayeeId = {doctorId})").FirstAsync();

        static string Kind(string? stream) => (stream ?? "").ToUpperInvariant() switch
        {
            "CONSULT" => "consultation",
            "MEDICINE" => "medicine",
            _ => "others"
        };
        static string State(string? status) => (status ?? "").ToUpperInvariant() switch
        {
            "CAPTURED" or "COLLECTED" or "PAID" => "Paid",
            "REFUNDED" or "PARTIALLY_REFUNDED" => "Refunded",
            "FAILED" or "CANCELLED" or "EXPIRED" => "Failed",
            _ => "Pending"
        };

        var paid = rows.Where(r => State(r.Status) == "Paid").ToList();
        var daily = paid
            .GroupBy(r => r.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                date = g.Key.ToString("yyyy-MM-dd"),
                consultation = g.Where(r => Kind(r.Stream) == "consultation").Sum(r => r.Amount),
                medicine = g.Where(r => Kind(r.Stream) == "medicine").Sum(r => r.Amount),
                others = g.Where(r => Kind(r.Stream) == "others").Sum(r => r.Amount)
            })
            .ToList();

        return S4ActionResult.Ok(new
        {
            success = true,
            from = start,
            to = end.AddDays(-1),
            totals = new
            {
                total = paid.Sum(r => r.Amount),
                consultation = paid.Where(r => Kind(r.Stream) == "consultation").Sum(r => r.Amount),
                medicine = paid.Where(r => Kind(r.Stream) == "medicine").Sum(r => r.Amount),
                others = paid.Where(r => Kind(r.Stream) == "others").Sum(r => r.Amount),
                pending = rows.Where(r => State(r.Status) == "Pending").Sum(r => r.Amount),
                payoutPending = pendingPayout.Amount
            },
            daily,
            data = rows.Select(r => new
            {
                r.PaymentOrderId,
                reference = string.IsNullOrWhiteSpace(r.GatewayPaymentId) ? "PO" + r.PaymentOrderId : r.GatewayPaymentId,
                date = r.CreatedAt,
                r.PatientAppId,
                r.MedicineOrderId,
                r.PatientId,
                patientName = r.PatientName,
                type = Kind(r.Stream) switch { "consultation" => "Consultation", "medicine" => "Medicine Order", _ => "Other" },
                r.Amount,
                method = r.Method,
                rawStatus = r.Status,
                status = State(r.Status)
            })
        });
    });

    private static S4ActionResult? ReportScope(S4Caller caller, out int? doctorId)
    {
        doctorId = caller.DoctorId is > 0 ? caller.DoctorId : null;
        if (doctorId.HasValue || caller.IsAdmin)
            return null;
        return S4ActionResult.Fail(403, "FORBIDDEN", "Doctor reports need a doctor account.");
    }

    private static void ReportRange(DateTime? from, DateTime? to, out DateTime start, out DateTime end)
    {
        start = (from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var last = (to ?? DateTime.Today).Date;
        if (last < start) last = start;
        if ((last - start).TotalDays > MaxReportDays) last = start.AddDays(MaxReportDays);
        end = last.AddDays(1);
    }

    private static bool IsTeleVisit(string? consultMode, bool? isTele)
        => isTele == true || string.Equals(consultMode, "Tele", StringComparison.OrdinalIgnoreCase);

    private static string VisitLabel(string? visitType, string? consultMode, bool? isTele, bool isNew)
    {
        var tele = IsTeleVisit(consultMode, isTele);
        var type = (visitType ?? "").Trim();
        if (type.Equals("FollowUp", StringComparison.OrdinalIgnoreCase) || (!isNew && !type.Equals("First", StringComparison.OrdinalIgnoreCase)))
            return tele ? "Tele Follow-up" : "Follow-up Consultation";
        return tele ? "Tele Consultation" : "New Consultation";
    }

    private sealed class DayStatusRow
    {
        public string Bucket { get; set; } = "";
        public string Status { get; set; } = "";
        public int Cnt { get; set; }
    }
    private sealed class HourRow
    {
        public int? Hour { get; set; }
        public int Cnt { get; set; }
    }
    private sealed class MissedRow
    {
        public int PatientAppId { get; set; }
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? MobileNo { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? AppointmentTime { get; set; }
        public string? Status { get; set; }
        public string? ConsultMode { get; set; }
        public bool? IsTele { get; set; }
        public string? CancelReasonCode { get; set; }
        public string? CancelReasonText { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
    private sealed class PracticeVisitRow
    {
        public int PatientAppId { get; set; }
        public int? PatientId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? VisitType { get; set; }
        public string? ConsultMode { get; set; }
        public bool? IsTele { get; set; }
        public bool IsNew { get; set; }
        public decimal Paid { get; set; }
    }
    private sealed class RatingRow
    {
        public decimal? Average { get; set; }
        public int Cnt { get; set; }
    }
    private sealed class FollowReportRow
    {
        public int FollowUpTaskId { get; set; }
        public string? Title { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public DateTime? CompletedAt { get; set; }
        public int? PatientAppId { get; set; }
        public int? PatientId { get; set; }
        public string? Note { get; set; }
        public string? PatientName { get; set; }
        public string? MobileNo { get; set; }
        public string? ConsultMode { get; set; }
        public bool? IsTele { get; set; }
        public DateTime? LastVisit { get; set; }
    }
    private sealed class EarningRow
    {
        public long PaymentOrderId { get; set; }
        public string? Stream { get; set; }
        public int? PatientAppId { get; set; }
        public int? MedicineOrderId { get; set; }
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? Method { get; set; }
        public string? GatewayPaymentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
