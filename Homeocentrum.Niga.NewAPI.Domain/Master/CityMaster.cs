using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class CityMaster
{
    public int CityId { get; set; }

    public string CityName { get; set; } = null!;

    public int DistrictId { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual DistrictMaster District { get; set; } = null!;

    public virtual ICollection<PinCodeMaster> PinCodeMasters { get; set; } = new List<PinCodeMaster>();
}
