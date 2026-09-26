using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class FirmDetail
{
    public int FirmId { get; set; }

    public string FirmName { get; set; } = null!;

    public string? FirmNameMarathi { get; set; }

    public string FirmRegNumber { get; set; } = null!;

    public DateTime FirmRegDate { get; set; }

    public string FirmBranchName { get; set; } = null!;

    public string? FirmBranchNameMarathi { get; set; }

    public string FirmOfficeAddress { get; set; } = null!;

    public string? FirmOfficeAddressMarathi { get; set; }

    public string? FirmLogo { get; set; }

    public string? FirmPhoneNumber { get; set; }

    public string? FirmFaxNumber { get; set; }

    public string? FirmEmailIid { get; set; }

    public string? MailPassword { get; set; }

    public bool IsFederation { get; set; }

    public string FirmConnectionPath { get; set; } = null!;

    public string LanguageIds { get; set; } = null!;

    public int? ParentFirmId { get; set; }

    public string? ModuleIds { get; set; }

    public int? UserLimit { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public bool? IsNeedToBeSingleTerminalLogin { get; set; }

    public string? DatabaseBackupPath { get; set; }

    public bool? IsDateOverlap { get; set; }

    public DateOnly? ApplicationLockDate { get; set; }

    public bool? IsLockApplication { get; set; }

    public bool? IsSyncStaring { get; set; }

    public virtual ICollection<UserDetail> UserDetails { get; set; } = new List<UserDetail>();

    public virtual ICollection<YearMaster> YearMasters { get; set; } = new List<YearMaster>();
}
