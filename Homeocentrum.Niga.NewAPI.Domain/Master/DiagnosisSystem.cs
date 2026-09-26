using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisSystem
{
    public int DiagnosisSystemId { get; set; }

    public string? DiagnosisSystemName { get; set; }

    public string? Description { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<DiagnosisSystemDetail> DiagnosisSystemDetails { get; set; } = new List<DiagnosisSystemDetail>();
}
