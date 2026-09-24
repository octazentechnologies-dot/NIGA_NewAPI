namespace Niga_Domain.DTOs
{
    public class RescheduleAppointmentRequest
    {
        public int PatientAppId { get; set; }
        public DateTime? AppointmentDate { get; set; }
        public TimeOnly? AppointmentTime { get; set; }
        public string? Reason { get; set; }
    }

    public class CancelAppointmentRequest
    {
        public int PatientAppId { get; set; }
        public string? ReasonCode { get; set; }
        public string? ReasonText { get; set; }
    }

    public class PatchVisitTypeRequest
    {
        public string? VisitType { get; set; }
        public string? ConsultMode { get; set; }
    }

    public class AppointmentMutationResult
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
        public PatientAppointmentModel? Appointment { get; set; }
        public List<AppointmentSlotModel> Alternatives { get; set; } = new();
        public AppointmentNotificationResult? Notification { get; set; }
        public WaitlistOfferResult WaitlistOffer { get; set; } = new();
        public CancelRefundPolicyResult RefundPolicy { get; set; } = new();
    }

    /// <summary>
    /// APT-06.04 — cancel stub. Oldest JOINED waitlist row for that doctor and date becomes OFFERED.
    /// No SMS, no auto-booking. Full waitlist is Phase 9.
    /// </summary>
    public class WaitlistOfferResult
    {
        public bool Offered { get; set; }
        public int? BookingWaitlistId { get; set; }
        public string? ContactName { get; set; }
        public string? SlotDate { get; set; }
        public string? SlotTime { get; set; }
        public string Sms { get; set; } = "skipped";
        public string Push { get; set; } = "skipped";
        public string Detail { get; set; } = "SMS and push run when Phase 12 communications exist. No auto-booking.";
    }

    /// <summary>
    /// APT-06.02 — if the visit is PAID, queue the refund policy. Razorpay is not called.
    /// </summary>
    public class CancelRefundPolicyResult
    {
        public bool Queued { get; set; }
        public string Policy { get; set; } = "NONE";
        public decimal Amount { get; set; }
        public long? RefundId { get; set; }
        public string Detail { get; set; } = "No refund was sent.";
    }

    /// <summary>
    /// APT-05.04 — channel outcomes. Push stays "later". Failed channels do not undo the move.
    /// </summary>
    public class AppointmentNotificationResult
    {
        public string Sms { get; set; } = "skipped";
        public string WhatsApp { get; set; } = "skipped";
        public string Push { get; set; } = "later";
        public string? Message { get; set; }
        public string? Detail { get; set; }
    }

    public class AppointmentChangeLogItem
    {
        public long AppointmentChangeLogId { get; set; }
        public int PatientAppId { get; set; }
        public string Action { get; set; } = "";
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
        public long ByUserId { get; set; }
        public string? ByRole { get; set; }
        public string? Reason { get; set; }
        public DateTime At { get; set; }
    }

    public class JoinWaitlistRequest
    {
        public int DoctorId { get; set; }
        public int? PatientId { get; set; }
        public DateTime RequestedDate { get; set; }
        public string? ConsultMode { get; set; }
        public string? ContactName { get; set; }
        public string? ContactMobile { get; set; }
    }

    public class ReceptionProfileUpdate
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MobileNo { get; set; }
        public string? EmailId { get; set; }
    }

    public class CasePaperRequest
    {
        public int? PatientAppId { get; set; }
        public int PatientId { get; set; }
        public int? CaseId { get; set; }
        public string? ChiefComplaint { get; set; }
    }

    public class TeleAvailabilityUpdate
    {
        public bool IsOnline { get; set; }
    }

    public class TeleConsentRequest
    {
        public int TeleSessionId { get; set; }
        public bool Accepted { get; set; }
    }

    public class TeleChatRequest
    {
        public int SessionId { get; set; }
        public string? Body { get; set; }
    }

    public class ConsultationSummaryRequest
    {
        public int PatientAppId { get; set; }
        public string? Text { get; set; }
    }

    public class InstantConsultRequestBody
    {
        public int? PatientId { get; set; }
        public string? ContactName { get; set; }
        public string? ContactMobile { get; set; }
    }

    public class SupportTicketCreate
    {
        public string? Category { get; set; }
        public string? Subject { get; set; }
        public string? Body { get; set; }
        public string? ReporterRole { get; set; }
    }

    public class SupportTicketUpdate
    {
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public long? AssigneeUserId { get; set; }
    }

    public class SupportMessageCreate
    {
        public string? Body { get; set; }
        public string? FileName { get; set; }
    }

    public class HelpArticleWrite
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Body { get; set; }
        public bool IsPublished { get; set; }
    }

    public class AssistedBookRequest
    {
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeOnly AppointmentTime { get; set; }
        public string? ConsultMode { get; set; }
    }

    /// <summary>SUP-07.02 — patient asks clinic staff to book on their behalf (AssistedRequest ticket).</summary>
    public class AssistanceRequestBody
    {
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
        public string? Notes { get; set; }
        public string? PreferredDate { get; set; }
        public string? ContactMobile { get; set; }
    }
}
