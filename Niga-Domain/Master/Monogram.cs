using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class Monogram
{
    public int MonogramId { get; set; }

    public string? Monogram1 { get; set; }

    public string? Keywords { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<DiagnosisMonogram> DiagnosisMonograms { get; set; } = new List<DiagnosisMonogram>();

    public virtual ICollection<MonogramDetail> MonogramDetails { get; set; } = new List<MonogramDetail>();
}
