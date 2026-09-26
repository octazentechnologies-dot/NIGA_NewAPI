using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class AuthorMaster
{
    public int AuthorId { get; set; }

    public string AuthorName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsDeleted { get; set; }

    public bool? IsForRepertory { get; set; }

    public string? AuthorAlias { get; set; }

    public virtual ICollection<MateriaMedicaHeadMaster> MateriaMedicaHeadMasters { get; set; } = new List<MateriaMedicaHeadMaster>();

    public virtual ICollection<MateriaMedicaMaster> MateriaMedicaMasters { get; set; } = new List<MateriaMedicaMaster>();

    public virtual ICollection<RemedyRubricAuthorDetail> RemedyRubricAuthorDetails { get; set; } = new List<RemedyRubricAuthorDetail>();
}
