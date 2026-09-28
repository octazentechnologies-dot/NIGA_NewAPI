using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AppointmentHistoryNote
{
    public int HistoryId { get; set; }

    public int? AppointmentId { get; set; }

    public string? HistoryNote { get; set; }

    /// <summary>ERX-03.01 — ChiefComplaint | FollowUp | General (clinical notes stay off eRx).</summary>
    public string? NoteType { get; set; }

    /// <summary>ERX-03.01 — when true, note must not appear on signed eRx / patient prescription.</summary>
    public bool IsErxExcluded { get; set; } = true;

    public bool? DeletedStatus { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? ModifyBy { get; set; }

    public DateTime? ModifyDate { get; set; }

    public virtual PatientAppointment? Appointment { get; set; }
}
