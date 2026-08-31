using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
   public class PartLocationModel
    {
        public int PartLocationId { get; set; }
        [Required(ErrorMessage = "PartLocation Name is required")]
        public string PartLocationName { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }
}
