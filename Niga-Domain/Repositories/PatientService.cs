using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using API.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;
using Niga_Domain.Interface;
using Niga_Domain.Master;

namespace Niga_Domain.Implementation
{
    public class PatientService : IPatientService
    {
        NIGACentrumContext context;

        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public PatientService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        public async Task<bool> SaveAllAsync()
        {
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<PatientModel> SavePatient(PatientModel patientModel)
        {
            var errorResponseModel = new ErrorResponseModel();
            var CurrentDate = DateTime.Now;
            if (patientModel.PatientID == 0)
            {
                // Save new patient
                var patientEntity = new Patient();
                patientEntity.PatientName = patientModel.PatientName;
                patientEntity.Address = patientModel.Address;
                patientEntity.StateId = ToNullableFkId(patientModel.StateId);
                patientEntity.CountryId = ToNullableFkId(patientModel.CountryId);
                patientEntity.MobileNo = patientModel.MobileNo;
                patientEntity.Email = patientModel.Email;
                patientEntity.PhoneNo = patientModel.PhoneNo;
                patientEntity.DateOfBirth = patientModel.DateOfBirth;
                patientEntity.Gender = patientModel.Gender;
                patientEntity.DeleteStatus = false;
                patientEntity.IsWhatsAppOptIn = patientModel.IsWhatsAppOptIn;
                patientEntity.WhatsAppOptInDate = patientModel.IsWhatsAppOptIn ? CurrentDate : null;
                if (patientModel.DateOfBirth.HasValue)
                {
                    var today = DateTime.Today;
                    var dob = patientModel.DateOfBirth.Value;
                    var age = today.Year - dob.Year;
                    if (dob.Date > today.AddYears(-age))
                        age--;
                    patientEntity.Age = age;
                }
                else
                {
                    patientEntity.Age = null;
                }
                context.Patients.Add(patientEntity);
                var doctorEntity = context
                    .Doctors.Where(x => x.UserId == patientModel.LoggedInUser)
                    .FirstOrDefault();
                if (doctorEntity != null)
                {
                    var caseEntryDetailsEntity = new CaseEntryDetail();
                    caseEntryDetailsEntity.UserId = patientModel.LoggedInUser;
                    caseEntryDetailsEntity.DoctorId = doctorEntity.DoctorId;
                    caseEntryDetailsEntity.DateodFirstVisit = patientModel.DateodFirstVisit;
                    caseEntryDetailsEntity.RefBy = patientModel.RefBy;
                    caseEntryDetailsEntity.DeleteStatus = false;
                    patientEntity.CaseEntryDetails.Add(caseEntryDetailsEntity);
                }
                if (await SaveAllAsync())
                {
                    patientModel.PatientID = patientEntity.PatientId;
                    patientModel.IsWhatsAppOptIn = patientEntity.IsWhatsAppOptIn;
                    patientModel.WhatsAppOptInDate = patientEntity.WhatsAppOptInDate;
                    patientModel.Message = "Patient Saved Successfully";
                }
            }
            else
            {
                // Update existing patient
                var patientEntity = await context.Patients.FirstOrDefaultAsync(x =>
                    x.PatientId == patientModel.PatientID
                );
                if (patientEntity == null)
                {
                    patientModel.Message = "Patient not found for update";
                    return patientModel;
                }

                patientEntity.PatientName = patientModel.PatientName;
                patientEntity.Address = patientModel.Address;
                patientEntity.StateId = ToNullableFkId(patientModel.StateId);
                patientEntity.CountryId = ToNullableFkId(patientModel.CountryId);
                patientEntity.MobileNo = patientModel.MobileNo;
                patientEntity.Email = patientModel.Email;
                patientEntity.PhoneNo = patientModel.PhoneNo;
                patientEntity.DateOfBirth = patientModel.DateOfBirth;
                patientEntity.Gender = patientModel.Gender;
                patientEntity.ChangedBy = patientModel.ChangedBy;
                patientEntity.ChangedDate = CurrentDate;
                if (patientModel.IsWhatsAppOptIn && !patientEntity.IsWhatsAppOptIn)
                {
                    patientEntity.WhatsAppOptInDate = CurrentDate;
                }
                else if (!patientModel.IsWhatsAppOptIn)
                {
                    patientEntity.WhatsAppOptInDate = null;
                }

                patientEntity.IsWhatsAppOptIn = patientModel.IsWhatsAppOptIn;
                if (patientModel.DateOfBirth.HasValue)
                {
                    var today = DateTime.Today;
                    var dob = patientModel.DateOfBirth.Value;
                    var age = today.Year - dob.Year;
                    if (dob.Date > today.AddYears(-age))
                        age--;
                    patientEntity.Age = age;
                }
                else
                {
                    patientEntity.Age = null;
                }

                if (patientModel.CaseId > 0)
                {
                    var caseEntryDetailsEntity = await context.CaseEntryDetails.FirstOrDefaultAsync(
                        x =>
                            x.PatientId == patientModel.PatientID
                            && x.CaseId == patientModel.CaseId
                    );
                    if (caseEntryDetailsEntity != null)
                    {
                        caseEntryDetailsEntity.DateodFirstVisit = patientModel.DateodFirstVisit;
                        caseEntryDetailsEntity.RefBy = patientModel.RefBy;
                        caseEntryDetailsEntity.ChangedBy = patientModel.ChangedBy
                            ?? patientModel.EnteredBy;
                        caseEntryDetailsEntity.ChangedDate = CurrentDate;
                    }
                }

                try
                {
                    await context.SaveChangesAsync();
                    patientModel.IsWhatsAppOptIn = patientEntity.IsWhatsAppOptIn;
                    patientModel.WhatsAppOptInDate = patientEntity.WhatsAppOptInDate;
                    patientModel.Message = "Patient Updated Successfully";
                }
                catch (DbUpdateException ex)
                {
                    patientModel.Message =
                        ex.InnerException?.Message ?? ex.Message;
                }
            }
            return patientModel;
        }

        /// <summary>Treats 0 as null for optional FK columns (State, Country).</summary>
        private static int? ToNullableFkId(int? value) => value is > 0 ? value : null;

        /// <summary>
        /// Method inpmplementaion for get all the cases.
        /// </summary>
        /// <param name="DoctorId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<PagedList<PatientModel>> GetCases(ParameterParams parameter)
        {
            var errorResponseModel = new ErrorResponseModel();

            // var doctorEntity = context.Doctors.FirstOrDefault(x => x.UserId == parameter.UserId);

            // if (doctorEntity == null)
            // {
            //     errorResponseModel.StatusCode = HttpStatusCode.NotFound;
            //     errorResponseModel.Message = "Doctor not found";
            //     return null;
            // }

            // Single LINQ query – no Include, no foreach
            var patientModelList =
                from c in context.CaseEntryDetails
                join p in context.Patients on c.PatientId equals p.PatientId
                join d in context.Doctors on c.DoctorId equals d.DoctorId
                where c.DoctorId == parameter.UserId && c.DeleteStatus == false
                orderby c.PatientId descending
                select new PatientModel
                {
                    DoctorID = c.DoctorId,
                    PatientID = c.PatientId,
                    PatientName = p.PatientName,
                    MobileNo = p.MobileNo,
                    Email = p.Email,
                    UserId = (int)c.UserId,
                    DateodFirstVisit = c.DateodFirstVisit,
                    Gender = p.Gender,
                    Address = p.Address,
                    DateOfBirth = p.DateOfBirth,
                    CaseId = c.CaseId,
                    EnteredDate = c.EnteredDate,
                    Age = p.Age,
                    IsWhatsAppOptIn = p.IsWhatsAppOptIn,
                    WhatsAppOptInDate = p.WhatsAppOptInDate,
                    DiagnosisIds = string.Join(
                        ",",
                        (
                            from cd in context.CaseEntryDiagnoses
                            join dm in context.DiagnosisMasters
                                on cd.DiagnosisId equals dm.DiagnosisId
                                into diagGroup
                            from dm in diagGroup.DefaultIfEmpty()
                            where cd.CaseId == c.CaseId && (dm == null || dm.DeleteStatus == false)
                            select dm.DiagnosisName ?? string.Empty
                        )
                            .Where(name => !string.IsNullOrEmpty(name))
                            .ToList()
                    ),
                };

            if (patientModelList == null || !patientModelList.Any())
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Not found";
            }

            if (!string.IsNullOrEmpty(parameter.search))
            {
                var search = parameter.search;
                patientModelList = patientModelList.Where(x =>
                    (
                        !string.IsNullOrEmpty(x.PatientName)
                        && x.PatientName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !string.IsNullOrEmpty(x.MobileNo)
                        && x.MobileNo.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !string.IsNullOrEmpty(x.Address)
                        && x.Address.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    || (
                        !string.IsNullOrEmpty(x.DiagnosisIds)
                        && x.DiagnosisIds.Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                );
            }

            // return await PagedList<PatientModel>.CreateAsync(
            //     patientModelList.AsQueryable(),
            //     parameter.PageNumber,
            //     parameter.PageSize
            // );

            return await PagedList<PatientModel>.CreateAsync(patientModelList.AsNoTracking(), parameter.PageNumber,
                parameter.PageSize);

        }

        /// <summary>
        /// Method implementation for patient details
        /// </summary>
        /// <param name="PatientID"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public PatientModel GetPatientDetails(
            long PatientID,
            long caseId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var patientModelList = new PatientModel();
            errorResponseModel = new ErrorResponseModel();
            var patientEntity = (
                from p in context.Patients
                join c in context.CaseEntryDetails on p.PatientId equals c.PatientId
                where p.PatientId == PatientID && c.CaseId == caseId
                select new PatientModel
                {
                    CaseId = c.CaseId,
                    UserId = c.UserId,
                    DoctorID = c.DoctorId,
                    PatientID = p.PatientId,
                    PatientName = p.PatientName,
                    Address = p.Address,
                    StateId = p.StateId,
                    CountryId = p.CountryId,
                    MobileNo = p.MobileNo,
                    Email = p.Email,
                    PhoneNo = p.PhoneNo,
                    DateOfBirth = p.DateOfBirth,
                    Gender = p.Gender,
                    EnteredBy = p.EnteredBy,
                    EnteredDate = p.EnteredDate,
                    ChangedBy = p.ChangedBy,
                    ChangedDate = p.ChangedDate,
                    DeleteStatus = p.DeleteStatus,
                    IsWhatsAppOptIn = p.IsWhatsAppOptIn,
                    WhatsAppOptInDate = p.WhatsAppOptInDate,
                }
            ).FirstOrDefault();
            //context.Patient.Where(x => x.PatientId == PatientID).FirstOrDefault();
            if (patientEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "patient not found";
                return null;
            }
            return patientEntity;
        }

        /// <summary>
        /// Method implementation for Saving new Complaints.
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public string SaveComplaints(
            PatientModel patient,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string Message = "";
            var CaseEntry = context
                .CaseEntryDetails.Where(x => x.PatientId == patient.PatientID)
                .FirstOrDefault();
            foreach (var item in patient.ChiefComplaintIds.Split(','))
            {
                var caseEntryChiefComplaint = new CaseEntryChiefComplaint();
                caseEntryChiefComplaint.ChiefComplaintName = item;
                caseEntryChiefComplaint.CaseId = CaseEntry.CaseId;
                context.CaseEntryChiefComplaints.Add(caseEntryChiefComplaint);
                context.SaveChanges();
                Message = "Complaints Saved Successfully";
            }
            return Message;
        }

        /// <summary>
        /// Methood to get GetPatientDetails by patientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public GetPatientDetailsById GetPatientDetailsById(
            long patientId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            errorResponseModel = new ErrorResponseModel();
            var patientEntity = context
                .Patients.Where(x => x.PatientId == patientId)
                .FirstOrDefault();
            if (patientEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Patient not found";
            }
            return new GetPatientDetailsById
            {
                PatientID = patientEntity.PatientId,
                PatientName = patientEntity.PatientName,
                IsWhatsAppOptIn = patientEntity.IsWhatsAppOptIn,
                WhatsAppOptInDate = patientEntity.WhatsAppOptInDate,
            };
        }

