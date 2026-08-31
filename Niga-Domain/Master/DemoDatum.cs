using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class DemoDatum
{
    public int SubSectionLanguageId { get; set; }

    public int SubSectionId { get; set; }

    public int LanguageId { get; set; }

    public string SubSectionDetails { get; set; } = null!;

    public bool? DeleteStatus { get; set; }

    public virtual LanguageMaster Language { get; set; } = null!;

    public virtual SubSectionMaster SubSection { get; set; } = null!;
}
