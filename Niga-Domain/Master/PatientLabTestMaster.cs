using System;
using System.Collections.Generic;
using Niga_Domain.Entities;

namespace Niga_Domain.Master;

public partial class PatientLabTestMaster:AuditableEntities
{
    public int PatientLabTestId { get; set; }

    public string? LabTestName { get; set; }

    public string? Description { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<PatientLabEntry> PatientLabEntries { get; set; } = new List<PatientLabEntry>();

    public virtual ICollection<PatientLabOrder> PatientLabOrders { get; set; } = new List<PatientLabOrder>();
}
