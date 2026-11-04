using Homeocentrum.Niga.API.Domain.Master;

namespace Homeocentrum.Niga.API.Domain.Helpers;

/// <summary>
/// Missed-slot grace: OVERDUE for 24h (reschedule allowed), then EXPIRED (no reschedule, no refund).
/// Patient reschedule cap: MaxPatientReschedules successful RescheduleAppointment calls per visit.
/// </summary>
public static class AppointmentLifecycle
{
    public const int GraceHours = 24;
    public const int GraceWarningHoursBeforeEnd = 4;
    /// <summary>Hard cap on patient/clinic RescheduleAppointment for one PatientAppId.</summary>
    public const int MaxPatientReschedules = 2;

    public static int GetRescheduleCount(PatientAppointment? appointment)
        => appointment == null ? 0 : Math.Max(0, appointment.RescheduleCount);

    public static int GetReschedulesRemaining(PatientAppointment? appointment)
        => Math.Max(0, MaxPatientReschedules - GetRescheduleCount(appointment));

    public static bool HasRescheduleQuota(PatientAppointment? appointment)
        => GetReschedulesRemaining(appointment) > 0;

    /// <summary>
    /// Patient-facing status label key for apps (stored Status stays WAITING/EARLY/…).
    /// </summary>
    public static string ResolveDisplayStatus(PatientAppointment appointment, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        var status = (appointment.Status ?? "").Trim().ToUpperInvariant();
        if (S3AppointmentRules.IsCompleted(status)) return "Completed";
        if (S3AppointmentRules.IsCancelled(status)) return "Cancelled";
        if (IsExpired(status)) return "Expired";
        if (IsOverdue(status)) return "Missed";

        return status switch
        {
            "EARLY" => "ArrivedEarly",
            "READY" => "Ready",
            "REMAINING" => "InProgress",
            "WALK-IN" => "AtClinic",
            "E-CONSULT" => "Tele",
            "WAITING" => ResolveWaitingDisplay(appointment, clock),
            _ => string.IsNullOrWhiteSpace(appointment.Status) ? "Booked" : appointment.Status!
        };
    }

    private static string ResolveWaitingDisplay(PatientAppointment appointment, DateTime clock)
    {
        var slotAt = ComputeSlotAt(appointment.AppointmentDate, appointment.AppointmentTime);
        var upcoming = slotAt.HasValue && slotAt.Value > clock;
        var paid = S3AppointmentRules.IsPaid(appointment.PaymentStatus);
        if (upcoming)
            return paid ? "Confirmed" : "Booked";
        // Same calendar day / past slot but still WAITING — treat as checked-in queue.
        return "CheckedIn";
    }

    public static DateTime? ComputeSlotAt(DateTime? appointmentDate, TimeOnly? appointmentTime)
    {
        if (!appointmentDate.HasValue)
            return null;
        var tod = appointmentTime?.ToTimeSpan() ?? TimeSpan.Zero;
        return appointmentDate.Value.Date.Add(tod);
    }

    public static DateTime? ComputeSlotAt(DateTime? appointmentDate, TimeSpan? appointmentTime)
    {
        if (!appointmentDate.HasValue)
            return null;
        return appointmentDate.Value.Date.Add(appointmentTime ?? TimeSpan.Zero);
    }

    public static bool IsOverdue(string? status)
        => string.Equals(status?.Trim(), S3AppointmentRules.Overdue, StringComparison.OrdinalIgnoreCase)
           || string.Equals(status?.Trim(), "NOT ARRIVED", StringComparison.OrdinalIgnoreCase);

    public static bool IsExpired(string? status)
        => string.Equals(status?.Trim(), S3AppointmentRules.Expired, StringComparison.OrdinalIgnoreCase);

    public static bool IsTerminalForLifecycle(string? status)
        => S3AppointmentRules.IsCancelled(status)
           || S3AppointmentRules.IsCompleted(status)
           || IsExpired(status);

    /// <summary>Open visit statuses that become OVERDUE after the slot time.</summary>
    public static bool IsOpenForOverdue(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return true;
        if (IsTerminalForLifecycle(status) || IsOverdue(status))
            return false;
        var s = status.Trim().ToUpperInvariant();
        return s is "WAITING" or "WALK-IN" or "E-CONSULT" or "REMAINING" or "EARLY" or "READY" or "NOSHOW" or "NO_SHOW";
    }

    public static DateTime ComputeGraceEndsAt(DateTime slotAt)
        => slotAt.AddHours(GraceHours);

    public static bool CanReschedule(PatientAppointment appointment, DateTime? now = null)
    {
        var clock = now ?? DateTime.Now;
        if (appointment == null || appointment.DeleteStatus == true)
            return false;
        if (IsTerminalForLifecycle(appointment.Status))
            return false;
        if (S3AppointmentRules.IsCompleted(appointment.Status))
            return false;

        if (IsExpired(appointment.Status))
            return false;

        if (!HasRescheduleQuota(appointment))
            return false;

        var slotAt = ComputeSlotAt(appointment.AppointmentDate, appointment.AppointmentTime);

        if (IsOverdue(appointment.Status)
            || (slotAt.HasValue && slotAt.Value <= clock && IsOpenForOverdue(appointment.Status)))
        {
            var graceEnd = appointment.GraceEndsAt
                ?? (slotAt.HasValue ? ComputeGraceEndsAt(slotAt.Value) : (DateTime?)null);
            return graceEnd.HasValue && clock < graceEnd.Value;
        }

        // Upcoming normal visits remain reschedulable.
        return slotAt.HasValue && slotAt.Value > clock;
    }

    public static bool IsRefundBlocked(PatientAppointment? appointment)
    {
        if (appointment == null)
            return false;
        if (appointment.RefundEligible == false)
            return true;
        if (IsExpired(appointment.Status))
            return true;
        return false;
    }

    public static string ActiveStatusAfterReschedule(PatientAppointment appointment)
        => appointment.IsTele == true ? "E-CONSULT" : "WALK-IN";
}
