using Niga_Domain.DTOs;

namespace Niga_Domain.Services
{
    /// <summary>
    /// APT-05.04 — patient notice after a reschedule is already saved.
    /// </summary>
    public interface IAppointmentRescheduleNotifier
    {
        Task<AppointmentNotificationResult> NotifyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string oldSlot,
            string newSlot,
            CancellationToken cancellationToken = default);
    }
}
