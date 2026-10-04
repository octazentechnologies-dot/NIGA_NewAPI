using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PinCodeMaster
{
    public int PinCodeId { get; set; }

    public string PinCode { get; set; } = null!;

    public int CityId { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual CityMaster City { get; set; } = null!;
}
