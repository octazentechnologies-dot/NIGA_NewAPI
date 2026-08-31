using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class OrderModel
    {
        public int Amount { get; set; } = 0;
        public string Currency { get; set; } = string.Empty;
        public string Receipt { get; set; } = string.Empty;
        public int PaymentCapture { get; set; } = 0;

    }
}
