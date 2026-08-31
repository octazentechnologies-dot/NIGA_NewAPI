using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class MedicalAstrologyMaster
{
    public int AstrologyId { get; set; }

    public string? MedicalAstrologyName { get; set; }

    public int? DiseaseId { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual DiseaseMaster? Disease { get; set; }
}
