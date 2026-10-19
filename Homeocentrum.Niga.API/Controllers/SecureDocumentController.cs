using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Authorization;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;

namespace Homeocentrum.Niga.API.Controllers
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
        private readonly IStoredFileStore _files;

        public SecureDocumentController(NIGACentrumContext context, IWebHostEnvironment env, IPatientAccessGuard patientAccess, IStoredFileStore files)
        {
            _context = context;
            _env = env;
            _patientAccess = patientAccess;
            _files = files;
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

            var safeName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(safeName);
            var folder = ownerType.Trim().ToLowerInvariant() switch
            {
                "patient" => $"patients/{ownerId}/documents",
                "doctor" => $"doctors/{ownerId}/documents",
                "user" => $"users/{ownerId}/documents",
                _ => $"documents/{ownerId}"
            };

            byte[] bytes;
            await using (var upload = file.OpenReadStream())
            {
                using var memory = new MemoryStream();
                await upload.CopyToAsync(memory);
                bytes = memory.ToArray();
            }

            string relativePath;
            try
            {
                relativePath = await _files.SaveAsync(folder, string.IsNullOrWhiteSpace(extension) ? ".bin" : extension, bytes);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
            }

            var hashHex = Convert.ToHexString(SHA256.HashData(bytes));

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

            if (!allowed
                && doc.OwnerType.Equals("Patient", StringComparison.OrdinalIgnoreCase)
                && doc.OwnerId <= int.MaxValue)
                allowed = await _patientAccess.CanAccessPatientAsync(User, (int)doc.OwnerId);

            if (!allowed)
                return Forbid();

            byte[]? bytes;
            try
            {
                bytes = await _files.ReadAsync(_env.ContentRootPath, doc.BlobPath);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
            }
            if (bytes == null || bytes.Length == 0)
                return NotFound(new { success = false, message = "Blob missing on server." });

            var mime = string.IsNullOrWhiteSpace(doc.Mime) ? "application/octet-stream" : doc.Mime;
            return File(bytes, mime, doc.FileName ?? "document");
        }
    }
}
