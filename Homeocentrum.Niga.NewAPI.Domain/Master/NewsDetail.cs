using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class NewsDetail
{
    public int NewsId { get; set; }

    public int? NewsCategoryId { get; set; }

    public DateTime? NewsDate { get; set; }

    public string? NewsHeading { get; set; }

    public string? NewsSubHeading { get; set; }

    public string? NewsContent { get; set; }

    public string? NewsImage1 { get; set; }

    public string? NewsImage2 { get; set; }

    public string? NewsImage3 { get; set; }

    public string? NewsImage4 { get; set; }

    public int? EnteredBy { get; set; }

    public DateTime? EnteredDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual NewsCategory? NewsCategory { get; set; }
}
