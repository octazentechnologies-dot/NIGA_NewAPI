using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class ThreeDBodyPartSectionMaster
{
    public int ThreeDBodyPartSectionMasterId { get; set; }

    public int ThreeDBodyPartMeshKeyId { get; set; }

    public int ThreeDBodyPartSectionId { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ThreeDBodyPartMeshKeyMaster? ThreeDBodyPartMeshKey { get; set; }

    public virtual SectionMaster? Section { get; set; }
}
