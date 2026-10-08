using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class QuestionSubgroupSection
{
    public int QuestionSubgroupSectionId { get; set; }

    public int QuestionSubgroupId { get; set; }

    public int SectionId { get; set; }

    public bool DeleteStatus { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }
}
