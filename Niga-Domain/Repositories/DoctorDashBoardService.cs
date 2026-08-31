using System.Net;
using API.Helpers;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Enums;
using Niga_Domain.Helpers;
using Niga_Domain.Interface;

namespace Niga_Domain.Implementation
{
    /// <summary>
    /// This is implementation  for the doctor dashboard Get operations
    /// </summary>
    public class DoctorDashBoardService : IDoctorDashBoardService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public DoctorDashBoardService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        /// <summary>
        /// Method is used for to get patient appointment by AppointmentDate
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<PagedList<PatientAppointmentModel>> GetPatientAppUserDate(ParameterParams parameter)
        {
            var patientAppModelList = new List<PatientAppointmentModel>();
            var errorResponseModel = new ErrorResponseModel();
            var patientAppEntityList = (
                from p in context.PatientAppointments
                join pa in context.Patients on p.PatientId equals pa.PatientId
                join c in context.CaseEntryDetails on p.PatientId equals c.PatientId
                join u in context.UserMasters on p.UserId equals u.UserId
                where p.UserId == parameter.UserId && p.AppointmentDate.Value.Date == parameter.Date.Value.Date
                && p.DeleteStatus == false
                select new PatientAppointmentModel
                {
                    PatientAppId = p.PatientAppId,
                    PatientId = p.PatientId,
                    PatientName = pa.PatientName,
                    MobileNo = pa.MobileNo,
                    Email = pa.Email,
                    UserId = p.UserId,
                    DoctorId = p.DoctorId,
                    AppointmentDate = p.AppointmentDate,
                    AppointmentTime = p.AppointmentTime.HasValue
                    ? p.AppointmentTime.Value
                        : null,
                    Status = p.Status,
                    DeleteStatus = p.DeleteStatus,
                    CaseId = c.CaseId,
                    Address = pa.Address,
                    Age = pa.Age,
                    Gender = pa.Gender,
                    DateOfBirth = pa.DateOfBirth,
                    IsWhatsAppOptIn = pa.IsWhatsAppOptIn,
                    WhatsAppOptInDate = pa.WhatsAppOptInDate,
                }
            ).AsQueryable();
            if (patientAppEntityList == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Patient Appointment not found";
            }
            
            if (!string.IsNullOrEmpty(parameter.search))
            {
                var search = parameter.search;
                patientAppEntityList = patientAppEntityList.Where(x =>
                    (
                        !string.IsNullOrEmpty(x.PatientName)
                        && x.PatientName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !string.IsNullOrEmpty(x.MobileNo)
                        && x.MobileNo.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !string.IsNullOrEmpty(x.Email)
                        && x.Email.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !string.IsNullOrEmpty(x.Status)
                        && x.Status.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
        
                );
            }

            return await PagedList<PatientAppointmentModel>.CreateAsync(
                patientAppEntityList.AsQueryable(),
                parameter.PageNumber,
                parameter.PageSize
            );
        }

        /// <summary>
        /// Method is used for to get patient appointment by appointmentDate
        /// </summary>
        /// <param name="appointmentDate"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public DoctorDashBoardModel GetPatientAppCount(
            long userId,
            DateTime? appointmentDate,
            ref ErrorResponseModel errorResponseModel
        )
        {
            
            DoctorDashBoardModel appointmentCount = new DoctorDashBoardModel();
            appointmentCount.patientApp = context
                .PatientAppointments.Where(x =>
                    x.AppointmentDate.Value.Date  == appointmentDate.Value.Date  && x.UserId == userId
                )
                .Count();
            appointmentCount.patientAppComplated = context
                .PatientAppointments.Where(x =>
                    x.AppointmentDate.Value.Date  == appointmentDate.Value.Date 
                    && x.UserId == userId
                    && x.Status == PatientsStatus.Completed.GetDisplayName()
                )
                .Count();
            appointmentCount.patientAppWaiting = context
                .PatientAppointments.Where(x =>
                    x.AppointmentDate.Value.Date  == appointmentDate.Value.Date 
                    && x.UserId == userId
                    && x.Status == PatientsStatus.Waiting.GetDisplayName()
                )
                .Count();
            appointmentCount.patientAppNotArrived = context
                .PatientAppointments.Where(x =>
                    x.AppointmentDate.Value.Date  == appointmentDate.Value.Date 
                    && x.UserId == userId
                    && x.Status == PatientsStatus.NotArrived.GetDisplayName()
                )
                .Count();
            appointmentCount.patientAppRemaining = context
                .PatientAppointments.Where(x =>
                    x.AppointmentDate.Value.Date  == appointmentDate.Value.Date 
                    && x.UserId == userId
                    && x.Status == PatientsStatus.Remaining.GetDisplayName()
                )
                .Count();
                appointmentCount.patientAppEConsult = context
                .PatientAppointments.Where(x =>
                    x.AppointmentDate.Value.Date  == appointmentDate.Value.Date 
                    && x.UserId == userId
                    && x.Status == PatientsStatus.E_Consult.GetDisplayName()
                )
                .Count();
            appointmentCount.patientAppWalkIn = context
               .PatientAppointments.Where(x =>
                   x.AppointmentDate.Value.Date == appointmentDate.Value.Date
                   && x.UserId == userId
                   && x.Status == PatientsStatus.Walk_In.GetDisplayName()
               )
               .Count();

            var doctor = context.Doctors.AsNoTracking()
                .FirstOrDefault(x => x.UserId == userId);
            if (doctor != null)
            {
                appointmentCount.totalPatients = context.CaseEntryDetails.AsNoTracking()
                    .Where(x => x.DoctorId == doctor.DoctorId && x.DeleteStatus == false)
                    .Select(x => x.PatientId)
                    .Distinct()
                    .Count();
            }

            return appointmentCount;
        }

