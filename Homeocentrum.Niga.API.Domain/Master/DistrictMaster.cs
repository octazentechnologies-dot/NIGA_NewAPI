using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.API.Domain.Master;

public partial class DistrictMaster
{
    public int DistrictId { get; set; }

    public string DistrictName { get; set; } = null!;

    public int StateId { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual StateMaster State { get; set; } = null!;

    public virtual ICollection<CityMaster> CityMasters { get; set; } = new List<CityMaster>();
}
