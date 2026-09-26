using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interface;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using System.Net;

namespace Homeocentrum.Niga.NewAPI.Domain.Implementation
{
    public class SubscriptionService : ISubscriptionService
    {
        NIGACentrumContext context;
        /// <summary>
        /// Creating constructor and injection dbContext
        /// </summary>
        /// <param name="centrumContext"></param>
        public SubscriptionService(NIGACentrumContext centrumContext)
        {
            context = centrumContext;
        }

        public List<SubscriptionModel> GetSubscription(ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var subscriptionEntityList = (from subscriptionEntity in context.PackageEntryDetails
                                          select new SubscriptionModel
                                          {
                                              PackageDetailId = subscriptionEntity.PackageId,
                                              PackageId = subscriptionEntity.PackageId,
                                              DoctorId = subscriptionEntity.DoctorId,
                                              ActivationDate = subscriptionEntity.ActivationDate,
                                              ExpiryDate = subscriptionEntity.ExpiryDate,
                                              TransactionId = subscriptionEntity.TransactionId,
                                              OrderId = subscriptionEntity.OrderId,
                                              PaymentId = subscriptionEntity.PaymentId,
                                              IsActive = subscriptionEntity.IsActive
                                          }
                                           ).ToList();

            if (subscriptionEntityList.Count == 0)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Pathology not found";
            }
           
            return subscriptionEntityList;
        }

        public SubscriptionModel GetSubscriptionById(long packageDetailId, ref ErrorResponseModel errorResponseModel)
        {
            errorResponseModel = new ErrorResponseModel();
            var subscriptionEntity = context.PackageEntryDetails.Where(x => x.PackageDetailId == packageDetailId).FirstOrDefault();
            if (subscriptionEntity == null)
            {
                errorResponseModel.StatusCode = HttpStatusCode.NotFound;
                errorResponseModel.Message = "Subscription not found";
            }
            return new SubscriptionModel
            {
                PackageDetailId = subscriptionEntity.PackageId,
                PackageId = subscriptionEntity.PackageId,
                DoctorId = subscriptionEntity.DoctorId,
                ActivationDate = subscriptionEntity.ActivationDate,
                ExpiryDate = subscriptionEntity.ExpiryDate,
                TransactionId = subscriptionEntity.TransactionId,
                OrderId = subscriptionEntity.OrderId,
                PaymentId = subscriptionEntity.PaymentId,
                IsActive = subscriptionEntity.IsActive
            };
       
        }

        public async Task<bool> SaveAllAsync()
        {
            return await context.SaveChangesAsync() > 0;
        }

        public string SaveSubscription(SubscriptionModel subscriptionModel,int userId, ref ErrorResponseModel errorResponseModel)
        {
            string Message = "";
            DateTime? expireDate=DateTime.Now; 

            var planDetail = context.PackageMasters.Where(x => x.PackageId == subscriptionModel.PackageId).FirstOrDefault();
            if (planDetail != null)
            {
                expireDate = Convert.ToDateTime(subscriptionModel.ActivationDate).AddDays(planDetail.ValidityInDays);
            }


            if (subscriptionModel.PackageDetailId == 0)
            {
                PackageEntryDetail subscriptionEntity = new PackageEntryDetail();
                subscriptionEntity.PackageId = subscriptionModel.PackageId;
                subscriptionEntity.DoctorId = subscriptionModel.DoctorId;
                subscriptionEntity.ActivationDate = subscriptionModel.ActivationDate;
                subscriptionEntity.ExpiryDate = expireDate;
                subscriptionEntity.TransactionId = subscriptionModel.TransactionId;
                subscriptionEntity.OrderId = subscriptionModel.OrderId;
                subscriptionEntity.PaymentId = subscriptionModel.PaymentId;
                subscriptionEntity.IsActive = true;
                subscriptionEntity.CreatedBy = userId;
                subscriptionEntity.CreatedDate = DateTime.Now;
                context.PackageEntryDetails.Add(subscriptionEntity);
                context.SaveChanges();
                Message = "Subscription Saved Successfully";
            }
            else
            {
                var subscriptionEntity = context.PackageEntryDetails.FirstOrDefault(x => x.PackageDetailId == subscriptionModel.PackageDetailId);
                if (subscriptionEntity != null)
                {
                    subscriptionEntity.PackageId = subscriptionModel.PackageId;
                    subscriptionEntity.DoctorId = subscriptionModel.DoctorId;
                    subscriptionEntity.ActivationDate = subscriptionModel.ActivationDate;
                    subscriptionEntity.ExpiryDate = expireDate;
                    subscriptionEntity.TransactionId = subscriptionModel.TransactionId;
                    subscriptionEntity.OrderId = subscriptionModel.OrderId;
                    subscriptionEntity.PaymentId = subscriptionModel.PaymentId;
                    subscriptionEntity.IsActive = true;
                    context.SaveChanges();
                    Message = "Subscription Updated Successfully";
                }
            }
            return Message;
        }
    }
}
