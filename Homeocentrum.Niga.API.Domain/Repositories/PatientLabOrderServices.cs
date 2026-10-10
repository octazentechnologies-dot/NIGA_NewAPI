using System.Net;
using Homeocentrum.Niga.API.Domain.Business.Interface;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Business.Implementation
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
            var userId = patientLabOrderModel.UserId > 0 ? patientLabOrderModel.UserId : patientLabOrderModel.EnteredBy;
            PatientLabOrder row;
            var isNew = patientLabOrderModel.PatientOrderedTestId == 0;
            if (isNew)
            {
                row = new PatientLabOrder
                {
                    PatientId = patientLabOrderModel.PatientId,
                    PatientLabTestId = patientLabOrderModel.PatientLabTestId,
                    OrderDate = patientLabOrderModel.OrderDate ?? DateTime.Now,
                    LabName = patientLabOrderModel.LabName,
                    DeleteStatus = false
                };
                context.PatientLabOrders.Add(row);
            }
            else
            {
                row = context.PatientLabOrders.FirstOrDefault(x =>
                    x.PatientOrderedTestId == patientLabOrderModel.PatientOrderedTestId && !x.DeleteStatus);
                if (row == null)
                    return "Not found";
                row.PatientId = patientLabOrderModel.PatientId;
                row.PatientLabTestId = patientLabOrderModel.PatientLabTestId;
                row.OrderDate = patientLabOrderModel.OrderDate;
                row.LabName = patientLabOrderModel.LabName;
            }

            if (!await SaveAllAsync())
                return "Something went wrong";

            // EnteredBy/ChangedBy are int columns that the EF model ignores, so they are written directly.
            if (userId > 0)
            {
                if (isNew)
                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE dbo.PatientLabOrder SET EnteredBy = {userId} WHERE PatientOrderedTestId = {row.PatientOrderedTestId}");
                else
                    await context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE dbo.PatientLabOrder SET ChangedBy = {userId} WHERE PatientOrderedTestId = {row.PatientOrderedTestId}");
            }
            return "Record saved successfully";
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
                   } into model
                   orderby model.PatientOrderedTestId descending
                   select model;
        }
    }
}
