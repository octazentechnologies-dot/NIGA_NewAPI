using System;
using System.Collections.Generic;
using System.Text;
using Niga_Domain.DTOs;

namespace Niga_Domain.Business.Interface
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
