using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class ClinicalQueKeyword
{
    public int ClinicalQueKeywordId { get; set; }

    public int? QuestionsId { get; set; }

    public string? KeywordQuestion { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ClinicalQuestion? Questions { get; set; }
}
