using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PrescriptionRubricDetail
{
    public int PrescriptionRubricId { get; set; }

    public int AppointmentId { get; set; }

    public int RubricId { get; set; }

    public int IntensityId { get; set; }

    public int RemedyCount { get; set; }

    public bool? DeletedStatus { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual PatientAppointment Appointment { get; set; } = null!;
}
