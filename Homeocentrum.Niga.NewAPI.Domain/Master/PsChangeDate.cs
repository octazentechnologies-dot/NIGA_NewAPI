using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class PsChangeDate
{
    public int ChangeDateId { get; set; }

    public int FirmId { get; set; }

    public DateTime CloseYear { get; set; }

    public DateTime CloseMonth { get; set; }

    public DateTime CurrDate { get; set; }

    public bool? BkdtStatus { get; set; }

    public int? BkdtId { get; set; }

    public bool? Status { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}
