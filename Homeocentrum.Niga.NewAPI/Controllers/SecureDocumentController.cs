using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Helpers;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>SEC-09.02 — Multipart upload + authorised GET (no public folder listing).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [ForbidMoneyRoles]
    public class SecureDocumentController : ControllerBase
    {
        private readonly NIGACentrumContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IPatientAccessGuard _patientAccess;

        public SecureDocumentController(NIGACentrumContext context, IWebHostEnvironment env, IPatientAccessGuard patientAccess)
        {
            _context = context;
            _env = env;
            _patientAccess = patientAccess;
        }

        /// <summary>User owner = the caller; Doctor owner = caller's clinic; Patient owner = patient the caller may access.</summary>
        private async Task<bool> OwnerAllowedAsync(string ownerType, long ownerId)
        {
            if (DoctorOwnership.IsGlobalAdminPortalUser(User))
                return true;
            switch (ownerType.Trim().ToLowerInvariant())
            {
                case "user":
                    try { return User.GetUserId() == ownerId; }
                    catch { return false; }
                case "doctor":
                    return ownerId <= int.MaxValue && DoctorOwnership.EnsureDoctorOwns(User, (int)ownerId);
                case "patient":
                    return ownerId <= int.MaxValue && await _patientAccess.CanAccessPatientAsync(User, (int)ownerId);
                default:
                    return false;
            }
        }

        [HttpPost("Upload")]
        [RequestSizeLimit(20_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload(
            IFormFile file,
            [FromForm] string ownerType,
            [FromForm] long ownerId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "File is required." });
            if (string.IsNullOrWhiteSpace(ownerType) || ownerId <= 0)
                return BadRequest(new { success = false, message = "ownerType and ownerId are required." });
            if (!await OwnerAllowedAsync(ownerType, ownerId))
                return StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Access denied for this owner." });

            var root = UploadedMedia.Folder(_env.ContentRootPath, UploadedMedia.SecureDocuments);
            Directory.CreateDirectory(root);

            var safeName = Path.GetFileName(file.FileName);
            var storedName = Homeocentrum.Niga.NewAPI.Domain.Security.Uploads.UploadGuard.RandomStoredName(safeName);
            var relativePath = UploadedMedia.ContentRelative(UploadedMedia.SecureDocuments, storedName);
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
                    doc.CreatedAt,
                    path = doc.BlobPath,
                    downloadUrl = $"/api/SecureDocument/{doc.SecureDocumentId}"
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

            var fullPath = UploadedMedia.Resolve(_env.ContentRootPath, doc.BlobPath);

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { success = false, message = "Blob missing on server." });

            var mime = string.IsNullOrWhiteSpace(doc.Mime) ? "application/octet-stream" : doc.Mime;
            return PhysicalFile(fullPath, mime, doc.FileName ?? Path.GetFileName(fullPath));
        }
    }
}
