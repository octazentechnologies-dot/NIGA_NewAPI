using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ClipboardRubric
{
    public int ClipboardRubricsId { get; set; }

    public int? PatientId { get; set; }

    public int? SubSectionId { get; set; }

    public string? Intensity { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual SubSectionMaster? SubSection { get; set; }
}
