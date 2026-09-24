using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PackageMaster:AuditableEntities
{
    public int PackageId { get; set; }

    public string PackageName { get; set; } = null!;

    public int CaseCount { get; set; }

    public int ValidityInDays { get; set; }

    public decimal Amount { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();

    public virtual ICollection<PackageEntryDetail> PackageEntryDetails { get; set; } = new List<PackageEntryDetail>();
}
