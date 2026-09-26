using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Homeocentrum.Niga.NewAPI.Domain.Logging
{
    /// <summary>
    /// Swagger is not public. /swagger asks for the SwaggerAuth user in appsettings first.
    /// </summary>
    public sealed class SwaggerGateMiddleware
    {
        public const string CookieName = "HomeocentrumSwagger";
        private readonly RequestDelegate _next;
        private readonly string _username;
        private readonly string _password;
        private readonly string _apiName;

        public SwaggerGateMiddleware(RequestDelegate next, IConfiguration configuration, string apiName)
        {
            _next = next;
            _username = configuration["SwaggerAuth:Username"] ?? "";
            _password = configuration["SwaggerAuth:Password"] ?? "";
            _apiName = apiName;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (path.Equals("/swagger-login", StringComparison.OrdinalIgnoreCase))
            {
                await ServeLoginAsync(context);
                return;
            }

            if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
            {
                if (!IsSignedIn(context))
                {
                    context.Response.Redirect("/swagger-login");
                    return;
                }
            }

            await _next(context);
        }

        private async Task ServeLoginAsync(HttpContext context)
        {
            if (IsSignedIn(context) && !HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.Redirect("/swagger");
                return;
            }

            var error = "";
            if (HttpMethods.IsPost(context.Request.Method))
            {
                var form = await context.Request.ReadFormAsync();
                var user = form["username"].ToString();
                var pass = form["password"].ToString();
                if (FixedEquals(user, _username) && FixedEquals(pass, _password) && _username.Length > 0)
                {
                    context.Response.Cookies.Append(CookieName, Sign(user), new CookieOptions
                    {
                        HttpOnly = true,
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        MaxAge = TimeSpan.FromHours(8),
                        Path = "/"
                    });
                    context.Response.Redirect("/swagger");
                    return;
                }
                error = "Those sign-in details are not correct.";
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(Page(_apiName, error));
        }

        private bool IsSignedIn(HttpContext context)
        {
            if (!context.Request.Cookies.TryGetValue(CookieName, out var token))
                return false;
            var parts = (token ?? "").Split('|');
            if (parts.Length != 3) return false;
            if (!long.TryParse(parts[1], out var exp)) return false;
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return false;
            if (!FixedEquals(parts[0], _username)) return false;
            var expected = Hash(parts[0] + "|" + parts[1]);
            return FixedEquals(parts[2], expected);
        }

        private string Sign(string user)
        {
            var exp = DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds();
            var payload = user + "|" + exp;
            return payload + "|" + Hash(payload);
        }

        private string Hash(string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_password));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        private static bool FixedEquals(string left, string right)
        {
            var a = Encoding.UTF8.GetBytes(left ?? "");
            var b = Encoding.UTF8.GetBytes(right ?? "");
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        public static string Page(string apiName, string error)
        {
            var message = string.IsNullOrEmpty(error)
                ? ""
                : "<p class=\"err\">" + System.Net.WebUtility.HtmlEncode(error) + "</p>";
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>" + System.Net.WebUtility.HtmlEncode(apiName) + @"</title>
<link rel=""icon"" type=""image/png"" href=""/favicon.png"" />
<link rel=""shortcut icon"" href=""/favicon.ico"" />
<style>
body{margin:0;min-height:100vh;display:flex;align-items:center;justify-content:center;background:#e8eef5;font-family:Segoe UI,Arial,sans-serif;color:#0f172a}
.card{width:420px;max-width:92vw;background:#fff;border-radius:12px;box-shadow:0 12px 40px rgba(15,23,42,.12);padding:28px 28px 24px}
h1{margin:0;font-size:22px}
.sub{margin:6px 0 18px;color:#475569;font-size:14px}
label{display:block;font-size:13px;font-weight:600;margin:12px 0 6px}
input{width:100%;box-sizing:border-box;border:1px solid #cbd5e1;border-radius:8px;padding:10px 12px;font-size:15px}
.url{background:#f8fafc;border:1px dashed #94a3b8;border-radius:8px;padding:10px 12px;font-size:13px;white-space:nowrap;overflow-x:auto;color:#0f172a}
button{margin-top:18px;width:100%;background:#000;color:#fff;border:0;border-radius:8px;padding:11px;font-size:15px;cursor:pointer}
.err{background:#fef2f2;color:#991b1b;border-radius:8px;padding:8px 10px;font-size:13px}
</style>
</head>
<body>
<form class=""card"" method=""post"" action=""/swagger-login"">
<h1>Homeocentrum</h1>
<p class=""sub"">" + System.Net.WebUtility.HtmlEncode(apiName) + @" documentation. Sign in to open Swagger.</p>
" + message + @"
<label>Swagger URL</label>
<div class=""url"" id=""docUrl"">Loading…</div>
<label for=""username"">Username</label>
<input id=""username"" name=""username"" autocomplete=""username"" required>
<label for=""password"">Password</label>
<input id=""password"" name=""password"" type=""password"" autocomplete=""current-password"" required>
<button type=""submit"">Sign in</button>
</form>
<script>
document.getElementById('docUrl').textContent = window.location.origin + '/swagger';
</script>
</body>
</html>";
        }
    }
}
