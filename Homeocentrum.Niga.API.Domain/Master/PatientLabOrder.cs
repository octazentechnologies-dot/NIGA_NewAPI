using System;
using System.Collections.Generic;
using Homeocentrum.Niga.API.Domain.Entities;

namespace Homeocentrum.Niga.API.Domain.Master;

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
