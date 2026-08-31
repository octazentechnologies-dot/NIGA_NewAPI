using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class BodyPartMaster
{
    public int BodyPartId { get; set; }

    public int SectionId { get; set; }

    public string BodyPartName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual SectionMaster Section { get; set; } = null!;
}
