using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class NewsCategory
{
    public int NewsCategoryId { get; set; }

    public string? NewsCategory1 { get; set; }

    public int? SeqNo { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<NewsDetail> NewsDetails { get; set; } = new List<NewsDetail>();
}
