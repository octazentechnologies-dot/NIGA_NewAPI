using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
   public class PackageModel
    {
        public int PackageId { get; set; }
        [Required(ErrorMessage = "Package Name is required")]
        public string PackageName { get; set; }
        [Required(ErrorMessage = "Case Count is required")]
        public int CaseCount { get; set; }
        [Required(ErrorMessage = "Validity is required")]
        public int ValidityInDays { get; set; }
        [Required(ErrorMessage = "Amount is required")]
        public decimal Amount { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }
}
