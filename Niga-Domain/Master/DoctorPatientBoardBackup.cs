using System;

namespace Niga_Domain.Master;

public partial class DoctorPatientBoardBackup
{
    public long BackupId { get; set; }

    public long DoctorUserId { get; set; }

    public string BackupPayload { get; set; } = null!;

    public int PatientCount { get; set; }

    public int SchemaVersion { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}
