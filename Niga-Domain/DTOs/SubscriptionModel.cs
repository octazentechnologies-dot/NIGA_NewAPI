using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class SubscriptionModel
    {
        public int? PackageDetailId { get; set; }
        public int? PackageId { get; set; }
        public int? DoctorId { get; set; }
        public DateTime? ActivationDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public string PaymentId { get; set; }
        public bool? IsActive { get; set; }
      
    }
}
