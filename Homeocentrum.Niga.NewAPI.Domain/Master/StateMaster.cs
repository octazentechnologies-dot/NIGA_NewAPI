using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class StateMaster
{
    public int StateId { get; set; }

    public string StateName { get; set; } = null!;

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public int? CountryId { get; set; }

    public virtual CountryMaster? Country { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
