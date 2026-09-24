using System;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class SubscriptionStatusModel
    {
        public bool IsPlanActive { get; set; }
        public bool IslastFiveDays { get; set; }
        public int DaysRemaining { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
