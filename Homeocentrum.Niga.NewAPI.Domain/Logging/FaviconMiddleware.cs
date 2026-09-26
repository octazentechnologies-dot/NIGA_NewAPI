using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Homeocentrum.Niga.NewAPI.Domain.Logging
{
    /// <summary>
    /// Same Homeocentrum favicon as the SPA — covers /favicon.* and Swagger UI's own favicon URLs.
    /// </summary>
    public sealed class FaviconMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IFileProvider _webRoot;

        public FaviconMiddleware(RequestDelegate next, IWebHostEnvironment env)
        {
            _next = next;
            _webRoot = env.WebRootFileProvider;
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";
            if (HttpMethods.IsGet(context.Request.Method) && IsFaviconPath(path))
            {
                var bytes = ReadWebRoot("favicon.png")
                    ?? ReadWebRoot("favicon.ico");
                if (bytes is { Length: > 0 })
                {
                    context.Response.StatusCode = StatusCodes.Status200OK;
                    // Always serve the SPA dashboard icon (red diamond PNG).
                    context.Response.ContentType = "image/png";
                    context.Response.Headers.CacheControl = "public,max-age=86400";
                    await context.Response.Body.WriteAsync(bytes);
                    return;
                }
            }

            await _next(context);
        }

        private byte[]? ReadWebRoot(string name)
        {
            var file = _webRoot.GetFileInfo(name);
            if (!file.Exists || file.Length <= 0) return null;
            using var stream = file.CreateReadStream();
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return ms.ToArray();
        }

        private static bool IsFaviconPath(string path)
        {
            return path.Equals("/favicon.ico", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/favicon.png", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/swagger/favicon-32x32.png", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/swagger/favicon-16x16.png", StringComparison.OrdinalIgnoreCase)
                || path.Equals("/swagger/favicon.ico", StringComparison.OrdinalIgnoreCase);
        }
    }

    public static class FaviconMiddlewareExtensions
    {
        public static IApplicationBuilder UseHomeocentrumFavicon(this IApplicationBuilder app)
            => app.UseMiddleware<FaviconMiddleware>();
    }
}
