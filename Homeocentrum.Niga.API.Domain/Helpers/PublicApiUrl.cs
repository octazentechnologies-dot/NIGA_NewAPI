using Microsoft.AspNetCore.Http;

namespace Homeocentrum.Niga.API.Domain.Helpers
{
    /// <summary>
    /// Absolute URLs for API paths, built from the request so they follow whichever machine, host and port
    /// the caller used to reach this API. When a proxy strips a path prefix (the UI site maps /new-api/* here),
    /// the prefix is recovered from X-Forwarded-Prefix or from the original URL IIS URL Rewrite forwards.
    /// </summary>
    public static class PublicApiUrl
    {
        public static string For(HttpRequest request, string apiPath)
        {
            var path = "/" + apiPath.TrimStart('/');
            var host = FirstValue(request.Headers["X-Forwarded-Host"]) ?? request.Host.Value ?? string.Empty;
            var prefix = NormalizePrefix(FirstValue(request.Headers["X-Forwarded-Prefix"]) ?? StrippedProxyPrefix(request));
            return $"{request.Scheme}://{host}{prefix}{request.PathBase}{path}";
        }

        /// <summary>"/new-api" when IIS received /new-api/api/... and forwarded /api/... to this API.</summary>
        private static string? StrippedProxyPrefix(HttpRequest request)
        {
            var original = FirstValue(request.Headers["X-Original-URL"]);
            if (original == null)
                return null;
            var originalPath = original.Split('?')[0];
            var currentPath = $"{request.PathBase}{request.Path}";
            if (currentPath.Length == 0 || originalPath.Length <= currentPath.Length
                || !originalPath.EndsWith(currentPath, StringComparison.OrdinalIgnoreCase))
                return null;
            return originalPath[..^currentPath.Length];
        }

        private static string? FirstValue(string? header)
        {
            var value = header?.Split(',')[0].Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static string NormalizePrefix(string? prefix)
        {
            var trimmed = prefix?.Trim().Trim('/');
            return string.IsNullOrEmpty(trimmed) ? string.Empty : "/" + trimmed;
        }
    }
}
