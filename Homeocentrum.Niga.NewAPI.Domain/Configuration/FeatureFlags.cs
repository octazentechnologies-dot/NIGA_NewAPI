using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.NewAPI.Domain.Configuration;

/// <summary>
/// On/off switches from the FeatureFlags section of appsettings.json. Read once at startup, so a change
/// needs an app restart (recycle the app pool). Missing keys keep the defaults below.
/// </summary>
public sealed class FeatureFlags
{
    public const string SectionName = "FeatureFlags";
    public const string DefaultMaintenanceMessage = "The service is under maintenance. Please try again shortly.";

    public bool EnableSwagger { get; set; } = true;
    public bool ExposeExceptionDetails { get; set; }
    public bool EnableDetailedLogging { get; set; }
    public bool EnableRequestLogging { get; set; } = true;
    public bool EnableResponseLogging { get; set; }
    public bool EnableSensitiveDataLogging { get; set; }
    public bool EnableAuditLogging { get; set; } = true;
    public bool EnableRefreshToken { get; set; } = true;
    public bool EnableRateLimiting { get; set; } = true;
    public bool EnableCors { get; set; } = true;
    public bool EnableSecurityHeaders { get; set; } = true;
    public bool EnableMaintenanceMode { get; set; }
    public string MaintenanceMessage { get; set; } = DefaultMaintenanceMessage;
    public bool EnableResponseCompression { get; set; } = true;
    public bool EnableBackgroundJobs { get; set; } = true;

    public static FeatureFlags Current { get; private set; } = new();

    public static FeatureFlags Load(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var flags = section.Get<FeatureFlags>() ?? new FeatureFlags();
        if (string.IsNullOrWhiteSpace(flags.MaintenanceMessage))
            flags.MaintenanceMessage = DefaultMaintenanceMessage;
        Current = flags;
        return flags;
    }
}
