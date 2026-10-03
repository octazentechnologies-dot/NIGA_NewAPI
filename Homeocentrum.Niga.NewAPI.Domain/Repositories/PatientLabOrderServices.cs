using System.Net;
using Homeocentrum.Niga.NewAPI.Domain.Business.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Business.Implementation
{
    public class PatientLabOrderServices : IPatientLabOrderServices
    {
        private readonly NIGACentrumContext context;

        public PatientLabOrderServices(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public async Task<string> SavePatinetLabOrder(PatientLabOrderModel patientLabOrderModel)
        {
            if (patientLabOrderModel.PatientOrderedTestId == 0)
            {
                context.PatientLabOrders.Add(new PatientLabOrder
                {
                    PatientId = patientLabOrderModel.PatientId,
                    PatientLabTestId = patientLabOrderModel.PatientLabTestId,
                    OrderDate = patientLabOrderModel.OrderDate ?? DateTime.Now,
                    LabName = patientLabOrderModel.LabName,
                    DeleteStatus = false
                });
            }
            else
            {
                var row = context.PatientLabOrders.FirstOrDefault(x =>
                    x.PatientOrderedTestId == patientLabOrderModel.PatientOrderedTestId && !x.DeleteStatus);
                if (row == null)
                    return "Not found";
                row.PatientId = patientLabOrderModel.PatientId;
                row.PatientLabTestId = patientLabOrderModel.PatientLabTestId;
                row.OrderDate = patientLabOrderModel.OrderDate;
                row.LabName = patientLabOrderModel.LabName;
            }

            return await SaveAllAsync() ? "Record saved successfully" : "Something went wrong";
        }

        public List<PatientLabOrderModel> GetAllPatinetLabOrder(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var rows = Map(context.PatientLabOrders.Where(x => !x.DeleteStatus)).ToList();
            if (rows.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Lab order not found";
            }
            return rows;
        }

        public List<PatientLabOrderModel> GetPatinetLabOrder(int patientId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var rows = Map(context.PatientLabOrders.Where(x => !x.DeleteStatus && x.PatientId == patientId)).ToList();
            if (rows.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Lab order not found";
            }
            return rows;
        }

        private IQueryable<PatientLabOrderModel> Map(IQueryable<PatientLabOrder> query)
        {
            return from order in query
                   join test in context.PatientLabTestMasters on order.PatientLabTestId equals test.PatientLabTestId into tests
                   from test in tests.DefaultIfEmpty()
                   select new PatientLabOrderModel
                   {
                       PatientOrderedTestId = order.PatientOrderedTestId,
                       PatientId = order.PatientId,
                       PatientLabTestId = order.PatientLabTestId,
                       PatientLabTestName = test.LabTestName,
                       OrderDate = order.OrderDate,
                       LabName = order.LabName,
                       DeleteStatus = order.DeleteStatus
                   };
        }
    }
}
