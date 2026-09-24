using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
   public class IntensityModel
    {
        public int IntensityId { get; set; }
        [Required(ErrorMessage = "Intensity No is required")]
        public int IntensityNo { get; set; }
        public string Description { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }
}
