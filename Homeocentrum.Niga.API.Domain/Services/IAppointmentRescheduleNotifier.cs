using Homeocentrum.Niga.API.Domain.DTOs;

namespace Homeocentrum.Niga.API.Domain.Services
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

        Task<AppointmentNotificationResult> NotifyConfirmAsync(
            string? mobile,
            bool whatsAppOptIn,
            string doctorName,
            string slotDate,
            string slotTime,
            CancellationToken cancellationToken = default);

        /// <summary>Same as confirm SMS/WhatsApp, with FCM targeted at <paramref name="recipientUserId"/> when set.</summary>
        Task<AppointmentNotificationResult> NotifyConfirmAsync(
            string? mobile,
            bool whatsAppOptIn,
            string doctorName,
            string slotDate,
            string slotTime,
            long? recipientUserId,
            CancellationToken cancellationToken = default);

        Task<AppointmentNotificationResult> NotifyReminderAsync(
            string? mobile,
            bool whatsAppOptIn,
            string doctorName,
            string slotDate,
            string slotTime,
            CancellationToken cancellationToken = default);

        Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Same as <see cref="NotifyChannelsAsync(string?,bool,string,CancellationToken)"/> plus FCM push
        /// to the recipient's registered devices (resolves UserId from mobile when omitted).
        /// </summary>
        Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            long? recipientUserId,
            string? pushTitle,
            CancellationToken cancellationToken = default);

        /// <summary>SMS / WhatsApp / FCM with explicit push action + data for the client.</summary>
        Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            long? recipientUserId,
            string? pushTitle,
            string? pushAction,
            IReadOnlyDictionary<string, string>? pushData,
            CancellationToken cancellationToken = default);
    }
}
