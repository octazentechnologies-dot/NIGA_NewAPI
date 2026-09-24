using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class CaseEntryDetailsModel
    {
        public int CaseId { get; set; }
        [Required(ErrorMessage ="Please select Patient")]
        public int PatientId { get; set; }
        public int UserId { get; set; }
        [Required(ErrorMessage = "Please select Doctor")]
        public int DoctorId { get; set; }
        public string DiagnosisIds { get; set; }
        public string ChiefComplaintIds { get; set; }
        public DateTime DateodFirstVisit { get; set; }
        public string RefBy { get; set; }
        public string EnteredBy { get; set; }
        public DateTime EnteredDate { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public bool DeleteStatus { get; set; }
    }
}