        public string Deletepatient(int patientId, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var PatientEntity = context.Patients.FirstOrDefault(x => x.PatientId == patientId);
            var CaseEntryDetails = context.CaseEntryDetails.FirstOrDefault(x =>
                x.PatientId == patientId
            );
            if (PatientEntity != null)
            {
                PatientEntity.DeleteStatus = true;
                context.SaveChanges();
                CaseEntryDetails.DeleteStatus = true;
                context.SaveChanges();

                Message = " Patient Delete Successfully";
            }
            return Message;
        }

        public async Task<PagedList<PatientModel>> getAllCases(ParameterParams parameterParams)
        {
            var errorResponseModel = new ErrorResponseModel();
            var patientModelQuery = (
                from caseEntry in context.CaseEntryDetails
                join doctor in context.Doctors on caseEntry.DoctorId equals doctor.DoctorId
                join patient in context.Patients on caseEntry.PatientId equals patient.PatientId
                join diagnosis in context.CaseEntryDiagnoses
                    on caseEntry.CaseId equals diagnosis.CaseId
                    into diagnosisGroup
                where doctor.UserId == parameterParams.UserId && caseEntry.DeleteStatus == false
                orderby caseEntry.PatientId descending
                select new PatientModel
                {
                    DoctorID = caseEntry.DoctorId,
                    PatientID = caseEntry.PatientId,
                    PatientName = patient.PatientName,
                    MobileNo = patient.MobileNo,
                    UserId = (int)caseEntry.UserId,
                    DateodFirstVisit = caseEntry.DateodFirstVisit,
                    Gender = patient.Gender,
                    Address = patient.Address,
                    DateOfBirth = patient.DateOfBirth,
                    CaseId = caseEntry.CaseId,
                    EnteredDate = caseEntry.EnteredDate,
                    IsWhatsAppOptIn = patient.IsWhatsAppOptIn,
                    WhatsAppOptInDate = patient.WhatsAppOptInDate,
                    DiagnosisIds = string.Join(
                        ',',
                        diagnosisGroup
                            .Join(
                                context.DiagnosisMasters,
                                d => d.DiagnosisId,
                                dm => dm.DiagnosisId,
                                (d, dm) => new { dm.DiagnosisName, dm.DeleteStatus }
                            )
                            .Where(x => !x.DeleteStatus)
                            .Select(x => x.DiagnosisName)
                    ),
                }
            ).AsQueryable();
            if (!string.IsNullOrEmpty(parameterParams.Flag) && parameterParams.Flag == "Today")
            {
                patientModelQuery = patientModelQuery.Where(x =>
                    x.EnteredDate.Value.Date == DateTime.Now.Date
                );
            }
            if (!patientModelQuery.Any())
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Not found";
            }
            return await PagedList<PatientModel>.CreateAsync(
                patientModelQuery.AsNoTracking(),
                parameterParams.PageNumber,
                parameterParams.PageSize
            );
        }

