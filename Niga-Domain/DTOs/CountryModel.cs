using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class CountryModel
    {
        public int CountryId { get; set; }
        [Required(ErrorMessage ="Country Name is required")]
        public string CountryName { get; set; }
        public string EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public bool DeleteStatus  { get; set; }
}
}
