using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Homeocentrum.Niga.API.Domain.Configuration;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Services
{
    /// <summary>
    /// Patient SMS + WhatsApp notices (reschedule / cancel / waitlist / tele ready).
    /// The appointment write is already saved before this runs. A failed or unsent notice is logged
    /// on NotificationOutbox and does not undo the cancel or reschedule.
    /// SMS via <see cref="ISmsSender"/>. WhatsApp via Meta when configured. Push stays deferred.
    /// </summary>
    public sealed class AppointmentRescheduleNotifier : IAppointmentRescheduleNotifier
    {
        private readonly ISmsSender _sms;
        private readonly IWhatsAppMetaApiClient _whatsApp;
        private readonly WhatsAppMetaOptions _whatsAppOptions;
        private readonly SmsOptions _smsOptions;
        private readonly INotificationOutbox _outbox;
        private readonly ILogger<AppointmentRescheduleNotifier> _logger;

        public AppointmentRescheduleNotifier(
            ISmsSender sms,
            IWhatsAppMetaApiClient whatsApp,
            IOptions<WhatsAppMetaOptions> whatsAppOptions,
            IOptions<SmsOptions> smsOptions,
            INotificationOutbox outbox,
            ILogger<AppointmentRescheduleNotifier> logger)
        {
            _sms = sms;
            _whatsApp = whatsApp;
            _whatsAppOptions = whatsAppOptions.Value ?? new WhatsAppMetaOptions();
            _smsOptions = smsOptions.Value ?? new SmsOptions();
            _outbox = outbox;
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
                if (SmsProviderIsLive())
                {
                    notice.Sms = accepted ? "sent" : "logged";
                    if (!accepted)
                    {
                        await _outbox.EnqueueAsync("SMS", "NOTICE", mobile, message, "LOGGED",
                            "SMS provider did not accept the notice. The appointment change is saved.");
                    }
                }
                else
                {
                    notice.Sms = "logged";
                    if (!accepted)
                    {
                        await _outbox.EnqueueAsync("SMS", "NOTICE", mobile, message, "LOGGED",
                            "SMS is off or has no provider keys. The appointment change is saved.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Patient SMS notice failed.");
                notice.Sms = "logged";
                await _outbox.EnqueueAsync("SMS", "NOTICE", mobile, message, "LOGGED",
                    "SMS notice failed. The appointment change is saved. " + ex.Message);
            }

            if (!whatsAppOptIn)
            {
                notice.WhatsApp = "skipped";
            }
            else if (!_whatsAppOptions.IsConfigured())
            {
                await _outbox.EnqueueAsync("WhatsApp", "NOTICE", mobile, message, "PENDING_KEYS",
                    "Fill WhatsAppMeta AccessToken and PhoneNumberId in API appsettings. The appointment change is saved.");
                notice.WhatsApp = "logged";
            }
            else
            {
                try
                {
                    var to = WhatsAppMessageTemplateEngine.FormatForWhatsAppApi(
                        mobile,
                        _whatsAppOptions.DefaultCountryDialCode);
                    var (ok, _, err) = await _whatsApp.SendTextMessageAsync(to, message, cancellationToken);
                    if (ok)
                    {
                        notice.WhatsApp = "sent";
                    }
                    else
                    {
                        notice.WhatsApp = "logged";
                        _logger.LogWarning("WhatsApp notice failed. Error={Error}", err);
                        await _outbox.EnqueueAsync("WhatsApp", "NOTICE", mobile, message, "LOGGED",
                            string.IsNullOrWhiteSpace(err)
                                ? "WhatsApp did not accept the notice. The appointment change is saved."
                                : err);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WhatsApp notice failed.");
                    notice.WhatsApp = "logged";
                    await _outbox.EnqueueAsync("WhatsApp", "NOTICE", mobile, message, "LOGGED",
                        "WhatsApp notice failed. The appointment change is saved. " + ex.Message);
                }
            }

            notice.Detail = DescribeNotice(notice);
            return notice;
        }

        private bool SmsProviderIsLive()
        {
            if (!_smsOptions.Enabled)
                return false;
            var provider = (_smsOptions.Provider ?? "Stub").Trim();
            if (provider.Equals("Msg91", StringComparison.OrdinalIgnoreCase))
                return _smsOptions.Msg91.IsConfigured();
            if (provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase))
                return _smsOptions.Twilio.IsConfigured();
            return false;
        }

        private static string DescribeNotice(AppointmentNotificationResult notice)
        {
            var sms = notice.Sms == "sent"
                ? "SMS was sent."
                : notice.Sms == "logged"
                    ? "SMS was logged and was not sent."
                    : "SMS was skipped.";
            var whatsApp = notice.WhatsApp == "sent"
                ? "WhatsApp was sent."
                : notice.WhatsApp == "logged"
                    ? "WhatsApp was logged and was not sent."
                    : "WhatsApp was skipped.";
            return "The appointment change is saved. " + sms + " " + whatsApp + " Push was not sent.";
        }
    }
}
