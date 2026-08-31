using System;
using System.Collections.Generic;
using Niga_Domain.Entities;

namespace Niga_Domain.Master;

public partial class PatientLabOrder:AuditableEntities
{
    public int PatientOrderedTestId { get; set; }

    public int PatientId { get; set; }

    public int? PatientLabTestId { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? LabName { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual PatientLabTestMaster? PatientLabTest { get; set; }
}
