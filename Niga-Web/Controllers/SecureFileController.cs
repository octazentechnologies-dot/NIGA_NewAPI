using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Niga_Domain.Authorization;
using Niga_Domain.DTOs;
using Niga_Domain.Services;

namespace Niga_Domain.API.Controllers
{
    /// <summary>SEC-05.02 — Authorised and signed download for Data/attachments and Data/Blogs.</summary>
    [Route("api/[controller]")]
    [ApiController]
    public class SecureFileController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ISignedFileUrlService _signer;

        public SecureFileController(IWebHostEnvironment env, ISignedFileUrlService signer)
        {
            _env = env;
            _signer = signer;
        }

        [HttpPost("Sign")]
        [Authorize]
        public IActionResult Sign([FromBody] SecureFileSignRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Root) || string.IsNullOrWhiteSpace(request.Path))
                return BadRequest(new { success = false, message = "Root and Path are required." });

            if (!_signer.TryResolvePhysicalPath(_env.ContentRootPath, request.Root, request.Path, out _, out var error))
                return BadRequest(new { success = false, message = error });

            var ttl = request.TtlMinutes <= 0 ? 15 : Math.Clamp(request.TtlMinutes, 1, 120);
            var expires = DateTime.UtcNow.AddMinutes(ttl);
            var exp = new DateTimeOffset(expires).ToUnixTimeSeconds();
            var sig = _signer.Sign(request.Root, request.Path, expires);
            var url = $"/api/SecureFile/Download?root={Uri.EscapeDataString(request.Root)}&path={Uri.EscapeDataString(request.Path.TrimStart('/'))}&exp={exp}&sig={sig}";
            return Ok(new { success = true, data = new { url, expiresAt = expires } });
        }

        /// <summary>JWT download (no query signature).</summary>
        [HttpGet("{root}/{*path}")]
        [Authorize]
        public IActionResult GetAuthorised(string root, string path)
        {
            if (!_signer.TryResolvePhysicalPath(_env.ContentRootPath, root, path, out var fullPath, out var error))
                return BadRequest(new { success = false, message = error });
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { success = false, message = "File not found." });
            return PhysicalFile(fullPath, GuessMime(fullPath), Path.GetFileName(fullPath));
        }

        /// <summary>Time-limited signed download (img tags / emails).</summary>
        [HttpGet("Download")]
        [AllowAnonymous]
        public IActionResult DownloadSigned([FromQuery] string root, [FromQuery] string path, [FromQuery] long exp, [FromQuery] string sig)
        {
            if (!_signer.TryValidate(root, path, exp, sig, out var error))
                return Unauthorized(new { success = false, message = error });
            if (!_signer.TryResolvePhysicalPath(_env.ContentRootPath, root, path, out var fullPath, out var pathError))
                return BadRequest(new { success = false, message = pathError });
            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { success = false, message = "File not found." });
            return PhysicalFile(fullPath, GuessMime(fullPath), Path.GetFileName(fullPath));
        }

        private static string GuessMime(string fullPath)
        {
            return Path.GetExtension(fullPath).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".mp4" => "video/mp4",
                _ => "application/octet-stream"
            };
        }
    }
}
