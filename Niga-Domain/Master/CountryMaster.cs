using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class CountryMaster
{
    public int CountryId { get; set; }

    public string CountryName { get; set; } = null!;

    public string? CountryCode { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();

    public virtual ICollection<StateMaster> StateMasters { get; set; } = new List<StateMaster>();
}
