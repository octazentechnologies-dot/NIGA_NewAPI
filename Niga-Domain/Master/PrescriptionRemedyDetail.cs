using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class PrescriptionRemedyDetail
{
    public int PrescriptionRemedyId { get; set; }

    public int AppointmentId { get; set; }

    public int RemedyId { get; set; }

    public string? Description { get; set; }

    public string? Dose { get; set; }

    public bool? DeletedStatus { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual PatientAppointment Appointment { get; set; } = null!;
}
