using Niga_Domain.Business.Interface;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Master;
using System.Net;

namespace Niga_Domain.Business.Implementation
{
    public class PatientLabTestService : IPatientLabTestService
    {
        NIGACentrumContext context;
        public PatientLabTestService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        public List<PatientLabTestModel> GetPatientLabTests(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var labTestMasterEntities = (from patientLabTest in context.PatientLabTestMasters
                                         where patientLabTest.DeleteStatus == false
                                         select new PatientLabTestModel
                                         {
                                             PatientLabTestId = patientLabTest.PatientLabTestId,
                                             LabTestName = patientLabTest.LabTestName,
                                             Description = patientLabTest.Description,
                                         }
                                         ).ToList();

            if (labTestMasterEntities.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "records not found";
            }

            return labTestMasterEntities;
        }

         public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }
        public string DeletePatientLabTest(int patientLabTestId, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            var labTestEntity = context.PatientLabTestMasters.FirstOrDefault(x => x.PatientLabTestId == patientLabTestId);
            if (labTestEntity != null)
            {
                labTestEntity.DeleteStatus = true;
                context.SaveChanges();
                Message = "Lab Test Deleted Successfully";
            }
            return Message;
        }

        public PatientLabTestModel GetPatientLabTestById(int patientLabTestId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var labTestEntity = context.PatientLabTestMasters.Where(x => x.DeleteStatus == false).FirstOrDefault(x => x.PatientLabTestId == patientLabTestId);
            if (labTestEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Lab test not found";
            }
            return new PatientLabTestModel
            {
                PatientLabTestId = labTestEntity.PatientLabTestId,
                LabTestName = labTestEntity.LabTestName,
                Description = labTestEntity.Description,
            };
        }

        public async Task<string> AddEditPatientLabTest(PatientLabTestModel labTestModel, int userID)
        {
            string Message = "";
            if (labTestModel.PatientLabTestId == 0)
            {
                PatientLabTestMaster labTestEntity = new PatientLabTestMaster();
                labTestEntity.PatientLabTestId = labTestModel.PatientLabTestId;
                labTestEntity.LabTestName = labTestModel.LabTestName;
                labTestEntity.Description = labTestModel.Description;
                labTestEntity.DeleteStatus = false;
                context.PatientLabTestMasters.Add(labTestEntity);
                if (await context.SaveChangesAsync() > 0)
                {
                    Message = "Lab test saved successfully";
                }
            }

            else
            {
                var labTestEntity = context.PatientLabTestMasters.FirstOrDefault(x => x.PatientLabTestId == labTestModel.PatientLabTestId);
                if (labTestEntity != null)
                {
                    labTestEntity.LabTestName = labTestModel.LabTestName;
                    labTestEntity.Description = labTestModel.Description;
                    labTestEntity.DeleteStatus = false;
                    if (await context.SaveChangesAsync() > 0)
                    {
                        Message = "Lab test updated successfully";
                    }
                }
            }
            return Message;
        }
    }
}
