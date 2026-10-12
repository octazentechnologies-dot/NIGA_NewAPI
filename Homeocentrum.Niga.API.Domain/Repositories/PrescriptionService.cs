using Homeocentrum.Niga.API.Domain.Business.Interface;
using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;

namespace Homeocentrum.Niga.API.Domain.Repositories
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


        #region Old API compatible methods
#nullable disable

        /// <summary>
        ///  Method implementation for getting Prescription Remedy list
        /// </summary>
        /// <param name="rubricList"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<PrescriptionRemedyViewModel> GetPrescriptionRemedy(List<int?> rubricList, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();

            var remedyEntity = (from rubricRemedy in  _context.RubricRemedyDetails 
                                          join remedy in _context.RemedyMasters on rubricRemedy.RemedyId equals remedy.RemedyId
                                          where rubricList.Contains(rubricRemedy.SubSectionId) && rubricRemedy.DeletedStatus==false
                                          select new PrescriptionRemedyViewModel
                                          {
                                            RemedyId = remedy.RemedyId,
                                            RemedyName = remedy.RemedyName,
                                          }).OrderBy(rem => rem.RemedyId).ToList();

            if (remedyEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "records not found";
            }

            return remedyEntity.GroupBy(x => x.RemedyId).Select(x => x.FirstOrDefault()).ToList();
        }

        /// <summary>
        /// Methood to save prescription Rubric Detail
        /// </summary>
        /// <param name="prescriptionRubricDetail"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        /// 

        public string SavePrescriptionDetail(PrescriptionDetailModel prescriptionDetail, ref ErrorResponseModel errorResponseModel)
        {
            string Mesaage = "";

            foreach (var item in prescriptionDetail.PrescriptionRubricDetailList)
            {
                var prescriptionRubricDetailEntity = new PrescriptionRubricDetail();
                prescriptionRubricDetailEntity.AppointmentId = prescriptionDetail.AppointmentId;
                prescriptionRubricDetailEntity.RubricId = item.RubricId;
                prescriptionRubricDetailEntity.IntensityId = item.IntensityId;
                prescriptionRubricDetailEntity.RemedyCount = item.RemedyCount;
                prescriptionRubricDetailEntity.DeletedStatus = false;
                prescriptionRubricDetailEntity.CreatedDate = DateTime.Now;
                _context.PrescriptionRubricDetails.Add(prescriptionRubricDetailEntity);
                _context.SaveChanges();
            }

            foreach (var item in prescriptionDetail.PrescriptionRemedyDetailList)
            {
                var prescriptionRemedyDetailEntity = new PrescriptionRemedyDetail();
                prescriptionRemedyDetailEntity.AppointmentId = prescriptionDetail.AppointmentId;
                prescriptionRemedyDetailEntity.RemedyId = item.RemedyId;
                prescriptionRemedyDetailEntity.Description = item.Description;
                prescriptionRemedyDetailEntity.Dose = item.Dose;
                prescriptionRemedyDetailEntity.PotencyId = item.PotencyId;
                prescriptionRemedyDetailEntity.Frequency = item.Frequency;
                prescriptionRemedyDetailEntity.Duration = item.Duration;
                prescriptionRemedyDetailEntity.Instructions = item.Instructions;
                prescriptionRemedyDetailEntity.DeletedStatus = false;
                prescriptionRemedyDetailEntity.CreatedDate = DateTime.Now;
                _context.PrescriptionRemedyDetails.Add(prescriptionRemedyDetailEntity);
                _context.SaveChanges();
            }

            Mesaage = "Record Saved Successfully";
            
            return Mesaage;
        }

#nullable restore
        #endregion
    }
}
