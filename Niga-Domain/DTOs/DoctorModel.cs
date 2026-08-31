using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class DoctorModel
    {
       public int DoctorID { get; set; }

        [Required(ErrorMessage ="First Name is required")]
        public string FirstName { get; set; }
        public string MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        public string LastName { get; set; }
        public int? QualificationID { get; set; }
        public string PermanantAddress { get; set; }
        public string MobileNo { get; set; }
        public string EmailId { get; set; }
        public int? CasePaperValidity { get; set; }
        public int? PackageId { get; set; }
        public string PassingUniversity { get; set; }
        public string PassingCertNo { get; set; }
        public string City { get; set; }
        public string EnteredBy { get; set; }
        public int EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int? UserId { get; set; }
        public string DoctorName { get; set; }

    }
}
