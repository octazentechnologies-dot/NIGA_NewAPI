namespace Homeocentrum.Niga.NewAPI.Domain.Configuration;

public class AppInfoOptions
{
    public const string SectionName = "App";

    /// <summary>Manual deploy marker. Bump on each IIS publish when no CI injects a git SHA.</summary>
    public string Version { get; set; } = "2026.08.17";
}
