using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
     public interface IDoctorDashBoardService
    {

        /// <summary>
        /// Method is used for to get patient appointment by appointmentDate
        /// </summary>
        /// <param name="appointmentDate"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        DoctorDashBoardModel GetPatientAppCount(long userId, DateTime? appointmentDate, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method is used for to get patient appointment by user
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        Task<PagedList<PatientAppointmentModel>> GetPatientAppUserDate(ParameterParams parameter);

        /// <summary>
        /// Method to get patient statistics grouped by their status
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns>Dictionary containing status counts</returns>
        Task<DoctorDashBoardModel> GetPatientStatusStats(long userId, DateTime? FromDate, DateTime? ToDate, ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Patient stats for dashboard pie and bar charts.
        /// </summary>
        Task<PatientStatsChartsResponseModel> GetPatientStatsCharts(long userId, string period, DateTime? fromDate, DateTime? toDate);

        /// <summary>
        /// Fetch patient rows for dashboard export (one row per case).
        /// </summary>
        Task<List<PatientExportRowModel>> GetPatientsForExport(long userId, string scope, DateTime? date);
    }
}
