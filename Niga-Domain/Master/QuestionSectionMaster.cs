using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class QuestionSectionMaster
{
    public int QuestionSectionId { get; set; }

    public string QuestionSectionName { get; set; } = null!;

    public string? Desciption { get; set; }

    public string? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public string? ChangedBy { get; set; }

    public DateTime? ChangedDate { get; set; }

    public bool DeleteStatus { get; set; }
}
