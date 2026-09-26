using System;
using System.Collections.Generic;
using Homeocentrum.Niga.NewAPI.Domain.Entities;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class BlogDetail:AuditableEntities
{
    public int BlogId { get; set; }

    public string? BlogHead { get; set; }

    public string? BlogSubHead { get; set; }

    public DateTime? BlogDate { get; set; }

    public string? BlogImage1 { get; set; }

    public string? BlogImage2 { get; set; }

    public string? BlogDetails { get; set; }

    public bool? IsActive { get; set; }
 
}
