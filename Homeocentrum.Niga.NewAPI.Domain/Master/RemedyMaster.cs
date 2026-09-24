using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class RemedyMaster:AuditableEntities
{
    public int RemedyId { get; set; }

    public string RemedyName { get; set; } = null!;

    public string? RemedyAlias { get; set; }

    public string? Description { get; set; }

    public int? ThermalId { get; set; }

    public bool? CommonOrUncommon { get; set; }

    public string? ThemesOrCharacteristics { get; set; }

    public string? Generals { get; set; }

    public string? Modalities { get; set; }

    public string? Particulars { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<MateriaMedicaMaster> MateriaMedicaMasters { get; set; } = new List<MateriaMedicaMaster>();

    public virtual ICollection<RubricRemedyDetail> RubricRemedyDetails { get; set; } = new List<RubricRemedyDetail>();

    public virtual ThermalMaster? Thermal { get; set; }
}