    public async Task<DoctorDashBoardModel> GetPatientStatusStats(long userId, DateTime? FromDate, DateTime? ToDate, ErrorResponseModel errorResponseModel)
    {
            // DateTime fromDate = FromDate.HasValue ? FromDate.Value.Date : DateTime.MinValue;
            // DateTime toDate = ToDate.HasValue ? ToDate.Value.Date : DateTime.MaxValue;

            var appointmentStats =  (from  x in  context.PatientAppointments
                where  x.UserId == userId   && !string.IsNullOrEmpty(x.Status) && x.AppointmentDate.HasValue
                select new PatientAppModel
                {
                    Status = x.Status,
                    AppointmentDate = x.AppointmentDate,
                    PatientAppId = x.PatientAppId,
                    
                }).AsNoTracking().AsQueryable();
            if (FromDate.HasValue && ToDate.HasValue)
            {
                appointmentStats = appointmentStats
                    .Where(x => x.AppointmentDate.Value.Date >= FromDate.Value.Date && x.AppointmentDate.Value.Date <= ToDate.Value.Date);
            }

            var totalAppointments = appointmentStats.Count();

            var model = new DoctorDashBoardModel
            {
                patientApp = totalAppointments,
                patientAppComplated = totalAppointments > 0
                    ? appointmentStats.Count(x => x.Status == PatientsStatus.Completed.GetDisplayName()) * 100 / totalAppointments
                    : 0,
                patientAppWaiting = totalAppointments > 0
                    ? appointmentStats.Count(x => x.Status == PatientsStatus.Waiting.GetDisplayName()) * 100 / totalAppointments
                    : 0,
                patientAppNotArrived = totalAppointments > 0
                    ? appointmentStats.Count(x => x.Status == PatientsStatus.NotArrived.GetDisplayName()) * 100 / totalAppointments
                    : 0,
                patientAppRemaining = totalAppointments > 0
                    ? appointmentStats.Count(x => x.Status == PatientsStatus.Remaining.GetDisplayName()) * 100 / totalAppointments
                    : 0,
                patientAppEConsult = totalAppointments > 0
                    ? appointmentStats.Count(x => x.Status == PatientsStatus.E_Consult.GetDisplayName()) * 100 / totalAppointments
                    : 0
            };

            if (totalAppointments == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "No appointments found for the given date range.";
            }

            return model;
        }

        public async Task<PatientStatsChartsResponseModel> GetPatientStatsCharts(
            long userId,
            string period,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var normalizedPeriod = string.IsNullOrWhiteSpace(period) ? "ALL" : period.Trim().ToUpperInvariant();
            var (rangeFromDate, rangeToDate) = ResolvePatientStatsDateRange(normalizedPeriod, fromDate, toDate);

            var statusKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["waiting"] = PatientsStatus.Waiting.GetDisplayName(),
                ["walkin"] = PatientsStatus.Walk_In.GetDisplayName(),
                ["notarrived"] = PatientsStatus.NotArrived.GetDisplayName(),
                ["econsult"] = PatientsStatus.E_Consult.GetDisplayName(),
                ["remaining"] = PatientsStatus.Remaining.GetDisplayName(),
                ["completed"] = PatientsStatus.Completed.GetDisplayName(),
            };

            var appointments = await context.PatientAppointments
                .Where(x =>
                    x.UserId == userId
                    && x.DeleteStatus == false
                    && x.AppointmentDate.HasValue
                    && x.AppointmentDate.Value.Date >= rangeFromDate
                    && x.AppointmentDate.Value.Date <= rangeToDate)
                .Select(x => new
                {
                    AppointmentDate = x.AppointmentDate!.Value.Date,
                    x.Status
                })
                .AsNoTracking()
                .ToListAsync();

