using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Authorization;
using Niga_Domain.Data;
using Niga_Domain.Extensions;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>SEC-09.02 — Multipart upload + authorised GET (no public folder listing).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SecureDocumentController : ControllerBase
    {
        private readonly NIGACentrumContext _context;
        private readonly IWebHostEnvironment _env;

        public SecureDocumentController(NIGACentrumContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpPost("Upload")]
        [RequestSizeLimit(20_000_000)]
        public async Task<IActionResult> Upload(
            [FromForm] IFormFile file,
            [FromForm] string ownerType,
            [FromForm] long ownerId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File is required." });
            if (string.IsNullOrWhiteSpace(ownerType) || ownerId <= 0)
                return BadRequest(new { success = false, message = "ownerType and ownerId are required." });

            var root = Path.Combine(_env.ContentRootPath, "Data", "SecureDocuments");
            Directory.CreateDirectory(root);

            var safeName = Path.GetFileName(file.FileName);
            var storedName = $"{Guid.NewGuid():N}_{safeName}";
            var relativePath = Path.Combine("Data", "SecureDocuments", storedName).Replace('\\', '/');
            var fullPath = Path.Combine(root, storedName);

            string hashHex;
            await using (var fs = System.IO.File.Create(fullPath))
            {
                using var hashStream = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                int read;
                await using var upload = file.OpenReadStream();
                while ((read = await upload.ReadAsync(buffer)) > 0)
                {
                    hashStream.AppendData(buffer.AsSpan(0, read));
                    await fs.WriteAsync(buffer.AsMemory(0, read));
                }
                hashHex = Convert.ToHexString(hashStream.GetHashAndReset());
            }

            var doc = new SecureDocument
            {
                OwnerType = ownerType.Trim(),
                OwnerId = ownerId,
                BlobPath = relativePath,
                FileName = safeName,
                Mime = file.ContentType,
                Hash = hashHex,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTime.UtcNow
            };

            _context.SecureDocuments.Add(doc);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    doc.SecureDocumentId,
                    doc.OwnerType,
                    doc.OwnerId,
                    doc.FileName,
                    doc.Mime,
                    doc.Hash,
                    doc.CreatedAt
                }
            });
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id)
        {
            var doc = await _context.SecureDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.SecureDocumentId == id);
            if (doc == null)
                return NotFound(new { success = false, message = "Document not found." });

            var userId = User.GetUserId();
            var allowed = AdminAuthorizationPolicies.IsAdminPortalUser(User)
                || doc.CreatedBy == userId
                || (doc.OwnerType.Equals("User", StringComparison.OrdinalIgnoreCase) && doc.OwnerId == userId)
                || (doc.OwnerType.Equals("Doctor", StringComparison.OrdinalIgnoreCase)
                    && User.GetDoctorId() == (int)doc.OwnerId);

            if (!allowed)
                return Forbid();

            var fullPath = Path.IsPathRooted(doc.BlobPath)
                ? doc.BlobPath
                : Path.Combine(_env.ContentRootPath, doc.BlobPath.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { success = false, message = "Blob missing on server." });

            var mime = string.IsNullOrWhiteSpace(doc.Mime) ? "application/octet-stream" : doc.Mime;
            return PhysicalFile(fullPath, mime, doc.FileName ?? Path.GetFileName(fullPath));
        }
    }
}
