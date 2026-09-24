using System;

namespace Homeocentrum.Niga.NewAPI.Domain.Master;

/// <summary>SEC-09.01 — Authorised document metadata (blob path; no public listing).</summary>
public partial class SecureDocument
{
    public long SecureDocumentId { get; set; }

    public string OwnerType { get; set; } = null!;

    public long OwnerId { get; set; }

    public string BlobPath { get; set; } = null!;

    public string? FileName { get; set; }

    public string? Mime { get; set; }

    public string? Hash { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}
