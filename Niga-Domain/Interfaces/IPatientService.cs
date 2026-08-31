using System;
using System.Collections.Generic;
using System.Text;
using API.Helpers;
using Microsoft.AspNetCore.Http;
using Niga_Domain.DTOs;
using Niga_Domain.Helpers;

namespace Niga_Domain.Interface
{
    /// <summary>
    /// Interface for patient.
    /// </summary>
    public interface IPatientService
    {
        /// <summary>
        /// Method declarations for Saving new Patient.
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        Task<PatientModel> SavePatient(PatientModel patient);

        /// <summary>
        /// Method declaration for get all the GetCases
        /// </summary>
        /// <param name="DoctorId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        Task<PagedList<PatientModel>> GetCases(ParameterParams parameter);
        Task<PagedList<PatientModel>> getAllCases(ParameterParams parameterParams);
        Task<List<PatientModel>> getAllCasesForExport(ParameterParams parameterParams);

        /// <summary>
        /// Method declaration for patient details
        /// </summary>
        /// <param name="DoctorId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
      PatientModel GetPatientDetails(long PatientID, long CaseId,ref ErrorResponseModel errorResponseModel);



        /// <summary>
        /// Method is used for to get Patient by PatientId
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        GetPatientDetailsById GetPatientDetailsById(long patientId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        /// Method declarations for Saving new Complaints.
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string SaveComplaints(PatientModel patient, ref ErrorResponseModel errorResponseModel);


        /// <summary>
                /// Interface is used to deactivate patient.
                /// </summary>
                /// <param name="patientId"></param>
                /// <param name="errorResponseModel"></param>
                /// <returns></returns>
        string Deletepatient(int patientId, ref ErrorResponseModel errorResponseModel);

         string SaveCaseDetails(List<CaseDetailsModel> casedetailsModel, ref ErrorResponseModel errorResponseModel);

        List<PatientAppointmentModel1> GetPatientBackHostoryById(long patientId, ref ErrorResponseModel errorResponseModel);
        Task<bool> SaveAllAsync();

        byte[] GetPatientImportTemplate(string format);
        Task<PatientImportResultModel> ImportPatientsAsync(IFormFile file, long userId, string userName);

    }
}
