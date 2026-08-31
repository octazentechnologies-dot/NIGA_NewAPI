using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

public partial class LanguageVersion
{
    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = null!;

    public string LanguageLogo { get; set; } = null!;

    public int? SeqNo { get; set; }
}
