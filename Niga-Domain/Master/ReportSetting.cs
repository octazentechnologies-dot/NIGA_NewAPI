using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ReportSetting
{
    public int RecordId { get; set; }

    public int MenuId { get; set; }

    public string ReportName { get; set; } = null!;

    public string? FilterCriteria { get; set; }

    public string MethodName { get; set; } = null!;

    public bool MultipleIssue { get; set; }

    public string FirmIds { get; set; } = null!;

    public string? Applicablefor { get; set; }

    public double? PageWidth { get; set; }

    public double? PageHeight { get; set; }

    public string? ReportFont { get; set; }

    public string? ReportFontSize { get; set; }

    public string? TrustFontSize { get; set; }

    public string? BranchFontSize { get; set; }

    public string? BranchAddressFontSize { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual MenuMaster Menu { get; set; } = null!;
}
