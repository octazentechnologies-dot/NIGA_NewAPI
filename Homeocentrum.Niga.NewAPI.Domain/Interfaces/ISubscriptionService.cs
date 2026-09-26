using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interface
{
    public interface ISubscriptionService
    {
        List<SubscriptionModel> GetSubscription(ref ErrorResponseModel errorResponseModel);

        SubscriptionModel GetSubscriptionById(long packageDetailId, ref ErrorResponseModel errorResponseModel);

        string SaveSubscription(SubscriptionModel subscriptionModel, int userId, ref ErrorResponseModel errorResponseModel);

    }
}
