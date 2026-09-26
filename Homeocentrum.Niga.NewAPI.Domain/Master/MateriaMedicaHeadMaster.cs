using System;
using System.Collections.Generic;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

public partial class MateriaMedicaHeadMaster
{
    public int MateriaMedicaHeadId { get; set; }

    public int? AuthorId { get; set; }

    public string MateriaMedicaHeadName { get; set; } = null!;

    public string? Description { get; set; }

    public bool? IsSection { get; set; }

    public int? SeqNo { get; set; }

    public bool? DifferentialMm { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual AuthorMaster? Author { get; set; }

    public virtual ICollection<MateriaMedicaMaster> MateriaMedicaMasters { get; set; } = new List<MateriaMedicaMaster>();
}
