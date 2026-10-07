using System.Text.RegularExpressions;

namespace Homeocentrum.Niga.NewAPI.Domain.Logging
{
    /// <summary>
    /// Masks personal data and secrets before anything is written to log files or alert emails:
    /// JWTs, bearer tokens, OTPs, passwords, signatures, mobile numbers and email addresses.
    /// </summary>
    public static class LogRedactor
    {
        private const RegexOptions Opts = RegexOptions.Compiled | RegexOptions.CultureInvariant;
        private static readonly TimeSpan Timeout = TimeSpan.FromMilliseconds(250);

        private static readonly Regex Jwt = new(@"eyJ[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]{5,}\.[A-Za-z0-9_\-]*", Opts, Timeout);
        private static readonly Regex Bearer = new(@"(?i)\bBearer\s+[A-Za-z0-9_\-\.=+/]+", Opts, Timeout);
        private static readonly Regex SecretPair = new(
            @"(?i)(?<k>\b(?:otp|otpcode|otp_code|password|newpassword|oldpassword|confirmpassword|pwd|pin|token|accesstoken|access_token|refreshtoken|refresh_token|sig|signature|secret|keysecret|apikey|api_key|authkey|cvv|aadhaar|aadhar)\b)(?<sep>""?\s*[=:]\s*)(?<q>""?)(?<v>[^&\s"",;}]+)",
            Opts, Timeout);
        private static readonly Regex OtpPath = new(@"(?i)(?<p>/otp[a-z]*/(?:[^/?\s]+/)*?)(?<v>\d{4,8})(?=[/?\s]|$)", Opts, Timeout);
        private static readonly Regex OtpPhrase = new(@"(?i)(?<p>\b(?:otp|one[\s\-]time\s+password|verification\s+code|security\s+code)\b[^0-9\r\n]{0,24})(?<v>\d{4,8})(?!\d)", Opts, Timeout);
        private static readonly Regex Mobile = new(@"(?<![\d\w])(?:\+?91[\-\s]?)?[6-9]\d{9}(?![\d\w])", Opts, Timeout);
        private static readonly Regex Email = new(@"(?<u>[A-Za-z0-9._%+\-])[A-Za-z0-9._%+\-]*@(?<d>[A-Za-z0-9.\-]+\.[A-Za-z]{2,})", Opts, Timeout);

        public static string Redact(string? text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? "";
            try
            {
                var value = Jwt.Replace(text, "[jwt]");
                value = Bearer.Replace(value, "Bearer [redacted]");
                value = SecretPair.Replace(value, m => m.Groups["k"].Value + m.Groups["sep"].Value + m.Groups["q"].Value + "[redacted]");
                value = OtpPath.Replace(value, m => m.Groups["p"].Value + "[otp]");
                value = OtpPhrase.Replace(value, m => m.Groups["p"].Value + "[otp]");
                value = Mobile.Replace(value, m => MaskDigits(m.Value));
                value = Email.Replace(value, m => m.Groups["u"].Value + "***@" + m.Groups["d"].Value);
                return value;
            }
            catch (RegexMatchTimeoutException)
            {
                return "[redacted: log text could not be scanned]";
            }
        }

        public static IDictionary<string, string>? RedactDetails(IDictionary<string, string>? details)
        {
            if (details == null || details.Count == 0) return details;
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in details)
            {
                if (string.IsNullOrWhiteSpace(kv.Key)) continue;
                copy[kv.Key] = IsSecretKey(kv.Key) ? "[redacted]" : Redact(kv.Value);
            }
            return copy;
        }

        public static string MaskMobile(string? mobile)
            => string.IsNullOrWhiteSpace(mobile) ? "" : MaskDigits(mobile);

        private static bool IsSecretKey(string key)
        {
            var k = key.Replace("-", "").Replace("_", "");
            return k.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                || k.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
                || k.Equals("SetCookie", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Password", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Token", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Otp", StringComparison.OrdinalIgnoreCase)
                || k.Contains("Secret", StringComparison.OrdinalIgnoreCase)
                || k.Equals("RequestBody", StringComparison.OrdinalIgnoreCase)
                || k.Equals("Body", StringComparison.OrdinalIgnoreCase);
        }

        private static string MaskDigits(string value)
        {
            var digits = new string(value.Where(char.IsDigit).ToArray());
            if (digits.Length <= 4) return "****";
            return new string('*', digits.Length - 2) + digits[^2..];
        }
    }
}
