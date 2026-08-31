using System;
using System.Collections.Generic;
using System.Text;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interface
{
    public interface ISubscriptionService
    {
        List<SubscriptionModel> GetSubscription(ref ErrorResponseModel errorResponseModel);

        SubscriptionModel GetSubscriptionById(long packageDetailId, ref ErrorResponseModel errorResponseModel);

        string SaveSubscription(SubscriptionModel subscriptionModel, int userId, ref ErrorResponseModel errorResponseModel);

    }
}
