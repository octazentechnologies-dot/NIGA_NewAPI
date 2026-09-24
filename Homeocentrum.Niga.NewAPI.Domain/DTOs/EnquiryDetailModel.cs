using System;
using System.Collections.Generic;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class EnquiryDetailModel
    {
        public int EnquiryId { get; set; }
        public string EnquiryName { get; set; }
        public DateTime? EnquiryDate { get; set; }
        public string EmailId { get; set; }
        public string MobileNo { get; set; }
        public string EnquiryDetails1 { get; set; }
        public bool? EnquiryStatus { get; set; }
    }
}
