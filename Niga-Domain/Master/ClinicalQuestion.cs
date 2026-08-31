using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ClinicalQuestion
{
    public int QuestionsId { get; set; }

    public int? QuestionGroupId { get; set; }

    public int? QuestionSectionId { get; set; }

    public int? QuestionSubgroupId { get; set; }

    public int? BodyPartId { get; set; }

    public bool? DeleteStatus { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public int? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public virtual ICollection<ClinicalQueKeyword> ClinicalQueKeywords { get; set; } = new List<ClinicalQueKeyword>();

    public virtual QuestionGroupMaster? QuestionGroup { get; set; }
}
