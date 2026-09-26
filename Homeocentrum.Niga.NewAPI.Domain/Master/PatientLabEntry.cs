using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PatientLabEntry:AuditableEntities
{
    public int PatientLabId { get; set; }

    public int PatientId { get; set; }

    public int PatientLabTestId { get; set; }

    public DateTime? LabDate { get; set; }

    public string? ParameterName { get; set; }

    public string? ParameterValue { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual PatientLabTestMaster PatientLabTest { get; set; } = null!;
}
