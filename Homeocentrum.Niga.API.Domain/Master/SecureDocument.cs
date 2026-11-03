using System;

namespace Homeocentrum.Niga.API.Domain.Master;

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

    /// <summary>Patient upload category: Report, Prescription, LabReport, Invoice, Other.</summary>
    public string? DocumentType { get; set; }

    /// <summary>Optional note from the uploader.</summary>
    public string? Description { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}
