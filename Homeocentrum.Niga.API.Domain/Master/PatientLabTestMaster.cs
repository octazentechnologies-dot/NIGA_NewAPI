using System;
using System.Collections.Generic;
using Homeocentrum.Niga.API.Domain.Entities;

namespace Homeocentrum.Niga.API.Domain.Master;

public partial class PatientLabTestMaster:AuditableEntities
{
    public int PatientLabTestId { get; set; }

    public string? LabTestName { get; set; }

    public string? Description { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<PatientLabEntry> PatientLabEntries { get; set; } = new List<PatientLabEntry>();

    public virtual ICollection<PatientLabOrder> PatientLabOrders { get; set; } = new List<PatientLabOrder>();
}
