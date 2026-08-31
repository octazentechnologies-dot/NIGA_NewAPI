using System;

namespace Niga_Domain.Master;

public partial class ThreeDBodyPartSectionHotspot
{
    public int SectionHotspotId { get; set; }

    public int SectionId { get; set; }

    public string HotspotName { get; set; } = null!;

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}