            var pieCounts = CreateEmptyStatusCounts();
            var monthlyCounts = BuildMonthlyBuckets(rangeFromDate, rangeToDate);

            foreach (var appointment in appointments)
            {
                var category = ResolveAppointmentStatusKey(appointment.Status, statusKeys);
                if (category == null)
                {
                    continue;
                }

                pieCounts[category]++;
                var monthKey = (appointment.AppointmentDate.Year, appointment.AppointmentDate.Month);
                monthlyCounts[monthKey][category]++;
            }

            var pieTotal = pieCounts.Values.Sum();
            var pieChart = new PatientStatsPieChartModel
            {
                Waiting = pieCounts["waiting"],
                WalkIn = pieCounts["walkin"],
                NotArrived = pieCounts["notarrived"],
                EConsult = pieCounts["econsult"],
                Remaining = pieCounts["remaining"],
                Completed = pieCounts["completed"],
                Total = pieTotal,
                Series = new List<int>
                {
                    pieCounts["waiting"],
                    pieCounts["walkin"],
                    pieCounts["notarrived"],
                    pieCounts["econsult"],
                    pieCounts["remaining"],
                    pieCounts["completed"]
                }
            };

            return new PatientStatsChartsResponseModel
            {
                Period = normalizedPeriod,
                FromDate = rangeFromDate,
                ToDate = rangeToDate,
                PieChart = pieChart,
                BarChart = BuildBarChartModel(monthlyCounts)
            };
        }

        private static (DateTime FromDate, DateTime ToDate) ResolvePatientStatsDateRange(
            string period,
            DateTime? fromDate,
            DateTime? toDate)
        {
            if (fromDate.HasValue && toDate.HasValue)
            {
                var start = fromDate.Value.Date;
                var end = toDate.Value.Date;
                if (start > end)
                {
                    (start, end) = (end, start);
                }

                return (start, end);
            }

            var today = DateTime.Today;
            return period switch
            {
                "1M" => (today.AddMonths(-1), today),
                "3M" => (today.AddMonths(-3), today),
                "6M" => (today.AddMonths(-6), today),
                _ => (new DateTime(today.Year, 1, 1), today)
            };
        }

        private static string? ResolveAppointmentStatusKey(
            string? status,
            Dictionary<string, string> statusKeys)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            foreach (var entry in statusKeys)
            {
                if (status.Trim().Equals(entry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Key;
                }
            }

            return null;
        }

        private static SortedDictionary<(int Year, int Month), Dictionary<string, int>> BuildMonthlyBuckets(
            DateTime fromDate,
            DateTime toDate)
        {
            var buckets = new SortedDictionary<(int Year, int Month), Dictionary<string, int>>();
            var cursor = new DateTime(fromDate.Year, fromDate.Month, 1);
            var end = new DateTime(toDate.Year, toDate.Month, 1);

            while (cursor <= end)
            {
                buckets[(cursor.Year, cursor.Month)] = CreateEmptyStatusCounts();
                cursor = cursor.AddMonths(1);
            }

            return buckets;
        }

        private static Dictionary<string, int> CreateEmptyStatusCounts()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["waiting"] = 0,
                ["walkin"] = 0,
                ["notarrived"] = 0,
                ["econsult"] = 0,
                ["remaining"] = 0,
                ["completed"] = 0
            };
        }

        private static PatientStatsBarChartModel BuildBarChartModel(
            SortedDictionary<(int Year, int Month), Dictionary<string, int>> monthlyCounts)
        {
            var barChart = new PatientStatsBarChartModel();

            foreach (var entry in monthlyCounts)
            {
                var monthDate = new DateTime(entry.Key.Year, entry.Key.Month, 1);
                barChart.Months.Add(monthDate.ToString("MMM").ToUpperInvariant());
                barChart.Waiting.Add(entry.Value["waiting"]);
                barChart.WalkIn.Add(entry.Value["walkin"]);
                barChart.NotArrived.Add(entry.Value["notarrived"]);
                barChart.EConsult.Add(entry.Value["econsult"]);
                barChart.Remaining.Add(entry.Value["remaining"]);
                barChart.Completed.Add(entry.Value["completed"]);
            }

            return barChart;
        }

        public async Task<List<PatientExportRowModel>> GetPatientsForExport(long userId, string scope, DateTime? date)
        {
            var doctor = await context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (doctor == null)
            {
                return new List<PatientExportRowModel>();
            }

            var isToday = string.Equals(scope, "today", StringComparison.OrdinalIgnoreCase);
            if (isToday && !date.HasValue)
            {
                return new List<PatientExportRowModel>();
            }

            var targetDate = date?.Date;

            var baseRows = await (
                from caseEntry in context.CaseEntryDetails.AsNoTracking()
                join patient in context.Patients.AsNoTracking() on caseEntry.PatientId equals patient.PatientId
                join state in context.StateMasters.AsNoTracking() on patient.StateId equals state.StateId into stateGroup
                from state in stateGroup.DefaultIfEmpty()
                join country in context.CountryMasters.AsNoTracking() on patient.CountryId equals country.CountryId into countryGroup
                from country in countryGroup.DefaultIfEmpty()
                where caseEntry.DoctorId == doctor.DoctorId && caseEntry.DeleteStatus == false
                select new PatientExportRowModel
                {
                    PatientId = patient.PatientId,
                    CaseId = caseEntry.CaseId,
                    PatientName = patient.PatientName,
                    Address = patient.Address,
                    StateName = state != null ? state.StateName : null,
                    CountryName = country != null ? country.CountryName : null,
                    MobileNo = patient.MobileNo,
                    PhoneNo = patient.PhoneNo,
                    Email = patient.Email,
                    DateOfBirth = patient.DateOfBirth,
                    Gender = patient.Gender,
                    Age = patient.Age,
                    DateOfFirstVisit = caseEntry.DateodFirstVisit,
                    RefBy = caseEntry.RefBy,
                    IsWhatsAppOptIn = patient.IsWhatsAppOptIn,
                    WhatsAppOptInDate = patient.WhatsAppOptInDate,
                    EnteredBy = caseEntry.EnteredBy,
                    EnteredDate = caseEntry.EnteredDate,
                    ChangedBy = caseEntry.ChangedBy,
                    ChangedDate = caseEntry.ChangedDate,
                }
            ).ToListAsync();

            if (isToday && targetDate.HasValue)
            {
                var appointmentRows = await context.PatientAppointments.AsNoTracking()
                    .Where(a =>
                        a.UserId == userId
                        && a.DeleteStatus == false
                        && a.AppointmentDate.HasValue
                        && a.AppointmentDate.Value.Date == targetDate.Value)
                    .Select(a => new
                    {
                        a.PatientId,
                        a.PatientAppId,
                        a.AppointmentDate,
                        a.AppointmentTime,
                        a.Status,
                    })
                    .ToListAsync();

                var appointmentPatientIds = appointmentRows.Select(a => a.PatientId).ToHashSet();
                baseRows = baseRows.Where(r => appointmentPatientIds.Contains(r.PatientId)).ToList();

                var apptByPatient = appointmentRows
                    .GroupBy(a => a.PatientId)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (var row in baseRows)
                {
                    if (apptByPatient.TryGetValue(row.PatientId, out var appt))
                    {
                        row.PatientAppId = appt.PatientAppId;
                        row.AppointmentDate = appt.AppointmentDate;
                        row.AppointmentTime = appt.AppointmentTime;
                        row.AppointmentStatus = appt.Status;
                    }
                }
            }

            if (baseRows.Count == 0)
            {
                return baseRows;
            }

            var caseIds = baseRows.Select(r => r.CaseId).ToList();

            var diagnosisPairs = await (
                from d in context.CaseEntryDiagnoses.AsNoTracking()
                join dm in context.DiagnosisMasters.AsNoTracking() on d.DiagnosisId equals dm.DiagnosisId
                where caseIds.Contains(d.CaseId) && !dm.DeleteStatus
                select new { d.CaseId, dm.DiagnosisName }
            ).ToListAsync();

            var chiefPairs = await context.CaseEntryChiefComplaints.AsNoTracking()
                .Where(c => c.CaseId.HasValue && caseIds.Contains(c.CaseId.Value) && c.ChiefComplaintName != null && c.ChiefComplaintName != "")
                .Select(c => new { CaseId = c.CaseId!.Value, c.ChiefComplaintName })
                .ToListAsync();

            var diagnosisMap = diagnosisPairs
                .GroupBy(x => x.CaseId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.DiagnosisName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct()));

            var chiefMap = chiefPairs
                .GroupBy(x => x.CaseId)
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(", ", g.Select(x => x.ChiefComplaintName).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct()));

            foreach (var row in baseRows)
            {
                row.Diagnosis = diagnosisMap.TryGetValue(row.CaseId, out var diagnosis) ? diagnosis : string.Empty;
                row.ChiefComplaints = chiefMap.TryGetValue(row.CaseId, out var complaints) ? complaints : string.Empty;
            }

            return baseRows.OrderByDescending(r => r.CaseId).ToList();
        }

      
    }
}
