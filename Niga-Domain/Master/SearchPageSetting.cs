using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class SearchPageSetting
{
    public int RecordId { get; set; }

    public int MenuId { get; set; }

    public string? FilterCriteria { get; set; }

    public string MethodName { get; set; } = null!;

    public string DataKeyName { get; set; } = null!;

    public string FirmIds { get; set; } = null!;

    public string TableName { get; set; } = null!;

    public string? ExceptTableNames { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public virtual MenuMaster Menu { get; set; } = null!;
}
