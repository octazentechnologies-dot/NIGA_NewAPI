using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class MateriaMedicaDetail
{
    public int MatriaMedicaDetailId { get; set; }

    public int MateriaMedicaId { get; set; }

    public string? MateriaMedicaDetail1 { get; set; }

    public int? SeqNo { get; set; }

    public virtual MateriaMedicaMaster MateriaMedica { get; set; } = null!;
}
