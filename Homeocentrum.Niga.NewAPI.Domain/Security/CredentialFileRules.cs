namespace Homeocentrum.Niga.NewAPI.Domain.Security;

/// <summary>Optional doctor media. Any file except programs is stored, and the path is saved.</summary>
public static class CredentialFileRules
{
    private static readonly HashSet<string> Blocked = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".bat", ".cmd", ".com", ".msi", ".ps1", ".js", ".vbs", ".dll", ".scr", ".hta", ".jar", ".sh"
    };

    public static bool TryGetExtension(string? fileName, out string extension)
    {
        extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? string.Empty;
        return extension.Length > 1 && !Blocked.Contains(extension);
    }
}
