using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class QuestionSubgroup
{
    public int QuestionSubgroupId { get; set; }

    public string? QuestionSubgroup1 { get; set; }

    public string? Description { get; set; }

    public int? QuestionGroupId { get; set; }

    public bool? DeleteStatus { get; set; }
}
