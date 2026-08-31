using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class PatientLabOrderModel
    {
        public int PatientOrderedTestId { get; set; }
       
        [Required(ErrorMessage ="Please select patient")]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Please select test")]
        public int? PatientLabTestId { get; set; }
        public string PatientLabTestName { get; set; }
        public DateTime? OrderDate { get; set; }
        public string LabName { get; set; }
        public int EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public int ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
        public int CaseId { get; set; }
        public int UserId { get; set; }


    }
}
