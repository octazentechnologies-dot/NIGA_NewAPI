using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Interface
{
    public interface IPatientLabTestService
    {
        /// <summary>
        /// Get All lab test 
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        List<PatientLabTestModel> GetPatientLabTests(ref ErrorResponseModel errorResponseModel);
       Task<bool> SaveAllAsync();
        /// <summary>
        /// Delete lab test
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        string DeletePatientLabTest(int patientLabTestId, ref ErrorResponseModel errorResponseModel);

        /// <summary>
        ///Get lab test details by testId
        /// </summary>
        /// <param name="testId"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        PatientLabTestModel GetPatientLabTestById(int patientLabTestId, ref ErrorResponseModel errorResponseModel);


        /// <summary>
        ///Add/ Edit lab test details
        /// </summary>
        /// <param name="labTestModel"></param>
        /// <param name="userID"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        Task<string> AddEditPatientLabTest(PatientLabTestModel patientLabTestModel, int userID);
    }
}
