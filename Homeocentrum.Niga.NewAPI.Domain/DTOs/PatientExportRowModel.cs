using System;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class PatientExportRowModel
    {
        public int PatientId { get; set; }
        public int CaseId { get; set; }
        public string? PatientName { get; set; }
        public string? Address { get; set; }
        public string? StateName { get; set; }
        public string? CountryName { get; set; }
        public string? MobileNo { get; set; }
        public string? PhoneNo { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public int? Age { get; set; }
        public DateTime? DateOfFirstVisit { get; set; }
        public string? RefBy { get; set; }
        public bool IsWhatsAppOptIn { get; set; }
        public DateTime? WhatsAppOptInDate { get; set; }
        public string? Diagnosis { get; set; }
        public string? ChiefComplaints { get; set; }
        public string? EnteredBy { get; set; }
        public DateTime? EnteredDate { get; set; }
        public string? ChangedBy { get; set; }
        public DateTime? ChangedDate { get; set; }
        public int? PatientAppId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeOnly? AppointmentTime { get; set; }
        public string? AppointmentStatus { get; set; }
    }
}
