using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

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

    public virtual ICollection<AppointmentHistoryNote> AppointmentHistoryNotes { get; set; } = new List<AppointmentHistoryNote>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual ICollection<PrescriptionRemedyDetail> PrescriptionRemedyDetails { get; set; } = new List<PrescriptionRemedyDetail>();

    public virtual ICollection<PrescriptionRubricDetail> PrescriptionRubricDetails { get; set; } = new List<PrescriptionRubricDetail>();
}
