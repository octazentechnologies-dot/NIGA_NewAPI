#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Web;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
{
    public class CaseDetailsService : ICaseDetailsService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public CaseDetailsService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
            Homeocentrum.Niga.API.Domain.Data.OldApiCommandTimeout.Apply(context);
        }

        /// <summary>
        /// Method implementation for saving Case details.
        /// </summary>
        /// <param name="casedetailsModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>

        public string SaveCaseDetails(List<CaseDetailsModel> casedetailsModel, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            if (casedetailsModel == null || casedetailsModel.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.BadRequest;
                errorResponseModel.Message = "Case details are required";
                return null;
            }

            string Message = "";
            foreach (var item in casedetailsModel)
            {
                CaseDetail caseDetailsEntity;
                if (item.CaseDetailId == 0)
                {
                    caseDetailsEntity = new CaseDetail
                    {
                        SubsectionId = item.SubsectionId,
                        CaseId = item.CaseId,
                        IntensityId = item.IntensityId,
                        RemedyCount = item.RemedyCount
                    };
                    context.CaseDetails.Add(caseDetailsEntity);
                    context.SaveChanges();
                }
                else
                {
                    caseDetailsEntity = context.CaseDetails.FirstOrDefault(x =>
                        x.CaseDetailId == item.CaseDetailId && x.CaseId == item.CaseId);
                    if (caseDetailsEntity == null)
                    {
                        errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                        errorResponseModel.Message = "Case detail not found";
                        return null;
                    }
                    caseDetailsEntity.SubsectionId = item.SubsectionId;
                    caseDetailsEntity.IntensityId = item.IntensityId;
                    caseDetailsEntity.RemedyCount = item.RemedyCount;
                    context.SaveChanges();
                }

                if (casedetailsModel.IndexOf(item) == casedetailsModel.Count - 1 && item.ModelEx != null)
                {
                    foreach (var item1 in item.ModelEx)
                    {
                        var modeldetails = new CaseDetailRemedy();
                        modeldetails.CaseId = caseDetailsEntity.CaseId;
                        modeldetails.RemedyId = item1.RemedyId;
                        modeldetails.RemedyIndex = item1.RemedyIndex;
                        context.CaseDetailRemedies.Add(modeldetails);
                        context.SaveChanges();
                    }
                }
            }
            Message = "Case Details Saved Successfully";

            return Message;


            //if (existingDetails.Count < 0)
            //{
            //    Message = "Case Details Saved Successfully";
            //}
            //Message = "Case Details Saved Successfully";
            //return Message;
        }

        /// <summary>
        /// Methood to get GetPatientBackHostory by patientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<PatientAppointmentModel1> GetPatientBackHostoryById(long patientId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var patienappointmentmodel = new List<PatientAppointmentModel1>();
            var patientEntity = (from patientAppointment in context.PatientAppointments
                                 join caseEntryDetail in context.CaseEntryDetails on patientAppointment.PatientId equals caseEntryDetail.PatientId
                                 join patient in context.Patients on patientAppointment.PatientId equals patient.PatientId into patientGroup
                                 from patient in patientGroup.DefaultIfEmpty()
                                 join doctor in context.Doctors on patientAppointment.DoctorId equals doctor.DoctorId into doctorGroup
                                 from doctor in doctorGroup.DefaultIfEmpty()
                                 join AHN in context.AppointmentHistoryNotes on patientAppointment.PatientAppId equals AHN.AppointmentId into AHNGroup
                                 from AHN in AHNGroup.DefaultIfEmpty()
                                 where patientAppointment.PatientId == patientId
                                 select new PatientAppointmentModel1
                                 {
                                     PatientAppId = patientAppointment.PatientAppId,
                                     PatientId = patientAppointment.PatientId,
                                     PatientName = patient != null ? patient.PatientName : null,
                                     MobileNo = patient != null ? patient.MobileNo : null,
                                     DoctorName = doctor != null ? ((doctor.FirstName ?? "") + " " + (doctor.LastName ?? "")).Trim() : null,
                                     DeleteStatus = patientAppointment.DeleteStatus,
                                     IsWhatsAppOptIn = patient != null && patient.IsWhatsAppOptIn,
                                     WhatsAppOptInDate = patient != null ? patient.WhatsAppOptInDate : null,
                                     AppointmentDate = patientAppointment.AppointmentDate,
                                     AppointmentTime = patientAppointment.AppointmentTime.HasValue ? patientAppointment.AppointmentTime.Value.ToTimeSpan() : (TimeSpan?)null,
                                     Status = patientAppointment.Status,
                                     UserId = patientAppointment.UserId,
                                     DoctorId = patientAppointment.DoctorId,
                                     CaseId = caseEntryDetail.CaseId,
                                     HistoryNoteId =AHN!=null?AHN.HistoryId:0,
                                     PaymentStatus = patientAppointment.PaymentStatus,
                                     IsTele = patientAppointment.IsTele,
                                     VisitType = patientAppointment.VisitType,
                                     ConsultMode = patientAppointment.ConsultMode,

                                 }).ToList();
            

            if (patientEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Patient Back Hostory not found";
            }
           
            return patientEntity;
        }
    }
}
