using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;

namespace Niga_Domain.Business.Implementation
{
    /// <summary>
    /// Implementation for PatientLabEntryServices
    /// </summary>
    public class PatientLabEntryServices : IPatientLabEntryServices
    {
        NIGACentrumContext context;

        /// <summary>
        /// Constructor
        /// </summary>
        public PatientLabEntryServices(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Mthod implementation implementaion for SavePatinetLabOrder
        /// </summary>
        /// <param name="patientLabOrderModel"></param>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public async Task<string> SavePatientLabEntry(PatientLabEntryModel patientLabEntryModel)
        {
            var errorResponseModel = new ErrorResponseModel();
            string Mesaage = "";
            if (patientLabEntryModel.PatientLabId == 0)
            {
                var patientLabEntryEntity = new PatientLabEntry();
                patientLabEntryEntity.PatientId = patientLabEntryModel.PatientId;
                patientLabEntryEntity.PatientLabTestId = patientLabEntryModel.PatientLabTestId;
                patientLabEntryEntity.LabDate = patientLabEntryModel.LabDate;
                patientLabEntryEntity.ParameterName = patientLabEntryModel.ParameterName;
                patientLabEntryEntity.ParameterValue = patientLabEntryModel.ParameterValue;
                patientLabEntryEntity.DeleteStatus = false;
                if (await SaveAllAsync())
                {
                    Mesaage = "Record saved successfully";
                }
                else
                {
                    Mesaage = "Something went wrong";
                
                }
            }
            else
            {
                var patientLabEntryEntity = context.PatientLabEntries.FirstOrDefault(x =>
                    x.PatientLabId == patientLabEntryModel.PatientLabId
                );
                if (patientLabEntryEntity == null)
                {
                    Mesaage = "Not found";
                    errorResponseModel.Message = "Not found";
                    errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                }
                patientLabEntryEntity.PatientId = patientLabEntryModel.PatientId;
                patientLabEntryEntity.PatientLabTestId = patientLabEntryModel.PatientLabTestId;
                patientLabEntryEntity.LabDate = patientLabEntryModel.LabDate;
                patientLabEntryEntity.ParameterName = patientLabEntryModel.ParameterName;
                patientLabEntryEntity.ParameterValue = patientLabEntryModel.ParameterValue;
                patientLabEntryEntity.DeleteStatus = false;
                if (await SaveAllAsync())
                {
                    Mesaage = "Record updated successfully";
                }
                else
                {
                    Mesaage = "Something went wrong";
                }
            }
            return Mesaage;
        }

        /// <summary>
        ///  Method implementation for GetAllPatinetLabOrder
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<PatientLabEntryModel> GetAllPatientLabEntry(
            ref ErrorResponseModel errorResponseModel
        )
        {
            var patientLabEntryModelList = new List<PatientLabEntryModel>();
            errorResponseModel = new ErrorResponseModel();
            var patientLabEntities = context
                .PatientLabEntries.Where(x => x.DeleteStatus == false)
                .ToList();
            if (patientLabEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "records not found";
            }
            patientLabEntities.ForEach(item =>
            {
                patientLabEntryModelList.Add(
                    new PatientLabEntryModel
                    {
                        LabDate = item.LabDate,
                        ParameterName = item.ParameterName,
                        ParameterValue = item.ParameterValue,
                        PatientId = item.PatientId,
                        PatientLabId = item.PatientLabId,
                        PatientLabTestId = item.PatientLabTestId,
                    }
                );
            });
            return patientLabEntryModelList.OrderByDescending(x => x.PatientLabId).ToList();
        }

        /// <summary>
        ///  Method implementation for GetAllPatinetLabOrder
        /// </summary>
        /// <param name="errorResponseModel"></param>
        /// <returns></returns>
        public List<PatientLabEntryModel> GetPatientLabEntry(
            int PatientId,
            ref ErrorResponseModel errorResponseModel
        )
        {
            var patientLabEntryModelList = new List<PatientLabEntryModel>();
            errorResponseModel = new ErrorResponseModel();
            var patientLabEntities = (
                from labEntry in context.PatientLabEntries
                join labTest in context.PatientLabTestMasters
                    on labEntry.PatientLabTestId equals labTest.PatientLabTestId
                where labEntry.PatientId == PatientId && labEntry.DeleteStatus == false
                select new PatientLabEntryModel
                {
                    LabDate = labEntry.LabDate,
                    ParameterName = labEntry.ParameterName,
                    ParameterValue = labEntry.ParameterValue,
                    PatientId = labEntry.PatientId,
                    PatientLabId = labEntry.PatientLabId,
                    PatientLabTestId = labEntry.PatientLabTestId,
                    PatientLabTestName = labTest.LabTestName,
                }
            ).ToList();

            if (patientLabEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "records not found";
            }

            return patientLabEntities.OrderByDescending(x => x.PatientLabId).ToList();
        }
    }
}
