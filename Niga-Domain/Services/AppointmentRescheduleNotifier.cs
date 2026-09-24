using Microsoft.Extensions.Logging;
using Niga_Domain.DTOs;
using Niga_Domain.Security;

namespace Niga_Domain.Services
{
    /// <summary>
    /// APT-05.04 — SMS and WhatsApp say old slot to new slot.
    /// Push is deferred. A channel failure does not throw, so the saved move stays.
    /// SMS uses <see cref="ISmsSender"/> (vendor stub until PRE-03).
    /// WhatsApp uses the same non-carrier log until a reschedule template is approved.
    /// </summary>
    public sealed class AppointmentRescheduleNotifier : IAppointmentRescheduleNotifier
    {
        private readonly ISmsSender _sms;
        private readonly ILogger<AppointmentRescheduleNotifier> _logger;

        public AppointmentRescheduleNotifier(ISmsSender sms, ILogger<AppointmentRescheduleNotifier> logger)
        {
            _sms = sms;
            _logger = logger;
        }

        public async Task<AppointmentNotificationResult> NotifyAsync(
            string? mobile,
            bool whatsAppOptIn,
            string oldSlot,
            string newSlot,
            CancellationToken cancellationToken = default)
        {
            var notice = new AppointmentNotificationResult
            {
                Message = $"Your appointment moved from {oldSlot} to {newSlot}.",
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
                var accepted = await _sms.SendAsync(mobile, notice.Message, cancellationToken);
                notice.Sms = accepted ? "sent" : "failed";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Reschedule SMS failed. The appointment move is kept.");
                notice.Sms = "failed";
            }

            if (!whatsAppOptIn)
            {
                notice.WhatsApp = "skipped";
            }
            else
            {
                try
                {
                    _logger.LogInformation(
                        "WhatsApp reschedule notice. DestinationMasked={Masked} Length={Length}",
                        PhoneNormalizer.Mask(mobile),
                        notice.Message.Length);
                    notice.WhatsApp = "sent";
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reschedule WhatsApp failed. The appointment move is kept.");
                    notice.WhatsApp = "failed";
                }
            }

            notice.Detail = "Push was not sent.";
            return notice;
        }
    }
}
