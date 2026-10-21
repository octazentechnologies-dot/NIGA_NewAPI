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
    /// Patient SMS + WhatsApp notices (confirm / reschedule / cancel / waitlist / tele / remind).
    /// Bodies come from dbo.SmsTemplate (catalog fallback). SMS via <see cref="ISmsSender"/>.
    /// </summary>
    public sealed class AppointmentRescheduleNotifier : IAppointmentRescheduleNotifier
    {
        private readonly ISmsSender _sms;
        private readonly ISmsTemplateRenderer _templates;
        private readonly IWhatsAppMetaApiClient _whatsApp;
        private readonly WhatsAppMetaOptions _whatsAppOptions;
        private readonly SmsOptions _smsOptions;
        private readonly INotificationOutbox _outbox;
        private readonly IPushNotificationSender _push;
        private readonly ILogger<AppointmentRescheduleNotifier> _logger;

        public AppointmentRescheduleNotifier(
            ISmsSender sms,
            ISmsTemplateRenderer templates,
            IWhatsAppMetaApiClient whatsApp,
            IOptions<WhatsAppMetaOptions> whatsAppOptions,
            IOptions<SmsOptions> smsOptions,
            INotificationOutbox outbox,
            IPushNotificationSender push,
            ILogger<AppointmentRescheduleNotifier> logger)
        {
            _sms = sms;
            _templates = templates;
            _whatsApp = whatsApp;
            _whatsAppOptions = whatsAppOptions.Value ?? new WhatsAppMetaOptions();
            _smsOptions = smsOptions.Value ?? new SmsOptions();
            _outbox = outbox;
            _push = push;
            _logger = logger;
        }

        public async Task<AppointmentNotificationResult> NotifyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string oldSlot,
            string newSlot,
            CancellationToken cancellationToken = default)
        {
            var message = await _templates.RenderAsync("APPT_RESCHEDULE", new Dictionary<string, string?>
            {
                ["oldSlot"] = oldSlot,
                ["newSlot"] = newSlot
            }, cancellationToken);
            return await NotifyChannelsAsync(
                mobile, whatsAppOptIn, message, null, "Appointment rescheduled", "appointment_reschedule",
                new Dictionary<string, string>
                {
                    ["action"] = "appointment_reschedule",
                    ["type"] = "APPOINTMENT",
                    ["deepLink"] = "/patientdashboard"
                }, cancellationToken);
        }

        public async Task<AppointmentNotificationResult> NotifyCancelAsync(
            string? mobile,
            bool whatsAppOptIn,
            string slotLabel,
            string reasonCode,
            CancellationToken cancellationToken = default)
        {
            var reason = string.IsNullOrWhiteSpace(reasonCode) ? "." : $" ({reasonCode.Trim()}).";
            var message = await _templates.RenderAsync("APPT_CANCEL", new Dictionary<string, string?>
            {
                ["slot"] = slotLabel,
                ["reason"] = reason
            }, cancellationToken);
            return await NotifyChannelsAsync(
                mobile, whatsAppOptIn, message, null, "Appointment cancelled", "appointment_cancel",
                new Dictionary<string, string>
                {
                    ["action"] = "appointment_cancel",
                    ["type"] = "APPOINTMENT",
                    ["deepLink"] = "/patientdashboard"
                }, cancellationToken);
        }

        public async Task<AppointmentNotificationResult> NotifyWaitlistOfferAsync(
            string? mobile,
            string? contactName,
            string? slotDate,
            string? slotTime,
            CancellationToken cancellationToken = default)
        {
            var who = string.IsNullOrWhiteSpace(contactName) ? "there" : contactName.Trim();
            var when = $"{slotDate ?? "the open day"} {slotTime ?? ""}".Trim();
            var message = await _templates.RenderAsync("WAITLIST_OFFER", new Dictionary<string, string?>
            {
                ["name"] = who,
                ["when"] = when
            }, cancellationToken);
            return await NotifyChannelsAsync(
                mobile, true, message, null, "Slot available", "waitlist_offer",
                new Dictionary<string, string>
                {
                    ["action"] = "waitlist_offer",
                    ["type"] = "APPOINTMENT",
                    ["deepLink"] = "/patient/waitlist"
                }, cancellationToken);
        }

        public async Task<AppointmentNotificationResult> NotifyTeleReadyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string roomId,
            CancellationToken cancellationToken = default)
        {
            var message = await _templates.RenderAsync("TELE_READY", new Dictionary<string, string?>
            {
                ["room"] = roomId
            }, cancellationToken);
            return await NotifyChannelsAsync(
                mobile, whatsAppOptIn, message, null, "Tele consultation ready", "tele_ready",
                new Dictionary<string, string>
                {
                    ["action"] = "tele_ready",
                    ["type"] = "APPOINTMENT",
                    ["room"] = roomId ?? "",
                    ["deepLink"] = "/patient/rejoin"
                }, cancellationToken);
        }

        public Task<AppointmentNotificationResult> NotifyConfirmAsync(
            string? mobile,
            bool whatsAppOptIn,
            string doctorName,
            string slotDate,
            string slotTime,
            CancellationToken cancellationToken = default)
            => NotifyConfirmAsync(mobile, whatsAppOptIn, doctorName, slotDate, slotTime, null, cancellationToken);

        public async Task<AppointmentNotificationResult> NotifyConfirmAsync(
            string? mobile,
            bool whatsAppOptIn,
            string doctorName,
            string slotDate,
            string slotTime,
            long? recipientUserId,
            CancellationToken cancellationToken = default)
        {
            var doctor = string.IsNullOrWhiteSpace(doctorName) ? "your doctor" : doctorName.Trim();
            var message = await _templates.RenderAsync("APPT_CONFIRM", new Dictionary<string, string?>
            {
                ["doctor"] = doctor,
                ["date"] = slotDate,
                ["time"] = slotTime
            }, cancellationToken);
            return await NotifyChannelsAsync(
                mobile, whatsAppOptIn, message, recipientUserId, "Appointment booked", "appointment_confirm",
                new Dictionary<string, string>
                {
                    ["action"] = "appointment_confirm",
                    ["type"] = "APPOINTMENT",
                    ["date"] = slotDate ?? "",
                    ["time"] = slotTime ?? "",
                    ["deepLink"] = "/patientdashboard"
                }, cancellationToken);
        }

        public async Task<AppointmentNotificationResult> NotifyReminderAsync(
            string? mobile,
            bool whatsAppOptIn,
            string doctorName,
            string slotDate,
            string slotTime,
            CancellationToken cancellationToken = default)
        {
            var doctor = string.IsNullOrWhiteSpace(doctorName) ? "your doctor" : doctorName.Trim();
            var message = await _templates.RenderAsync("APPT_REMIND", new Dictionary<string, string?>
            {
                ["doctor"] = doctor,
                ["date"] = slotDate,
                ["time"] = slotTime,
                ["title"] = $"appointment with {doctor}",
                ["due"] = $"{slotDate} {slotTime}".Trim()
            }, cancellationToken);
            return await NotifyChannelsAsync(
                mobile, whatsAppOptIn, message, null, "Appointment reminder", "appointment_remind",
                new Dictionary<string, string>
                {
                    ["action"] = "appointment_remind",
                    ["type"] = "APPOINTMENT",
                    ["date"] = slotDate ?? "",
                    ["time"] = slotTime ?? "",
                    ["deepLink"] = "/patientdashboard"
                }, cancellationToken);
        }

        public Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            CancellationToken cancellationToken = default)
            => NotifyChannelsAsync(mobile, whatsAppOptIn, message, null, null, "general", null, cancellationToken);

        public Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            long? recipientUserId,
            string? pushTitle,
            CancellationToken cancellationToken = default)
            => NotifyChannelsAsync(mobile, whatsAppOptIn, message, recipientUserId, pushTitle, "general", null, cancellationToken);

        public async Task<AppointmentNotificationResult> NotifyChannelsAsync(
            string? mobile,
            bool whatsAppOptIn,
            string message,
            long? recipientUserId,
            string? pushTitle,
            string? pushAction,
            IReadOnlyDictionary<string, string>? pushData,
            CancellationToken cancellationToken = default)
        {
            var notice = new AppointmentNotificationResult
            {
                Message = message,
                Push = "skipped"
            };

            if (string.IsNullOrWhiteSpace(mobile))
            {
                notice.Sms = "skipped";
                notice.WhatsApp = "skipped";
                await SendPushPartAsync(notice, recipientUserId, mobile, pushTitle, message, pushAction, pushData, cancellationToken);
                notice.Detail = DescribeNotice(notice);
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
                        await _outbox.EnqueueAsync("SMS", "NOTICE", mobile, message, "QUEUED",
                            "SMS provider did not accept the notice. Worker will retry.");
                    }
                }
                else
                {
                    notice.Sms = "logged";
                    if (!accepted)
                    {
                        await _outbox.EnqueueAsync("SMS", "NOTICE", mobile, message, "PENDING_KEYS",
                            "SMS is off or has no provider keys. The appointment change is saved.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Patient SMS notice failed.");
                notice.Sms = "logged";
                await _outbox.EnqueueAsync("SMS", "NOTICE", mobile, message, "QUEUED",
                    "SMS notice failed. Worker will retry. " + ex.Message);
            }

            if (!whatsAppOptIn)
            {
                notice.WhatsApp = "skipped";
            }
            else if (!IsWhatsAppReady())
            {
                await _outbox.EnqueueAsync("WhatsApp", "NOTICE", mobile, message, "PENDING_KEYS",
                    UsesTwilioWhatsApp() || !_whatsAppOptions.IsMetaProvider()
                        ? "Fill Sms:Twilio AccountSid, AuthToken, and FromNumber for WhatsApp. The appointment change is saved."
                        : "Fill WhatsAppMeta AccessToken and PhoneNumberId in API appsettings. The appointment change is saved.");
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

            await SendPushPartAsync(notice, recipientUserId, mobile, pushTitle, message, pushAction, pushData, cancellationToken);
            notice.Detail = DescribeNotice(notice);
            return notice;
        }

        private async Task SendPushPartAsync(
            AppointmentNotificationResult notice,
            long? recipientUserId,
            string? mobile,
            string? pushTitle,
            string message,
            string? pushAction,
            IReadOnlyDictionary<string, string>? pushData,
            CancellationToken cancellationToken)
        {
            try
            {
                var userId = recipientUserId ?? await _push.ResolveUserIdByMobileAsync(mobile, cancellationToken);
                if (!userId.HasValue || userId.Value <= 0)
                {
                    notice.Push = "skipped";
                    return;
                }

                var title = string.IsNullOrWhiteSpace(pushTitle) ? "Homeocentrum" : pushTitle.Trim();
                var push = await _push.SendToUserAsync(
                    userId.Value,
                    title,
                    message,
                    pushData,
                    pushAction,
                    cancellationToken);
                notice.Push = push.Status;
                if (!string.IsNullOrWhiteSpace(push.Detail) && push.Status is "logged" or "failed")
                    await _outbox.EnqueueAsync("Push", "NOTICE", "user:" + userId.Value, message, push.Status.ToUpperInvariant(), push.Detail);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notice failed.");
                notice.Push = "logged";
            }
        }

        private bool UsesTwilioWhatsApp() =>
            !_whatsAppOptions.IsMetaProvider() && _smsOptions.Twilio.IsConfigured();

        private bool IsWhatsAppReady() =>
            UsesTwilioWhatsApp() || _whatsAppOptions.IsConfigured();

        private bool SmsProviderIsLive()
        {
            if (!_smsOptions.Enabled)
                return false;
            var provider = (_smsOptions.Provider ?? "Stub").Trim();
            return provider.Equals("Twilio", StringComparison.OrdinalIgnoreCase)
                && _smsOptions.Twilio.IsConfigured();
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
            var push = notice.Push == "sent"
                ? "Push was sent."
                : notice.Push == "logged"
                    ? "Push was logged and was not sent."
                    : notice.Push == "failed"
                        ? "Push failed."
                        : "Push was skipped.";
            return "The appointment change is saved. " + sms + " " + whatsApp + " " + push;
        }
    }
}
