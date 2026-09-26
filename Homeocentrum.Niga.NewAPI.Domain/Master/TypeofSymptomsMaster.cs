using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class TypeofSymptomsMaster
{
    public int TypeofSymptomsId { get; set; }

    public int? TypeofSymptomsGroupId { get; set; }

    public int? SectionId { get; set; }

    public int? SectionGroupId { get; set; }

    public string TypeofSymptomsName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual SectionMaster? Section { get; set; }

    public virtual SectionGroupMaster? SectionGroup { get; set; }

    public virtual TypeofSymptomsGroupMaster? TypeofSymptomsGroup { get; set; }
}
