using System.Globalization;

namespace Homeocentrum.Niga.API.Domain.Helpers;

/// <summary>
/// Clock text for patient and doctor notices. AppointmentTime is the clinic slot
/// the patient booked, so this prints that local time in 12-hour form (2:30 PM).
/// </summary>
public static class NotificationClock
{
    public static string Format12Hour(TimeOnly time) =>
        time.ToString("h:mm tt", CultureInfo.InvariantCulture);

    public static string Format12Hour(TimeOnly? time) =>
        time.HasValue ? Format12Hour(time.Value) : "";

    public static string Format12Hour(string? clock)
    {
        if (string.IsNullOrWhiteSpace(clock))
            return "";

        var text = clock.Trim();
        string[] formats =
        {
            "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss",
            "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt"
        };
        if (TimeOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var exact)
            || TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out exact))
            return Format12Hour(exact);

        return text;
    }

    public static string FormatVisit(DateTime date, TimeOnly? time)
    {
        var day = date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        var clock = Format12Hour(time);
        return string.IsNullOrEmpty(clock) ? day : day + " " + clock;
    }

    public static string FormatVisit(DateTime date, string? clock)
    {
        var day = date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);
        var formatted = Format12Hour(clock);
        return string.IsNullOrEmpty(formatted) ? day : day + " " + formatted;
    }
}
