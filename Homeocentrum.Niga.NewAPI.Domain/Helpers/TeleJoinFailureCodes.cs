namespace Homeocentrum.Niga.NewAPI.Domain.Helpers
{
    /// <summary>
    /// TEL-09.01 — canonical join-failure codes for TeleSessionEvent + JoinFailure API.
    /// Clients may send aliases; the API stores only these codes.
    /// </summary>
    public static class TeleJoinFailureCodes
    {
        public const string DeviceDenied = "DEVICE_DENIED";
        public const string NetworkError = "NETWORK_ERROR";
        public const string TokenFailed = "TOKEN_FAILED";
        public const string VendorError = "VENDOR_ERROR";
        public const string BrowserUnsupported = "BROWSER_UNSUPPORTED";
        public const string JoinFailed = "JOIN_FAILED";

        public static readonly string[] All =
        {
            DeviceDenied,
            NetworkError,
            TokenFailed,
            VendorError,
            BrowserUnsupported,
            JoinFailed
        };

        /// <summary>
        /// Map client code (or alias) to a canonical code. Empty / unknown → JOIN_FAILED.
        /// </summary>
        public static string Normalize(string? code, out string? rawInput)
        {
            rawInput = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
            if (rawInput == null)
                return JoinFailed;

            var key = rawInput
                .Replace('-', '_')
                .Replace(' ', '_')
                .ToUpperInvariant();

            return key switch
            {
                DeviceDenied or "CAMERA_DENIED" or "MIC_DENIED" or "PERMISSION_DENIED"
                    or "MEDIA_DENIED" or "AV_DENIED" => DeviceDenied,
                NetworkError or "NETWORK" or "CONNECTION_FAILED" or "CONNECTION"
                    or "OFFLINE" => NetworkError,
                TokenFailed or "TOKEN" or "TOKEN_EXPIRED" or "TOKEN_INVALID"
                    or "TOKEN_DENIED" => TokenFailed,
                VendorError or "SDK_ERROR" or "AGORA_ERROR" or "TWILIO_ERROR"
                    or "DAILY_ERROR" or "VENDOR" => VendorError,
                BrowserUnsupported or "UNSUPPORTED" or "WEBRTC_UNSUPPORTED" => BrowserUnsupported,
                JoinFailed or "JOIN_ERROR" or "FAILED" or "UNKNOWN" => JoinFailed,
                _ => JoinFailed
            };
        }

        public static bool SuggestRetry(string canonicalCode)
            => !string.Equals(canonicalCode, BrowserUnsupported, StringComparison.OrdinalIgnoreCase);

        public static string Message(string canonicalCode) => canonicalCode switch
        {
            DeviceDenied => "Camera or microphone was denied. Allow access and retry.",
            NetworkError => "Network problem joining the call. Check connection and retry.",
            TokenFailed => "Join token failed. Retry or rejoin if the session is still active.",
            VendorError => "Video provider error. Retry, or contact support if it keeps failing.",
            BrowserUnsupported => "This browser cannot join the video call. Try another browser or the app.",
            _ => "Could not join the call. Retry, rejoin if still active, or contact support."
        };
    }
}
