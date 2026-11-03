using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.API.Domain.Master;

public partial class PatientAppointment
{
    public int PatientAppId { get; set; }

    public int PatientId { get; set; }

    public DateTime? AppointmentDate { get; set; }

    public TimeOnly? AppointmentTime { get; set; }

    public string? Status { get; set; }

    public bool? DeleteStatus { get; set; }

    public long UserId { get; set; }

    public int DoctorId { get; set; }

    public string? BookingToken { get; set; }

    public string? VisitType { get; set; }

    public string? ConsultMode { get; set; }

    public string? PaymentStatus { get; set; }

    public bool? IsTele { get; set; }

    public DateTime? HoldExpiresAt { get; set; }

    public string? ConsentPolicyVersion { get; set; }

    public string? CancelReasonCode { get; set; }

    public string? CancelReasonText { get; set; }

    public long? CancelledBy { get; set; }

    public DateTime? CancelledAt { get; set; }

    public string? PaymentMethod { get; set; }

    public bool? PayAtClinicAllowed { get; set; }

    /// <summary>Patient / family login that created the hold (not the doctor UserId).</summary>
    public long? BookedByUserId { get; set; }

    public int? QueuePosition { get; set; }

    public string? BookingChannel { get; set; }

    public DateTime? CalledAt { get; set; }

    /// <summary>When the visit entered OVERDUE (missed slot).</summary>
    public DateTime? OverdueAt { get; set; }

    /// <summary>End of 24h reschedule window (SlotAt + 24h).</summary>
    public DateTime? GraceEndsAt { get; set; }

    /// <summary>When the visit was auto-marked EXPIRED after grace.</summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>False after EXPIRED (missed grace) — blocks refunds. Null = use default cancel policy.</summary>
    public bool? RefundEligible { get; set; }

    public virtual ICollection<AppointmentHistoryNote> AppointmentHistoryNotes { get; set; } = new List<AppointmentHistoryNote>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PrescriptionRemedyDetail> PrescriptionRemedyDetails { get; set; } = new List<PrescriptionRemedyDetail>();

    public virtual ICollection<PrescriptionRubricDetail> PrescriptionRubricDetails { get; set; } = new List<PrescriptionRubricDetail>();
}
