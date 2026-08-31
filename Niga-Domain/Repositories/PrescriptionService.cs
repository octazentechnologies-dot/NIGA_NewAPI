using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interfaces;

namespace Niga_Domain.Repositories
{
    /// <summary>
    /// Implementation for prescription rubric and remedy detail operations.
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly NIGACentrumContext _context;

        public PrescriptionService(NIGACentrumContext context)
        {
            _context = context;
        }

        public async Task<bool> AppointmentExistsAsync(int appointmentId)
        {
            return await _context.PatientAppointments
                .AsNoTracking()
                .AnyAsync(x => x.PatientAppId == appointmentId && x.DeleteStatus == false);
        }

        public async Task<PrescriptionDetailsPaginatedResult> GetPrescriptionDetailsByAppointmentIdAsync(
            GetPrescriptionDetailsByAppointmentIdRequest request)
        {
            var paginationRequest = new PaginationRequestModel
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var rubricQuery = BuildRubricDetailsQuery(request.AppointmentId);
            var remedyQuery = BuildRemedyDetailsQuery(request.AppointmentId);

            var paginatedRubrics = await rubricQuery.ToPaginatedResultAsync(paginationRequest);
            var paginatedRemedies = await remedyQuery.ToPaginatedResultAsync(paginationRequest);

            return new PrescriptionDetailsPaginatedResult
            {
                Details = new PrescriptionDetailsByAppointmentModel
                {
                    RubricDetails = paginatedRubrics.Items,
                    RemedyDetails = paginatedRemedies.Items
                },
                PageNumber = paginatedRubrics.PageNumber,
                PageSize = paginatedRubrics.PageSize,
                RubricTotalRecords = paginatedRubrics.TotalRecords,
                RubricTotalPages = paginatedRubrics.TotalPages,
                RemedyTotalRecords = paginatedRemedies.TotalRecords,
                RemedyTotalPages = paginatedRemedies.TotalPages
            };
        }

        private IQueryable<PrescriptionRubricDetailViewModel> BuildRubricDetailsQuery(int appointmentId)
        {
            return (
                from rubric in _context.PrescriptionRubricDetails.AsNoTracking()
                join subSection in _context.SubSectionMasters.AsNoTracking()
                    on rubric.RubricId equals subSection.SubSectionId into subSectionJoin
                from subSection in subSectionJoin.DefaultIfEmpty()
                where rubric.AppointmentId == appointmentId && rubric.DeletedStatus == false
                orderby rubric.CreatedDate
                select new PrescriptionRubricDetailViewModel
                {
                    PrescriptionRubricId = rubric.PrescriptionRubricId,
                    AppointmentId = rubric.AppointmentId,
                    RubricId = rubric.RubricId,
                    RubricName = subSection != null ? subSection.SubSectionName ?? string.Empty : string.Empty,
                    IntensityId = rubric.IntensityId,
                    RemedyCount = rubric.RemedyCount,
                    CreatedDate = rubric.CreatedDate.HasValue
                        ? rubric.CreatedDate.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                        : null
                });
        }

        private IQueryable<PrescriptionRemedyDetailViewModel> BuildRemedyDetailsQuery(int appointmentId)
        {
            return (
                from remedy in _context.PrescriptionRemedyDetails.AsNoTracking()
                join remedyMaster in _context.RemedyMasters.AsNoTracking()
                    on remedy.RemedyId equals remedyMaster.RemedyId into remedyMasterJoin
                from remedyMaster in remedyMasterJoin.DefaultIfEmpty()
                where remedy.AppointmentId == appointmentId && remedy.DeletedStatus == false
                orderby remedy.CreatedDate
                select new PrescriptionRemedyDetailViewModel
                {
                    PrescriptionRemedyId = remedy.PrescriptionRemedyId,
                    AppointmentId = remedy.AppointmentId,
                    RemedyId = remedy.RemedyId,
                    RemedyName = remedyMaster != null ? remedyMaster.RemedyName : string.Empty,
                    Description = remedy.Description ?? string.Empty,
                    Dose = remedy.Dose ?? string.Empty,
                    CreatedDate = remedy.CreatedDate.HasValue
                        ? remedy.CreatedDate.Value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture)
                        : null
                });
        }
    }
}
