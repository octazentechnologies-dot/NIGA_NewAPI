using System;
using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs;

public class SavePatientBoardBackupRequest
{
    [Required]
    public string BackupPayload { get; set; } = null!;

    public int PatientCount { get; set; }

    public int SchemaVersion { get; set; } = 1;
}

public class PatientBoardBackupSummaryModel
{
    public bool HasBackup { get; set; }

    public long? BackupId { get; set; }

    public int PatientCount { get; set; }

    public int SchemaVersion { get; set; }

    public DateTime? SavedAt { get; set; }
}

public class PatientBoardBackupDetailModel
{
    public long BackupId { get; set; }

    public int PatientCount { get; set; }

    public int SchemaVersion { get; set; }

    public DateTime SavedAt { get; set; }

    public string BackupPayload { get; set; } = null!;
}

public class SavePatientBoardBackupResultModel
{
    public long BackupId { get; set; }

    public int PatientCount { get; set; }

    public DateTime SavedAt { get; set; }
}
