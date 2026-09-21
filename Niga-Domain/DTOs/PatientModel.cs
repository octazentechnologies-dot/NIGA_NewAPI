using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
    public class PatientModel : BaseModel
    {
        public int DoctorID { get; set; }
        public int PatientID { get; set; }
        public string? PatientName { get; set; }
        public string? Address { get; set; }
        public int? StateId { get; set; }
        public int? CountryId { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }

        public string? Mail
        {
            get => Email;
            set => Email = value;
        }

        public string? PhoneNo { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public string? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public int? UserId { get; set; }
        public bool? DeleteStatus { get; set; }
        public DateTime? DateodFirstVisit { get; set; }
        public string? RefBy { get; set; }
        public string? Message { get; set; }
        public int CaseId { get; set; }
        public int? Age { get; set; }

        public bool IsWhatsAppOptIn { get; set; }

        public DateTime? WhatsAppOptInDate { get; set; }

        /// <summary>DOC-03.02 — latest appointment date for this patient (nullable).</summary>
        public DateTime? LastVisitAt { get; set; }
    }

    public class BaseModel
    {
        public int LoggedInUser { get; set; }
        public string? DiagnosisIds { get; set; }
        public string? ChiefComplaintIds { get; set; }
    }

    public class GetPatientDetailsById
    {
        public int PatientID { get; set; }
        public string? PatientName { get; set; }
        public bool IsWhatsAppOptIn { get; set; }
        public DateTime? WhatsAppOptInDate { get; set; }
    }


}
