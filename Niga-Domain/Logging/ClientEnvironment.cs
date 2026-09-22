using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Niga_Domain.Logging
{
    /// <summary>
    /// Who / when / where / device fields for ErrorAlert emails and Logs/.
    /// Parses User-Agent without extra packages.
    /// </summary>
    public static class ClientEnvironment
    {
        public static string Claim(ClaimsPrincipal? user, params string[] types)
        {
            if (user == null) return "";
            foreach (var type in types)
            {
                var value = user.FindFirst(type)?.Value;
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
            return "";
        }

        public static void ApplyUser(IDictionary<string, string> details, ClaimsPrincipal? user)
        {
            var userName = Claim(user, "unique_name", ClaimTypes.Name, "UserName");
            var displayName = Claim(user, "FullName", ClaimTypes.GivenName, "DisplayName");
            details["UserName"] = userName;
            details["DisplayName"] = string.IsNullOrWhiteSpace(displayName) ? userName : displayName;
            details["User"] = string.IsNullOrWhiteSpace(userName) ? (user?.Identity?.Name ?? "") : userName;
            details["UserId"] = Claim(user, ClaimTypes.NameIdentifier, "nameid", "UserId", "sub");
            details["Role"] = Claim(user, ClaimTypes.Role, "RoleName", "role");
            details["DoctorId"] = Claim(user, "DoctorID", "DoctorId", "doctorId");
            details["DoctorUserId"] = Claim(user, "DoctorUserID", "DoctorUserId");
            details["Authenticated"] = user?.Identity?.IsAuthenticated == true ? "yes" : "no";
        }

        public static void ApplyRequestPlace(IDictionary<string, string> details, HttpContext context)
        {
            details["Host"] = context.Request.Host.Value ?? "";
            details["Scheme"] = context.Request.Scheme ?? "";
            details["Referrer"] = context.Request.Headers["Referer"].ToString() ?? "";
            var forwarded = context.Request.Headers["X-Forwarded-For"].ToString();
            details["ForwardedFor"] = forwarded.Length > 200 ? forwarded.Substring(0, 200) : forwarded;
            details["RemoteIp"] = context.Connection.RemoteIpAddress?.ToString() ?? "";
            details["HasBearer"] = context.Request.Headers.ContainsKey("Authorization") ? "yes" : "no";
        }

        public static void ApplyUserAgent(IDictionary<string, string> details, string? ua)
        {
            ua ??= "";
            details["UserAgent"] = ua.Length > 400 ? ua.Substring(0, 400) : ua;
            var parsed = Parse(ua);
            if (!details.ContainsKey("Browser") || string.IsNullOrWhiteSpace(details["Browser"]))
                details["Browser"] = parsed.Browser;
            if (!details.ContainsKey("BrowserVersion") || string.IsNullOrWhiteSpace(details["BrowserVersion"]))
                details["BrowserVersion"] = parsed.BrowserVersion;
            if (!details.ContainsKey("Os") || string.IsNullOrWhiteSpace(details["Os"]))
                details["Os"] = parsed.Os;
            if (!details.ContainsKey("DeviceType") || string.IsNullOrWhiteSpace(details["DeviceType"]))
                details["DeviceType"] = parsed.DeviceType;
            if (!details.ContainsKey("DeviceName") || string.IsNullOrWhiteSpace(details["DeviceName"]))
                details["DeviceName"] = parsed.DeviceName;
        }

        public static ParsedUa Parse(string ua)
        {
            var result = new ParsedUa();
            if (string.IsNullOrWhiteSpace(ua))
                return result;

            if (TryMatch(ua, @"Edg(?:e|A|iOS)?/([\d.]+)", out var ver))
            {
                result.Browser = "Microsoft Edge";
                result.BrowserVersion = ver;
            }
            else if (TryMatch(ua, @"OPR/([\d.]+)", out ver) || TryMatch(ua, @"Opera/([\d.]+)", out ver))
            {
                result.Browser = "Opera";
                result.BrowserVersion = ver;
            }
            else if (TryMatch(ua, @"Firefox/([\d.]+)", out ver))
            {
                result.Browser = "Firefox";
                result.BrowserVersion = ver;
            }
            else if (TryMatch(ua, @"Chrome/([\d.]+)", out ver) && ua.IndexOf("Chromium", StringComparison.OrdinalIgnoreCase) < 0)
            {
                result.Browser = "Chrome";
                result.BrowserVersion = ver;
            }
            else if (ua.IndexOf("Safari", StringComparison.OrdinalIgnoreCase) >= 0 && TryMatch(ua, @"Version/([\d.]+)", out ver))
            {
                result.Browser = "Safari";
                result.BrowserVersion = ver;
            }
            else if (TryMatch(ua, @"MSIE ([\d.]+)", out ver) || TryMatch(ua, @"rv:([\d.]+).*Trident", out ver))
            {
                result.Browser = "Internet Explorer";
                result.BrowserVersion = ver;
            }

            if (ua.IndexOf("Windows NT 10", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "Windows 10/11";
            else if (ua.IndexOf("Windows NT 6.3", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "Windows 8.1";
            else if (ua.IndexOf("Windows NT 6.1", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "Windows 7";
            else if (ua.IndexOf("Mac OS X", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "macOS";
            else if (ua.IndexOf("Android", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "Android";
            else if (ua.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0 || ua.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "iOS";
            else if (ua.IndexOf("Linux", StringComparison.OrdinalIgnoreCase) >= 0)
                result.Os = "Linux";

            if (ua.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0 || ua.IndexOf("Tablet", StringComparison.OrdinalIgnoreCase) >= 0)
                result.DeviceType = "Tablet";
            else if (Regex.IsMatch(ua, "Mobi|Android.*Mobile|iPhone", RegexOptions.IgnoreCase))
                result.DeviceType = "Mobile";
            else
                result.DeviceType = "Desktop";

            if (ua.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) >= 0)
                result.DeviceName = "iPhone";
            else if (ua.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) >= 0)
                result.DeviceName = "iPad";
            else if (TryMatch(ua, @"Android [^;]*; ([^)]+) Build/", out var androidModel)
                     || TryMatch(ua, @"Android [^;]*; ([^;)]+)", out androidModel))
                result.DeviceName = androidModel.Trim();
            else if (result.DeviceType == "Desktop")
                result.DeviceName = string.IsNullOrEmpty(result.Os) ? "Desktop PC" : result.Os + " PC";
            else
                result.DeviceName = result.Os;

            return result;
        }

        private static bool TryMatch(string ua, string pattern, out string value)
        {
            var m = Regex.Match(ua, pattern, RegexOptions.IgnoreCase);
            value = m.Success && m.Groups.Count > 1 ? m.Groups[1].Value : "";
            return m.Success;
        }

        public sealed class ParsedUa
        {
            public string Browser { get; set; } = "Unknown";
            public string BrowserVersion { get; set; } = "";
            public string Os { get; set; } = "Unknown";
            public string DeviceType { get; set; } = "Unknown";
            public string DeviceName { get; set; } = "Unknown";
        }
    }
}
