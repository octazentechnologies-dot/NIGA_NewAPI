using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class DiagnosisKeywordSection
{
    public int DiagnosisKeywordSectionId { get; set; }

    public int DiagnosisId { get; set; }

    public string KeywordType { get; set; } = null!;

    public int KeywordDetailId { get; set; }

    public int SectionId { get; set; }

    public bool DeleteStatus { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }
}
