using System;
using System.Collections.Generic;
using System.Text;

namespace Niga_Domain.DTOs
{
#nullable disable
    public class PatientAppointmentModel
    {
        public int PatientAppId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string MobileNo { get; set; }
        public string? Email { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeOnly? AppointmentTime { get; set; }
        public string Status { get; set; }
        public bool? DeleteStatus { get; set; }
        public long UserId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public int CaseId { get; set; }
        public string Message { get; set; }
        public string? Address { get; set; }
        public int? Age { get; set; }
        public int? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool IsWhatsAppOptIn { get; set; }
        public DateTime? WhatsAppOptInDate { get; set; }
        public string? VisitType { get; set; }
        public string? ConsultMode { get; set; }
        public string? PaymentStatus { get; set; }
        public bool? IsTele { get; set; }
        public string? BookingToken { get; set; }
        }


    }

public class UpdateAppointmentStatusModel
{
    public long PatientAppId { get; set; }
    public string Status { get; set; }
}

public class UpdateAppointmentTimeModel
{
    public long PatientAppId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public TimeOnly? AppointmentTime { get; set; }
}


public class PatientAppointmentModel1
    {
        public int PatientAppId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; }
        public string MobileNo { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeSpan? AppointmentTime { get; set; }
        public string Status { get; set; }
        public bool? DeleteStatus { get; set; }
        public long UserId { get; set; }
        public int DoctorId { get; set; }
        public string DoctorName { get; set; }
        public int CaseId { get; set; }
        public int HistoryNoteId { get; set; }
        public bool IsWhatsAppOptIn { get; set; }
        public DateTime? WhatsAppOptInDate { get; set; }
        public string? VisitType { get; set; }
        public string? ConsultMode { get; set; }
        public string? PaymentStatus { get; set; }
        public bool? IsTele { get; set; }
    }

