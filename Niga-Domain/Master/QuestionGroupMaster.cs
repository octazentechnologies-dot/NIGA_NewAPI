using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class QuestionGroupMaster
{
    public int QuestionGroupId { get; set; }

    public string QuestionGroupName { get; set; } = null!;

    public string? Description { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }

    public int? QuestionSectionId { get; set; }

    public int? SectionId { get; set; }

    public virtual ICollection<ClinicalQuestion> ClinicalQuestions { get; set; } = new List<ClinicalQuestion>();

    public virtual SectionMaster? Section { get; set; }
}
