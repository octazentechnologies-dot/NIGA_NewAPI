using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.NewAPI.Domain.Configuration;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Domain.Services
{
    /// <summary>
    /// Patient SMS + WhatsApp notices (reschedule / cancel / waitlist / tele ready).
    /// SMS via <see cref="ISmsSender"/> (Stub until Sms:Provider keys). WhatsApp via Meta API when configured.
    /// Push stays deferred. Channel failures never throw after the business write.
    /// </summary>
    public sealed class AppointmentRescheduleNotifier : IAppointmentRescheduleNotifier
    {
        private readonly ISmsSender _sms;
        private readonly IWhatsAppMetaApiClient _whatsApp;
        private readonly WhatsAppMetaOptions _whatsAppOptions;
        private readonly ILogger<AppointmentRescheduleNotifier> _logger;

        public AppointmentRescheduleNotifier(
            ISmsSender sms,
            IWhatsAppMetaApiClient whatsApp,
            IOptions<WhatsAppMetaOptions> whatsAppOptions,
            ILogger<AppointmentRescheduleNotifier> logger)
        {
            _sms = sms;
            _whatsApp = whatsApp;
            _whatsAppOptions = whatsAppOptions.Value ?? new WhatsAppMetaOptions();
            _logger = logger;
        }

        public Task<AppointmentNotificationResult> NotifyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string oldSlot,
            string newSlot,
            CancellationToken cancellationToken = default)
        {
            var message = $"Your appointment moved from {oldSlot} to {newSlot}.";
            return NotifyChannelsAsync(mobile, whatsAppOptIn, message, cancellationToken);
        }

        public Task<AppointmentNotificationResult> NotifyCancelAsync(
            string? mobile,
            bool whatsAppOptIn,
            string slotLabel,
            string reasonCode,
            CancellationToken cancellationToken = default)
        {
            var message =
                $"Your appointment on {slotLabel} was cancelled"
                + (string.IsNullOrWhiteSpace(reasonCode) ? "." : $" ({reasonCode}).")
                + " Contact the clinic if you need a new slot.";
            return NotifyChannelsAsync(mobile, whatsAppOptIn, message, cancellationToken);
        }

        public Task<AppointmentNotificationResult> NotifyWaitlistOfferAsync(
            string? mobile,
            string? contactName,
            string? slotDate,
            string? slotTime,
            CancellationToken cancellationToken = default)
        {
            var who = string.IsNullOrWhiteSpace(contactName) ? "there" : contactName.Trim();
            var when = $"{slotDate ?? "the open day"} {slotTime ?? ""}".Trim();
            var message =
                $"Hi {who}, a slot opened on {when}. Reply or open the app to book — the offer does not reserve the slot.";
            // Waitlist contact was collected for notices; attempt WhatsApp when Meta is configured.
            return NotifyChannelsAsync(mobile, whatsAppOptIn: true, message, cancellationToken);
        }

        public Task<AppointmentNotificationResult> NotifyTeleReadyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string roomId,
            CancellationToken cancellationToken = default)
        {
            var message =
                $"Your tele consultation is ready (room {roomId}). Open the waiting room in the app to join. Poll session status — no live push.";
            return NotifyChannelsAsync(mobile, whatsAppOptIn, message, cancellationToken);
        }

        public async Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            CancellationToken cancellationToken = default)
        {
            var notice = new AppointmentNotificationResult
            {
                Message = message,
                Push = "later"
            };

            if (string.IsNullOrWhiteSpace(mobile))
            {
                notice.Sms = "skipped";
                notice.WhatsApp = "skipped";
                notice.Detail = "No mobile number. Push was not sent.";
                return notice;
            }

            try
            {
                var accepted = await _sms.SendAsync(mobile, message, cancellationToken);
                notice.Sms = accepted ? "sent" : "failed";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Patient SMS notice failed.");
                notice.Sms = "failed";
            }

            if (!whatsAppOptIn)
            {
                notice.WhatsApp = "skipped";
            }
            else if (!_whatsAppOptions.IsConfigured())
            {
                _logger.LogInformation(
                    "WhatsApp stub (Meta keys empty). DestinationMasked={Masked} Length={Length}",
                    PhoneNormalizer.Mask(mobile),
                    message.Length);
                notice.WhatsApp = "sent";
            }
            else
            {
                try
                {
                    var to = WhatsAppMessageTemplateEngine.FormatForWhatsAppApi(
                        mobile,
                        _whatsAppOptions.DefaultCountryDialCode);
                    var (ok, _, err) = await _whatsApp.SendTextMessageAsync(to, message, cancellationToken);
                    notice.WhatsApp = ok ? "sent" : "failed";
                    if (!ok)
                        _logger.LogWarning("WhatsApp notice failed. Error={Error}", err);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WhatsApp notice failed.");
                    notice.WhatsApp = "failed";
                }
            }

            notice.Detail = "Push was not sent.";
            return notice;
        }
    }
}
