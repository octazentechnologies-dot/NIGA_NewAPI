using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

/// <summary>RPT-01.03 platform overview for the admin home: KPIs vs previous period, visit trend, mode split, top doctors, remedies, locations.</summary>
public partial class S5Week5Service
{
    public Task<S4ActionResult> AdminSummaryAsync(DateTime? from, DateTime? to, int? doctorId, S4Caller caller) => Guard(async () =>
    {
        var deny = Admin(caller);
        if (deny != null) return deny;
        var end = (to ?? DateTime.Today).Date.AddDays(1);
        var start = (from ?? end.AddMonths(-1)).Date;
        if (start >= end) return S4ActionResult.Fail(400, "VALIDATION", "From date must be before to date.");
        if ((end - start).TotalDays > 732) return S4ActionResult.Fail(400, "VALIDATION", "Choose a range of up to two years.");
        var length = end - start;
        var prevStart = start - length;

        var current = await AppointmentKpisAsync(start, end, doctorId);
        var previous = await AppointmentKpisAsync(prevStart, start, doctorId);
        var revenue = await RevenueByStreamAsync(start, end, doctorId);
        var prevRevenue = await RevenueByStreamAsync(prevStart, start, doctorId);

        var doctorCounts = (await _context.Database.SqlQuery<DoctorCountRow>($@"
            SELECT COUNT(1) AS Total,
                   SUM(CASE WHEN VerificationStatus = N'Verified' THEN 1 ELSE 0 END) AS Verified
            FROM dbo.Doctor WHERE ISNULL(DeleteStatus, 0) = 0").ToListAsync()).First();
        var patients = doctorId.HasValue
            ? await CountAsync($@"SELECT COUNT(DISTINCT PatientId) AS Value FROM dbo.PatientAppointment
                                  WHERE ISNULL(DeleteStatus, 0) = 0 AND DoctorId = {doctorId}")
            : await CountAsync($@"SELECT COUNT(1) AS Value FROM dbo.Patient WHERE ISNULL(DeleteStatus, 0) = 0");
        var medicineOrders = await CountAsync($@"
            SELECT COUNT(1) AS Value FROM dbo.MedicineOrder o
            LEFT JOIN dbo.ErxSnapshot e ON e.ErxSnapshotId = o.ErxSnapshotId
            WHERE o.CreatedAt >= {start} AND o.CreatedAt < {end} AND ({doctorId} IS NULL OR e.DoctorId = {doctorId})");

        var daily = await _context.Database.SqlQuery<DailyVisitRow>($@"
            SELECT CAST(a.AppointmentDate AS date) AS Day,
                   COUNT(1) AS Appointments,
                   SUM(CASE WHEN f.FirstAt = a.AppointmentDate THEN 1 ELSE 0 END) AS NewPatients
            FROM dbo.PatientAppointment a
            JOIN (SELECT PatientId, MIN(AppointmentDate) AS FirstAt FROM dbo.PatientAppointment
                  WHERE ISNULL(DeleteStatus, 0) = 0 AND ({doctorId} IS NULL OR DoctorId = {doctorId})
                  GROUP BY PatientId) f ON f.PatientId = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0 AND a.AppointmentDate >= {start} AND a.AppointmentDate < {end}
              AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})
            GROUP BY CAST(a.AppointmentDate AS date)").ToListAsync();
        var dailyRevenue = await _context.Database.SqlQuery<DailyAmountRow>($@"
            SELECT CAST(CreatedAt AS date) AS Day, CAST(SUM(Amount) AS decimal(18,2)) AS Amount
            FROM dbo.PaymentOrder
            WHERE Status IN (N'CAPTURED', N'COLLECTED', N'PAID') AND CreatedAt >= {start} AND CreatedAt < {end}
              AND ({doctorId} IS NULL OR DoctorId = {doctorId})
            GROUP BY CAST(CreatedAt AS date)").ToListAsync();

        var modes = await _context.Database.SqlQuery<NameCountRow>($@"
            SELECT m.Name, COUNT(1) AS Cnt
            FROM (SELECT CASE
                         WHEN ISNULL(a.IsTele, 0) = 1 OR a.Status = N'E-CONSULT' OR a.ConsultMode LIKE N'%Tele%' OR a.ConsultMode LIKE N'%Video%' THEN N'Video consult'
                         WHEN a.Status = N'WALK-IN' THEN N'Walk-in'
                         ELSE N'In-clinic' END AS Name
                  FROM dbo.PatientAppointment a
                  WHERE ISNULL(a.DeleteStatus, 0) = 0 AND a.AppointmentDate >= {start} AND a.AppointmentDate < {end}
                    AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})) m
            GROUP BY m.Name").ToListAsync();

        var topDoctors = await _context.Database.SqlQuery<TopDoctorRow>($@"
            SELECT TOP 5 a.DoctorId,
                   LTRIM(RTRIM(CONCAT(N'Dr. ', MAX(d.FirstName), N' ', MAX(d.LastName)))) AS DoctorName,
                   MAX(d.ClinicName) AS ClinicName,
                   COUNT(1) AS Appointments,
                   COUNT(DISTINCT a.PatientId) AS Patients,
                   SUM(CASE WHEN a.Status = N'COMPLETED' THEN 1 ELSE 0 END) AS Completed,
                   CAST(ISNULL((SELECT SUM(po.Amount) FROM dbo.PaymentOrder po
                                WHERE po.DoctorId = a.DoctorId AND po.Status IN (N'CAPTURED', N'COLLECTED', N'PAID')
                                  AND po.CreatedAt >= {start} AND po.CreatedAt < {end}), 0) AS decimal(18,2)) AS Revenue
            FROM dbo.PatientAppointment a
            LEFT JOIN dbo.Doctor d ON d.DoctorID = a.DoctorId
            WHERE ISNULL(a.DeleteStatus, 0) = 0 AND a.AppointmentDate >= {start} AND a.AppointmentDate < {end}
              AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})
            GROUP BY a.DoctorId
            ORDER BY COUNT(1) DESC").ToListAsync();

        var remedies = await _context.Database.SqlQuery<NameCountRow>($@"
            SELECT TOP 5 ISNULL(NULLIF(i.RemedyName, N''), i.RemedyCode) AS Name, COUNT(1) AS Cnt
            FROM dbo.MedicineOrderItem i
            JOIN dbo.MedicineOrder o ON o.MedicineOrderId = i.MedicineOrderId
            LEFT JOIN dbo.ErxSnapshot e ON e.ErxSnapshotId = o.ErxSnapshotId
            WHERE o.CreatedAt >= {start} AND o.CreatedAt < {end} AND ({doctorId} IS NULL OR e.DoctorId = {doctorId})
            GROUP BY ISNULL(NULLIF(i.RemedyName, N''), i.RemedyCode)
            ORDER BY COUNT(1) DESC").ToListAsync();

        var locations = await _context.Database.SqlQuery<NameCountRow>($@"
            SELECT ISNULL(s.StateName, N'Not set') AS Name, COUNT(DISTINCT a.PatientId) AS Cnt
            FROM dbo.PatientAppointment a
            JOIN dbo.Patient p ON p.PatientID = a.PatientId
            LEFT JOIN dbo.StateMaster s ON s.StateId = p.StateId
            WHERE ISNULL(a.DeleteStatus, 0) = 0 AND a.AppointmentDate >= {start} AND a.AppointmentDate < {end}
              AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})
            GROUP BY ISNULL(s.StateName, N'Not set')").ToListAsync();

        var recent = await _context.Database.SqlQuery<RecentAppointmentRow>($@"
            SELECT TOP 8 a.PatientAppId, a.PatientId, p.PatientName, a.DoctorId,
                   LTRIM(RTRIM(CONCAT(N'Dr. ', d.FirstName, N' ', d.LastName))) AS DoctorName,
                   a.AppointmentDate, a.Status, a.VisitType, a.ConsultMode, a.IsTele, a.PaymentStatus
            FROM dbo.PatientAppointment a
            LEFT JOIN dbo.Patient p ON p.PatientID = a.PatientId
            LEFT JOIN dbo.Doctor d ON d.DoctorID = a.DoctorId
            WHERE ISNULL(a.DeleteStatus, 0) = 0 AND a.AppointmentDate >= {start} AND a.AppointmentDate < {end}
              AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})
            ORDER BY a.AppointmentDate DESC, a.PatientAppId DESC").ToListAsync();

        var doctors = await _context.Database.SqlQuery<DoctorOptionRow>($@"
            SELECT DoctorID AS DoctorId,
                   LTRIM(RTRIM(CONCAT(N'Dr. ', FirstName, N' ', LastName))) AS DoctorName,
                   ClinicName
            FROM dbo.Doctor WHERE ISNULL(DeleteStatus, 0) = 0
            ORDER BY FirstName, LastName").ToListAsync();

        var monthly = length.TotalDays > 62;
        var revenueByDay = dailyRevenue.ToDictionary(r => r.Day.Date, r => r.Amount);
        var visitsByDay = daily.ToDictionary(r => r.Day.Date);
        var trend = new List<object>();
        for (var cursor = monthly ? new DateTime(start.Year, start.Month, 1) : start; cursor < end; cursor = monthly ? cursor.AddMonths(1) : cursor.AddDays(1))
        {
            var bucketEnd = monthly ? cursor.AddMonths(1) : cursor.AddDays(1);
            int visits = 0, firsts = 0;
            decimal amount = 0;
            for (var day = cursor < start ? start : cursor; day < bucketEnd && day < end; day = day.AddDays(1))
            {
                if (visitsByDay.TryGetValue(day, out var v)) { visits += v.Appointments; firsts += v.NewPatients; }
                if (revenueByDay.TryGetValue(day, out var r)) amount += r;
            }
            trend.Add(new { label = cursor.ToString(monthly ? "MMM yyyy" : "dd MMM", CultureInfo.InvariantCulture), appointments = visits, newPatients = firsts, revenue = amount });
        }

        var locationTotal = locations.Sum(l => l.Cnt);
        return S4ActionResult.Ok(new
        {
            success = true,
            from = start,
            to = end.AddDays(-1),
            doctorId,
            data = new
            {
                kpis = new
                {
                    appointments = current.Appointments,
                    prevAppointments = previous.Appointments,
                    completed = current.Completed,
                    cancelled = current.Cancelled,
                    newPatients = current.NewPatients,
                    prevNewPatients = previous.NewPatients,
                    followUpRate = Rate(current.FollowUps, current.Appointments),
                    prevFollowUpRate = Rate(previous.FollowUps, previous.Appointments),
                    revenue = revenue.Total,
                    prevRevenue = prevRevenue.Total,
                    consultRevenue = revenue.Consult,
                    medicineRevenue = revenue.Medicine,
                    doctors = doctorCounts.Total,
                    verifiedDoctors = doctorCounts.Verified,
                    patients,
                    medicineOrders,
                },
                trendGranularity = monthly ? "month" : "day",
                trend,
                consultModes = modes.OrderByDescending(m => m.Cnt).Select(m => new { name = m.Name, count = m.Cnt }),
                topDoctors,
                topRemedies = remedies.Select(r => new { name = r.Name, count = r.Cnt }),
                locations = locations.OrderByDescending(l => l.Cnt).Take(6)
                    .Select(l => new { name = l.Name, patients = l.Cnt, share = Rate(l.Cnt, locationTotal) }),
                locationTotal,
                recentAppointments = recent,
                doctors,
            }
        });
    });

    private static decimal Rate(int part, int total) => total == 0 ? 0 : Math.Round(part * 100m / total, 1);

    private async Task<AppointmentKpiRow> AppointmentKpisAsync(DateTime start, DateTime end, int? doctorId)
    {
        var rows = await _context.Database.SqlQuery<AppointmentKpiRow>($@"
            SELECT COUNT(a.PatientAppId) AS Appointments,
                   ISNULL(SUM(CASE WHEN a.Status = N'COMPLETED' THEN 1 ELSE 0 END), 0) AS Completed,
                   ISNULL(SUM(CASE WHEN a.Status = N'CANCELLED' THEN 1 ELSE 0 END), 0) AS Cancelled,
                   ISNULL(SUM(CASE WHEN a.AppointmentDate > f.FirstAt THEN 1 ELSE 0 END), 0) AS FollowUps,
                   (SELECT COUNT(1) FROM (SELECT MIN(AppointmentDate) AS FirstAt FROM dbo.PatientAppointment
                                          WHERE ISNULL(DeleteStatus, 0) = 0 AND ({doctorId} IS NULL OR DoctorId = {doctorId})
                                          GROUP BY PatientId) n
                    WHERE n.FirstAt >= {start} AND n.FirstAt < {end}) AS NewPatients
            FROM dbo.PatientAppointment a
            JOIN (SELECT PatientId, MIN(AppointmentDate) AS FirstAt FROM dbo.PatientAppointment
                  WHERE ISNULL(DeleteStatus, 0) = 0 AND ({doctorId} IS NULL OR DoctorId = {doctorId})
                  GROUP BY PatientId) f ON f.PatientId = a.PatientId
            WHERE ISNULL(a.DeleteStatus, 0) = 0 AND a.AppointmentDate >= {start} AND a.AppointmentDate < {end}
              AND ({doctorId} IS NULL OR a.DoctorId = {doctorId})").ToListAsync();
        return rows.First();
    }

    private async Task<RevenueStreamRow> RevenueByStreamAsync(DateTime start, DateTime end, int? doctorId)
    {
        var rows = await _context.Database.SqlQuery<RevenueStreamRow>($@"
            SELECT CAST(ISNULL(SUM(Amount), 0) AS decimal(18,2)) AS Total,
                   CAST(ISNULL(SUM(CASE WHEN Stream = N'CONSULT' THEN Amount END), 0) AS decimal(18,2)) AS Consult,
                   CAST(ISNULL(SUM(CASE WHEN Stream = N'MEDICINE' THEN Amount END), 0) AS decimal(18,2)) AS Medicine
            FROM dbo.PaymentOrder
            WHERE Status IN (N'CAPTURED', N'COLLECTED', N'PAID') AND CreatedAt >= {start} AND CreatedAt < {end}
              AND ({doctorId} IS NULL OR DoctorId = {doctorId})").ToListAsync();
        return rows.First();
    }

    private sealed class AppointmentKpiRow
    {
        public int Appointments { get; set; }
        public int Completed { get; set; }
        public int Cancelled { get; set; }
        public int FollowUps { get; set; }
        public int NewPatients { get; set; }
    }

    private sealed class RevenueStreamRow
    {
        public decimal Total { get; set; }
        public decimal Consult { get; set; }
        public decimal Medicine { get; set; }
    }

    private sealed class DoctorCountRow
    {
        public int Total { get; set; }
        public int? Verified { get; set; }
    }

    private sealed class DailyVisitRow
    {
        public DateTime Day { get; set; }
        public int Appointments { get; set; }
        public int NewPatients { get; set; }
    }

    private sealed class DailyAmountRow
    {
        public DateTime Day { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed class NameCountRow
    {
        public string Name { get; set; } = "";
        public int Cnt { get; set; }
    }

    private sealed class TopDoctorRow
    {
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? ClinicName { get; set; }
        public int Appointments { get; set; }
        public int Patients { get; set; }
        public int Completed { get; set; }
        public decimal Revenue { get; set; }
    }

    private sealed class RecentAppointmentRow
    {
        public int PatientAppId { get; set; }
        public int PatientId { get; set; }
        public string? PatientName { get; set; }
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public string? Status { get; set; }
        public string? VisitType { get; set; }
        public string? ConsultMode { get; set; }
        public bool? IsTele { get; set; }
        public string? PaymentStatus { get; set; }
    }

    private sealed class DoctorOptionRow
    {
        public int DoctorId { get; set; }
        public string? DoctorName { get; set; }
        public string? ClinicName { get; set; }
    }
}
