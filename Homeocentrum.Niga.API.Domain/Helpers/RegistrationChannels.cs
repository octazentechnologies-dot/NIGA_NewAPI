namespace Homeocentrum.Niga.API.Domain.Helpers;

/// <summary>
/// Where a Patient / Doctor / User account was first created.
/// Stored on RegistrationChannel columns; also used as BookingChannel when appropriate.
/// </summary>
public static class RegistrationChannels
{
    public const string PatientApp = "PatientApp";
    public const string DoctorApp = "DoctorApp";
    public const string WebPortal = "WebPortal";
    public const string Reception = "Reception";
    public const string PublicBooking = "PublicBooking";
    public const string Admin = "Admin";
    public const string Staff = "Staff";

    /// <summary>Maps legacy PatientModel.EntityType to a stored channel.</summary>
    public static string FromEntityType(string? entityType)
    {
        var t = (entityType ?? "").Trim();
        if (string.Equals(t, "PatientMobile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, PatientApp, StringComparison.OrdinalIgnoreCase))
            return PatientApp;
        if (string.Equals(t, "DoctorMobile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, DoctorApp, StringComparison.OrdinalIgnoreCase))
            return DoctorApp;
        if (string.Equals(t, "Reception", StringComparison.OrdinalIgnoreCase))
            return Reception;
        if (string.Equals(t, "Admin", StringComparison.OrdinalIgnoreCase))
            return Admin;
        if (string.IsNullOrWhiteSpace(t) || string.Equals(t, "Web", StringComparison.OrdinalIgnoreCase)
            || string.Equals(t, WebPortal, StringComparison.OrdinalIgnoreCase))
            return WebPortal;
        return t.Length <= 40 ? t : t[..40];
    }

    public static string FromRequestHeader(string? xClientPlatform)
        => FromEntityType(xClientPlatform);

    public static string DisplayLabel(string? channel) => (channel ?? "").Trim() switch
    {
        PatientApp => "Patient App",
        DoctorApp => "Doctor App",
        WebPortal => "Web Portal",
        Reception => "Reception",
        PublicBooking => "Public booking",
        Admin => "Admin",
        Staff => "Staff",
        "" => "Unknown",
        _ => channel!
    };
}
