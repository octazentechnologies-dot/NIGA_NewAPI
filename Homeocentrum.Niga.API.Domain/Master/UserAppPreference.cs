using System;

namespace Homeocentrum.Niga.API.Domain.Master;

/// <summary>PAT-01.02 — Patient/doctor app preference (language, welcome version).</summary>
public class UserAppPreference
{
    public long UserId { get; set; }
    public int? PreferredLanguageId { get; set; }
    public string? WelcomeVersionSeen { get; set; }
    public DateTime UpdatedAt { get; set; }
}
