namespace Homeocentrum.Niga.API.Domain.Master;

/// <summary>History of FCM pushes sent (or attempted) to a user's devices.</summary>
public class FirebaseNotificationLog
{
    public long FirebaseNotificationLogId { get; set; }

    public long UserId { get; set; }

    /// <summary>appointment_confirm | appointment_cancel | appointment_remind | …</summary>
    public string Action { get; set; } = "";

    public string Title { get; set; } = "";

    public string Body { get; set; } = "";

    public int TokenCount { get; set; }

    public int SentCount { get; set; }

    /// <summary>sent | logged | skipped | failed</summary>
    public string Status { get; set; } = "";

    public string? Detail { get; set; }

    public DateTime CreatedAt { get; set; }
}
