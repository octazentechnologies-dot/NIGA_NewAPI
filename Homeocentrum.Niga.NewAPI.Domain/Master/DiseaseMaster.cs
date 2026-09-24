using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiseaseMaster
{
    public int DiseaseId { get; set; }

    public string? DiseaseName { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual ICollection<MedicalAstrologyMaster> MedicalAstrologyMasters { get; set; } = new List<MedicalAstrologyMaster>();
}
