using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    /// <summary>
    /// Query parameters for fetching paginated appointments by patient.
    /// </summary>
    public class GetAppointmentListByPatientIdRequest
    {
        [Required(ErrorMessage = "PatientId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "PatientId must be greater than 0")]
        public int PatientId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "PageNumber must be greater than 0")]
        public int PageNumber { get; set; } = 1;

        [Range(1, PaginationRequestModel.MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; }
    }

    /// <summary>
    /// Query parameters for fetching appointments on a specific date for a doctor user.
    /// </summary>
    public class GetAppointmentsByDateRequest
    {
        [Required(ErrorMessage = "UserId is required")]
        [Range(1, long.MaxValue, ErrorMessage = "UserId must be greater than 0")]
        public long UserId { get; set; }

        [Required(ErrorMessage = "AppointmentDate is required")]
        public DateTime AppointmentDate { get; set; }
    }

    /// <summary>
    /// Appointment list item returned by GetAppointmentListByPatientId.
    /// </summary>
    public class PatientAppointmentListItemModel
    {
        public int PatientAppId { get; set; }

        public int PatientId { get; set; }

        public string? AppointmentDate { get; set; }

        public string? AppointmentTime { get; set; }

        public string? AppointmentDateTime { get; set; }

        public string? Status { get; set; }

        public int DoctorId { get; set; }

        public long UserId { get; set; }

        public bool IsWhatsAppOptIn { get; set; }

        public DateTime? WhatsAppOptInDate { get; set; }

        public string? PaymentStatus { get; set; }

        public bool? IsTele { get; set; }

        public string? VisitType { get; set; }

        public string? ConsultMode { get; set; }

        public string? PaymentMethod { get; set; }

        public bool? PayAtClinicAllowed { get; set; }
    }
}
