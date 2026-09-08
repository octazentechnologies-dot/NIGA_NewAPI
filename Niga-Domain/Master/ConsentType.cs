using System;
using System.Collections.Generic;

namespace Niga_Domain.Master;

/// <summary>SEC-06.01 — Consent type master (Privacy, Booking, TeleRecording, …).</summary>
public partial class ConsentType
{
    public int ConsentTypeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<ConsentRecord> ConsentRecords { get; set; } = new List<ConsentRecord>();
}
