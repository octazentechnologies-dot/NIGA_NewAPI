using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Business.Interface
{
    /// <summary>
    /// Interface for PatientLabOrder Actions
    /// </summary>
   public interface IPatientLabOrderServices
    {
        Task<string> SavePatinetLabOrder(PatientLabOrderModel patientLabOrderModel); 
       Task<bool> SaveAllAsync();
        List<PatientLabOrderModel> GetAllPatinetLabOrder(ref ErrorResponseModel errorResponseModel);
        List<PatientLabOrderModel> GetPatinetLabOrder(int PatientId, ref ErrorResponseModel errorResponseModel);
    }
}
