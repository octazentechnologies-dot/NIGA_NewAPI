using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class ThreeDBodyPartMeshKeyMaster
{
    public int ThreeDBodyPartMeshKeyId { get; set; }

    public string ThreeDBodyPartMeshKeyName { get; set; } = null!;

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<ThreeDBodyPartSectionMaster> ThreeDBodyPartSectionMasters { get; set; } = new List<ThreeDBodyPartSectionMaster>();
}
