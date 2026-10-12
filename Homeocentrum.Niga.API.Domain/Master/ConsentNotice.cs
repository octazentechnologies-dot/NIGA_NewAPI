using System;

namespace Homeocentrum.Niga.API.Domain.Master;

/// <summary>
/// Published wording of a consent (type + version + language). Text and hash are immutable in the database;
/// a change of wording is a new version.
/// </summary>
public partial class ConsentNotice
{
    public int ConsentNoticeId { get; set; }

    public int ConsentTypeId { get; set; }

    public string Version { get; set; } = null!;

    public string Language { get; set; } = "en";

    public string Title { get; set; } = null!;

    public string Body { get; set; } = null!;

    /// <summary>Lower-case hex SHA-256 of the UTF-8 body.</summary>
    public string BodySha256 { get; set; } = null!;

    public DateTime EffectiveFrom { get; set; }

    /// <summary>Consents given before this version must be given again.</summary>
    public bool RequiresReconsent { get; set; }

    public bool IsCurrent { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ConsentType? ConsentType { get; set; }
}
