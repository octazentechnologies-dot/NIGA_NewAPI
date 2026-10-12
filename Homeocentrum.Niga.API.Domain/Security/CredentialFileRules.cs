namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>Credential uploads: block executable/script extensions; keep documents and images.</summary>
public static class CredentialFileRules
{
    public const long MaxDocumentBytes = 10_000_000;
    public const long MaxPhotoBytes = 5_000_000;

    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".msi", ".ps1", ".js", ".vbs", ".dll", ".scr", ".hta", ".jar", ".sh"
    };

    private static readonly Dictionary<string, string> PhotoTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp"
    };

    public static bool TryGetExtension(string? fileName, out string extension)
    {
        extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        return extension.Length > 1 && !Blocked.Contains(extension);
    }

    /// <summary>Null when the file can be stored; otherwise the reason shown to the client.</summary>
    public static string? RejectReason(string? fileName, long length)
    {
        if (length <= 0)
            return "File is empty.";
        if (length > MaxDocumentBytes)
            return "File is larger than 10 MB.";
        return TryGetExtension(fileName, out _) ? null : "This file type cannot be stored.";
    }

    public static bool TryGetPhotoType(string? fileName, out string extension, out string contentType)
    {
        extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        return PhotoTypes.TryGetValue(extension, out contentType!);
    }

    public static string PhotoContentType(string? path)
        => PhotoTypes.TryGetValue(Path.GetExtension(path) ?? string.Empty, out var type) ? type : "image/jpeg";

    /// <summary>Qualification, Registration, Experience or Other.</summary>
    public static string CanonicalType(string? documentType)
    {
        var type = (documentType ?? string.Empty).Trim();
        foreach (var known in new[] { "Qualification", "Registration", "Experience" })
        {
            if (type.Equals(known, StringComparison.OrdinalIgnoreCase))
                return known;
        }
        return "Other";
    }

    public static string DownloadUrl(int doctorCredentialDocumentId)
        => $"/api/Profile/CredentialDocuments/{doctorCredentialDocumentId}/File";
}
