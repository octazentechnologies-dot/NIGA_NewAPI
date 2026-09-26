using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class Pathology
{
    public int PathologyId { get; set; }

    public string? PathologyName { get; set; }

    public string? Description { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual ICollection<DiagnosisPathology> DiagnosisPathologies { get; set; } = new List<DiagnosisPathology>();
}
