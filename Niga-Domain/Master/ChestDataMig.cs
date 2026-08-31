using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class ChestDataMig
{
    public string? Section { get; set; }

    public string? TypeofSymptoms { get; set; }

    public string? IdsymptomwithBodyPart { get; set; }

    public string? SymptomwithBodyPart { get; set; }

    public int? ParentIdforSymptomswithLocation { get; set; }

    public string? SymptomswithLocation { get; set; }

    public string? Symptomlevelfive { get; set; }

    public string? SymptomlevelSix { get; set; }

    public string? SymptomlevelSeven { get; set; }
}
