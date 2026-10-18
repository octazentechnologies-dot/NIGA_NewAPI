namespace Homeocentrum.Niga.API.Domain.Configuration;

/// <summary>
/// Firebase Cloud Messaging (FCM). Leave Enabled false until the service account is saved.
/// Prefer CredentialsPath to a local JSON file that is not committed to git.
/// </summary>
public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>When false, tokens are still saved but no push is sent.</summary>
    public bool Enabled { get; set; }

    public string ProjectId { get; set; } = string.Empty;

    /// <summary>Absolute or ContentRoot-relative path to the Firebase service-account JSON.</summary>
    public string CredentialsPath { get; set; } = string.Empty;

    /// <summary>Optional inline service-account JSON. Prefer CredentialsPath so secrets stay out of appsettings.json.</summary>
    public string CredentialsJson { get; set; } = string.Empty;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(ProjectId)
        && (!string.IsNullOrWhiteSpace(CredentialsPath) || !string.IsNullOrWhiteSpace(CredentialsJson));
}
