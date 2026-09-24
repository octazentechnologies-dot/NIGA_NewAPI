using Niga_Domain.DTOs;

namespace Niga_Domain.Services
{
    /// <summary>
    /// Patient SMS / WhatsApp after appointment or tele events are already saved.
    /// </summary>
    public interface IAppointmentRescheduleNotifier
    {
        Task<AppointmentNotificationResult> NotifyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string oldSlot,
            string newSlot,
            CancellationToken cancellationToken = default);

        Task<AppointmentNotificationResult> NotifyCancelAsync(
            string? mobile,
            bool whatsAppOptIn,
            string slotLabel,
            string reasonCode,
            CancellationToken cancellationToken = default);

        Task<AppointmentNotificationResult> NotifyWaitlistOfferAsync(
            string? mobile,
            string? contactName,
            string? slotDate,
            string? slotTime,
            CancellationToken cancellationToken = default);

        Task<AppointmentNotificationResult> NotifyTeleReadyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string roomId,
            CancellationToken cancellationToken = default);

        Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            CancellationToken cancellationToken = default);
    }
}
