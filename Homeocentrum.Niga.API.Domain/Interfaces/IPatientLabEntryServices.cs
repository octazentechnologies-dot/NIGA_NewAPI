using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    public interface IPatientLabEntryServices
    {
        
        Task<string> SavePatientLabEntry(PatientLabEntryModel patientLabEntryModel);
        List<PatientLabEntryModel> GetAllPatientLabEntry(ref ErrorResponseModel errorResponseModel);
        List<PatientLabEntryModel> GetPatientLabEntry(int PatientId, ref ErrorResponseModel errorResponseModel);
        Task<bool> SaveAllAsync();
    }
}
