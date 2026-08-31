using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string FirstName { get; set; } = null!;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = null!;

    public int? QualificationId { get; set; }

    public string? PermanantAddress { get; set; }

    public string? MobileNo { get; set; }

    public string? EmailId { get; set; }

    public int? CasePaperValidity { get; set; }

    public int? PackageId { get; set; }

    public string? PassingUniversity { get; set; }

    public string? PassingCertNo { get; set; }

    public string? City { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<CaseEntryDetail> CaseEntryDetails { get; set; } = new List<CaseEntryDetail>();

    public virtual PackageMaster? Package { get; set; }

    public virtual ICollection<PackageEntryDetail> PackageEntryDetails { get; set; } = new List<PackageEntryDetail>();

    public virtual ICollection<PatientAppointment> PatientAppointments { get; set; } = new List<PatientAppointment>();

    public virtual ICollection<DoctorReceptionStaff> DoctorReceptionStaffs { get; set; } = new List<DoctorReceptionStaff>();

    public virtual QualificationMaster? Qualification { get; set; }
}
