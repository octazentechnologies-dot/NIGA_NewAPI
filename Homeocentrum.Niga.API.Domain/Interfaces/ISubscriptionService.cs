using System;
using System.Collections.Generic;
using System.Text;
using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Interface
{
    public interface ISubscriptionService
    {
        List<SubscriptionModel> GetSubscription(ref ErrorResponseModel errorResponseModel);

        SubscriptionModel GetSubscriptionById(long packageDetailId, ref ErrorResponseModel errorResponseModel);

        string SaveSubscription(SubscriptionModel subscriptionModel, int userId, ref ErrorResponseModel errorResponseModel);

    }
}
