namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Maps FCM action/data into the web bell inbox (dbo.AppNotification.Type / DeepLink).
/// </summary>
public static class AppNotificationInbox
{
    public static string MapType(string? action, IReadOnlyDictionary<string, string>? data, string? title, string? body)
    {
        if (TryGet(data, "type", out var explicitType))
            return NormalizeType(explicitType);

        var key = (action ?? "").Trim().ToLowerInvariant();
        if (key.Length == 0 && data != null && data.TryGetValue("action", out var fromData) && !string.IsNullOrWhiteSpace(fromData))
            key = fromData.Trim().ToLowerInvariant();

        return key switch
        {
            "appointment_confirm" or "appointment_reschedule" or "appointment_cancel"
                or "appointment_remind" or "waitlist_offer" or "tele_ready"
                or "appt_missed" or "follow_up_missed" or "carelink"
                or "appointment_request"
                => "APPOINTMENT",
            "medicine_status" or "medicine" or "pharmacy" or "refill" => "MEDICINE",
            "diary_remind" or "reminder" => "SYSTEM",
            "pay_link" or "payment" or "refund" or "receipt" => "PAYMENT",
            "prescription" or "erx" => "PRESCRIPTION",
            "marketing" or "promo" or "offer" => "MARKETING",
            "admin_send" or "general" => InferFromText(title, body),
            _ => InferFromText(title, body)
        };
    }

    public static string? MapDeepLink(string? action, IReadOnlyDictionary<string, string>? data)
    {
        if (TryGet(data, "deepLink", out var explicitLink) || TryGet(data, "deeplink", out explicitLink))
            return Truncate(explicitLink, 300);

        var key = (action ?? "").Trim().ToLowerInvariant();
        if (key.Length == 0 && data != null && data.TryGetValue("action", out var fromData) && !string.IsNullOrWhiteSpace(fromData))
            key = fromData.Trim().ToLowerInvariant();

        return key switch
        {
            "appointment_confirm" or "appointment_reschedule" or "appointment_cancel"
                or "appointment_remind" => "/patientdashboard",
            "waitlist_offer" => "/patient/waitlist",
            "appointment_request" => "/patient/appointment-requests",
            "tele_ready" => "/patient/rejoin",
            "appt_missed" or "follow_up_missed" => "/doctor/follow-ups",
            "carelink" => "/doctor/carelink",
            "medicine_status" or "medicine" or "pharmacy" => "/patient/medicine-orders",
            "refill" => "/patient/prescriptions",
            "pay_link" or "payment" => "/payments",
            "prescription" or "erx" => "/patient/prescriptions",
            "diary_remind" or "reminder" => "/patient/reminders",
            _ => null
        };
    }

    private static string InferFromText(string? title, string? body)
    {
        var text = ((title ?? "") + " " + (body ?? "")).ToLowerInvariant();
        if (text.Contains("prescription") || text.Contains("erx") || text.Contains("e-rx")) return "PRESCRIPTION";
        if (text.Contains("pharmacy") || text.Contains("medicine") || text.Contains("refill")) return "MEDICINE";
        if (text.Contains("payout") || text.Contains("payment") || text.Contains("refund") || text.Contains("receipt") || text.Contains("pay link")) return "PAYMENT";
        if (text.Contains("appointment") || text.Contains("visit") || text.Contains("consult") || text.Contains("carelink") || text.Contains("follow-up") || text.Contains("follow up")) return "APPOINTMENT";
        if (text.Contains("promo") || text.Contains("offer") || text.Contains("marketing")) return "MARKETING";
        return "SYSTEM";
    }

    private static string NormalizeType(string raw)
    {
        var t = raw.Trim().ToUpperInvariant();
        return t switch
        {
            "APPOINTMENT" or "PRESCRIPTION" or "MEDICINE" or "PAYMENT" or "SYSTEM" or "MARKETING" => t,
            "CARELINK" or "APPT_MISSED" or "FOLLOW_UP_MISSED" or "WAITLIST" or "TELE" => "APPOINTMENT",
            "PHARMACY" or "REFILL" => "MEDICINE",
            "MONEY" or "PAY_LINK" => "PAYMENT",
            "ERX" => "PRESCRIPTION",
            _ => t.Length == 0 ? "SYSTEM" : t
        };
    }

    private static bool TryGet(IReadOnlyDictionary<string, string>? data, string key, out string value)
    {
        value = "";
        if (data == null) return false;
        foreach (var kv in data)
        {
            if (!string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.IsNullOrWhiteSpace(kv.Value))
                return false;
            value = kv.Value.Trim();
            return true;
        }
        return false;
    }

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