        public async Task<List<PatientModel>> getAllCasesForExport(ParameterParams parameterParams)
        {
            var errorResponseModel = new ErrorResponseModel();
            var patientModelQuery = await (
                from caseEntry in context.CaseEntryDetails
                join doctor in context.Doctors on caseEntry.DoctorId equals doctor.DoctorId
                join patient in context.Patients on caseEntry.PatientId equals patient.PatientId
                join diagnosis in context.CaseEntryDiagnoses
                    on caseEntry.CaseId equals diagnosis.CaseId
                    into diagnosisGroup
                where doctor.UserId == parameterParams.UserId && caseEntry.DeleteStatus == false
                orderby caseEntry.PatientId descending
                select new PatientModel
                {
                    DoctorID = caseEntry.DoctorId,
                    PatientID = caseEntry.PatientId,
                    PatientName = patient.PatientName,
                    MobileNo = patient.MobileNo,
                    UserId = (int)caseEntry.UserId,
                    DateodFirstVisit = caseEntry.DateodFirstVisit,
                    Gender = patient.Gender,
                    Address = patient.Address,
                    DateOfBirth = patient.DateOfBirth,
                    CaseId = caseEntry.CaseId,
                    EnteredDate = caseEntry.EnteredDate,
                    IsWhatsAppOptIn = patient.IsWhatsAppOptIn,
                    WhatsAppOptInDate = patient.WhatsAppOptInDate,
                    DiagnosisIds = string.Join(
                        ',',
                        diagnosisGroup
                            .Join(
                                context.DiagnosisMasters,
                                d => d.DiagnosisId,
                                dm => dm.DiagnosisId,
                                (d, dm) => new { dm.DiagnosisName, dm.DeleteStatus }
                            )
                            .Where(x => !x.DeleteStatus)
                            .Select(x => x.DiagnosisName)
                    ),
                }
            ).ToListAsync();
            if (!string.IsNullOrEmpty(parameterParams.Flag) && parameterParams.Flag == "Today")
            {
                patientModelQuery = patientModelQuery
                    .Where(x => x.EnteredDate.Value.Date == DateTime.Now.Date)
                    .ToList();
            }
            if (!patientModelQuery.Any())
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Not found";
            }
            return patientModelQuery;
        }

        public string SaveCaseDetails(
            List<CaseDetailsModel> casedetailsModel,
            ref ErrorResponseModel errorResponseModel
        )
        {
            string Message = "";
            var existingDetails = context
                .CaseDetails.Where(x => x.CaseId == casedetailsModel[0].CaseId)
                .ToList();

            //foreach (var item in casedetailsModel)
            // {
            //     var caseDetailsEntity = new CaseDetails();
            //    // caseDetailsEntity.CaseDetailId = item.CaseDetailId;
            //     caseDetailsEntity.SubsectionId = item.SubsectionId;
            //     caseDetailsEntity.CaseId = item.CaseId;
            //     caseDetailsEntity.IntensityId = item.IntensityId;
            //     caseDetailsEntity.RemedyCount = item.RemedyCount;
            //     context.CaseDetails.Add(caseDetailsEntity);
            //     context.SaveChanges();

            // }

            foreach (var item in casedetailsModel)
            {
                var caseDetailsEntity = new CaseDetail();

                if (item.CaseDetailId == 0)
                {
                    // var caseDetailsEntity = new CaseDetails();
                    caseDetailsEntity.CaseDetailId = item.CaseDetailId;
                    caseDetailsEntity.SubsectionId = item.SubsectionId;
                    caseDetailsEntity.CaseId = item.CaseId;
                    caseDetailsEntity.IntensityId = item.IntensityId;
                    caseDetailsEntity.RemedyCount = item.RemedyCount;
                    context.CaseDetails.Add(caseDetailsEntity);
                    context.SaveChanges();
                }
                if (casedetailsModel.IndexOf(item) == casedetailsModel.Count - 1)
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
        public List<PatientAppointmentModel1> GetPatientBackHostoryById(
            long patientId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            errorResponseModel = new ErrorResponseModel();
            var patienappointmentmodel = new List<PatientAppointmentModel1>();
            var patientEntity = (
                from patientAppointment in context.PatientAppointments
                join patient in context.Patients on patientAppointment.PatientId equals patient.PatientId
                join caseEntryDetail in context.CaseEntryDetails
                    on patientAppointment.PatientId equals caseEntryDetail.PatientId
                join AHN in context.AppointmentHistoryNotes
                    on patientAppointment.PatientAppId equals AHN.AppointmentId
                    into AHNGroup
                from AHN in AHNGroup.DefaultIfEmpty()
                where patientAppointment.PatientId == patientId
                select new PatientAppointmentModel1
                {
                    PatientAppId = patientAppointment.PatientAppId,
                    PatientId = patientAppointment.PatientId,
                    AppointmentDate = patientAppointment.AppointmentDate,
                    AppointmentTime = patientAppointment.AppointmentTime.Value.ToTimeSpan(),
                    Status = patientAppointment.Status,
                    UserId = patientAppointment.UserId,
                    DoctorId = patientAppointment.DoctorId,
                    CaseId = caseEntryDetail.CaseId,
                    HistoryNoteId = AHN != null ? AHN.HistoryId : 0,
                    IsWhatsAppOptIn = patient.IsWhatsAppOptIn,
                    WhatsAppOptInDate = patient.WhatsAppOptInDate,
                }
            ).ToList();

            if (patientEntity.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Patient Back Hostory not found";
            }

            return patientEntity;
        }

        public byte[] GetPatientImportTemplate(string format)
        {
            return string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase)
                ? PatientImportFileHelper.BuildTemplateCsv()
                : PatientImportFileHelper.BuildTemplateExcel();
        }

        public async Task<PatientImportResultModel> ImportPatientsAsync(IFormFile file, long userId, string userName)
        {
            var result = new PatientImportResultModel();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            var doctor = await context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (doctor == null)
            {
                throw new InvalidOperationException("Doctor not found.");
            }

            var parsedRows = await PatientImportFileHelper.ParseImportFileAsync(file);
            result.TotalRows = parsedRows.Count;
            if (parsedRows.Count == 0)
            {
                return result;
            }

            var countries = await context.CountryMasters.AsNoTracking()
                .Where(x => !x.DeleteStatus)
                .ToListAsync();
            var states = await context.StateMasters.AsNoTracking()
                .Where(x => !x.DeleteStatus)
                .ToListAsync();

            var countriesByName = PatientImportValidator.BuildCountryNameMap(countries);
            var countryNamesById = PatientImportValidator.BuildCountryIdNameMap(countries);
            var statesByName = PatientImportValidator.BuildStateNameMap(states);
            var stateCountryById = PatientImportValidator.BuildStateCountryMap(states);

            var existingMobiles = await (
                from caseEntry in context.CaseEntryDetails.AsNoTracking()
                join patient in context.Patients.AsNoTracking() on caseEntry.PatientId equals patient.PatientId
                where caseEntry.DoctorId == doctor.DoctorId
                    && caseEntry.DeleteStatus == false
                    && patient.MobileNo != null
                select patient.MobileNo!
            ).Distinct().ToListAsync();

            var existingDoctorMobiles = new HashSet<string>(existingMobiles, StringComparer.OrdinalIgnoreCase);
            var fileMobiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var skippedRows = new List<PatientImportRowModel>();
            var validRows = new List<ValidatedPatientImportRow>();

            foreach (var row in parsedRows)
            {
                if (!PatientImportValidator.TryValidateRow(
                        row,
                        countriesByName,
                        countryNamesById,
                        statesByName,
                        stateCountryById,
                        existingDoctorMobiles,
                        fileMobiles,
                        out var validated,
                        out var error))
                {
                    row.SkipReason = error;
                    skippedRows.Add(row);
                    result.Errors.Add(new PatientImportErrorModel
                    {
                        RowNumber = row.RowNumber,
                        Message = error,
                    });
                    continue;
                }

                validRows.Add(validated!);
            }

            const int batchSize = 200;
            var now = DateTime.Now;
            var enteredBy = string.IsNullOrWhiteSpace(userName) ? "IMPORT" : userName.Trim();

            for (var offset = 0; offset < validRows.Count; offset += batchSize)
            {
                var batch = validRows.Skip(offset).Take(batchSize).ToList();
                var patients = new List<Patient>();
                var appointmentPairs = new List<(Patient patient, ValidatedPatientImportRow row)>();

                foreach (var item in batch)
                {
                    var age = CalculateAge(item.DateOfBirth, now);
                    var patient = new Patient
                    {
                        PatientName = item.PatientName,
                        Address = item.Address,
                        StateId = item.StateId,
                        CountryId = item.CountryId,
                        MobileNo = item.MobileNo,
                        PhoneNo = item.PhoneNo,
                        Email = item.Email,
                        DateOfBirth = item.DateOfBirth,
                        Gender = item.Gender,
                        DeleteStatus = false,
                        IsWhatsAppOptIn = item.IsWhatsAppOptIn,
                        WhatsAppOptInDate = item.IsWhatsAppOptIn ? now : null,
                        Age = age,
                        EnteredBy = enteredBy,
                        EnteredDate = now,
                    };

                    var caseEntry = new CaseEntryDetail
                    {
                        UserId = (int)userId,
                        DoctorId = doctor.DoctorId,
                        DateodFirstVisit = now,
                        RefBy = item.RefBy,
                        DeleteStatus = false,
                        EnteredBy = enteredBy,
                        EnteredDate = now,
                    };

                    patient.CaseEntryDetails.Add(caseEntry);
                    patients.Add(patient);

                    if (item.AppointmentDate.HasValue)
                    {
                        appointmentPairs.Add((patient, item));
                    }
                }

                context.Patients.AddRange(patients);
                await context.SaveChangesAsync();
                result.SuccessCount += batch.Count;

                if (appointmentPairs.Count > 0)
                {
                    result.HasAppointments = true;
                    foreach (var (patient, item) in appointmentPairs)
                    {
                        context.PatientAppointments.Add(new PatientAppointment
                        {
                            PatientId = patient.PatientId,
                            UserId = userId,
                            DoctorId = doctor.DoctorId,
                            AppointmentDate = item.AppointmentDate,
                            AppointmentTime = item.AppointmentTime,
                            Status = "WAITING",
                            DeleteStatus = false,
                        });
                    }

                    await context.SaveChangesAsync();
                }

                foreach (var item in batch)
                {
                    existingDoctorMobiles.Add(item.MobileNo);
                }
            }

            result.FailedCount = skippedRows.Count;

            if (skippedRows.Count > 0)
            {
                var skippedBytes = PatientImportFileHelper.BuildSkippedRowsFile(skippedRows, extension);
                result.SkippedFile = new PatientImportSkippedFileModel
                {
                    FileName = PatientImportFileHelper.BuildSkippedFileName(extension),
                    ContentType = PatientImportFileHelper.GetSkippedFileContentType(extension),
                    ContentBase64 = Convert.ToBase64String(skippedBytes),
                };
            }

            return result;
        }

        private static int? CalculateAge(DateTime dateOfBirth, DateTime today)
        {
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
            {
                age--;
            }

            return age;
        }
    }
}
