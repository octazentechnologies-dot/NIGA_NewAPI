using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class CaseEntryDetail :AuditableEntities
{
    public int CaseId { get; set; }

    public int PatientId { get; set; }

    public int? UserId { get; set; }

    public int DoctorId { get; set; }

    public DateTime? DateodFirstVisit { get; set; }

    public string? RefBy { get; set; }

    public bool? DeleteStatus { get; set; }

    public virtual ICollection<CaseDetailRemedy> CaseDetailRemedies { get; set; } = new List<CaseDetailRemedy>();

    public virtual ICollection<CaseEntryChiefComplaint> CaseEntryChiefComplaints { get; set; } = new List<CaseEntryChiefComplaint>();

    public virtual ICollection<CaseEntryDiagnosis> CaseEntryDiagnoses { get; set; } = new List<CaseEntryDiagnosis>();

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;
}
