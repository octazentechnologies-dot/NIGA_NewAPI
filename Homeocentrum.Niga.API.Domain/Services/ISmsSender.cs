using System.Threading;
using System.Threading.Tasks;

namespace Homeocentrum.Niga.API.Domain.Services
{
    /// <summary>
    /// SEC-07.02 — SMS provider adapter. DI uses <see cref="ConfigurableSmsSender"/> (Stub | Msg91 | Twilio).
    /// </summary>
    public interface ISmsSender
    {
        Task<SmsSendResult> SendAsync(string destination, string message, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of an SMS send. On Twilio Trial, <see cref="EmbeddedOtp"/> is the code Twilio put in sms_2fa.
    /// </summary>
    public sealed class SmsSendResult
    {
        public bool Accepted { get; init; }
        /// <summary>OTP embedded by Twilio Trial sms_2fa (store this for verify).</summary>
        public string? EmbeddedOtp { get; init; }
        public string? MessageSid { get; init; }
        public bool UsedTrialTemplate { get; init; }
        public string? ProviderLabel { get; init; }

        public static SmsSendResult Ok(
            string? embeddedOtp = null,
            string? messageSid = null,
            bool usedTrialTemplate = false,
            string? providerLabel = null) =>
            new()
            {
                Accepted = true,
                EmbeddedOtp = embeddedOtp,
                MessageSid = messageSid,
                UsedTrialTemplate = usedTrialTemplate,
                ProviderLabel = providerLabel
            };

        public static SmsSendResult Fail() => new() { Accepted = false };

        public static implicit operator bool(SmsSendResult result) => result.Accepted;
    }
}
