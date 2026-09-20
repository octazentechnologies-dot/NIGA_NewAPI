using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.DTOs;
using Niga_Domain.Extensions;
using Niga_Domain.Master;
using Niga_Domain.Security;

namespace Niga_Domain.API.Controllers
{
    /// <summary>DOC-10.02 — doctor profile GET/PUT /api/Profile/Me + photo. No role escalation.</summary>
    [Route("api/Profile")]
    [ApiController]
    [Authorize]
    public class DoctorProfileController : ControllerBase
    {
        private readonly NIGACentrumContext _context;
        private readonly IWebHostEnvironment _env;

        public DoctorProfileController(NIGACentrumContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("Me")]
        public async Task<IActionResult> Me()
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            // Reception may read the doctor's public-facing clinic name but not bank KYC — still doctor-owned.
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found for this user." });

            var dto = await MapAsync(doctor);
            if (deny != null)
                dto.Kyc = null;
            return Ok(new { success = true, data = dto });
        }

        [HttpPut("Me")]
        public async Task<IActionResult> Update([FromBody] DoctorProfileUpdateRequest request)
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;

            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });

            if (request == null)
                return BadRequest(new { success = false, message = "Body required." });

            if (!string.IsNullOrWhiteSpace(request.FirstName)) doctor.FirstName = request.FirstName.Trim();
            if (request.MiddleName != null) doctor.MiddleName = string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim();
            if (!string.IsNullOrWhiteSpace(request.LastName)) doctor.LastName = request.LastName.Trim();
            if (request.ClinicName != null) doctor.ClinicName = request.ClinicName.Trim();
            if (request.EmailId != null) doctor.EmailId = request.EmailId.Trim();
            if (request.MobileNo != null) doctor.MobileNo = request.MobileNo.Trim();
            if (request.City != null) doctor.City = request.City.Trim();
            if (request.QualificationId.HasValue) doctor.QualificationId = request.QualificationId;
            if (request.PassingUniversity != null) doctor.PassingUniversity = request.PassingUniversity.Trim();
            if (request.PassingCertNo != null) doctor.PassingCertNo = request.PassingCertNo.Trim();
            if (request.ConsultFeeInClinic.HasValue) doctor.ConsultFeeInClinic = request.ConsultFeeInClinic;
            if (request.ConsultFeeTele.HasValue) doctor.ConsultFeeTele = request.ConsultFeeTele;
            if (request.WorkingHoursNote != null) doctor.WorkingHoursNote = request.WorkingHoursNote.Trim();
            doctor.ChangedDate = DateTime.UtcNow;

            if (request.Kyc != null)
            {
                var kyc = await _context.DoctorPayeeKycs.FirstOrDefaultAsync(k => k.DoctorId == doctor.DoctorId && !k.DeleteStatus);
                if (kyc == null)
                {
                    kyc = new DoctorPayeeKyc
                    {
                        DoctorId = doctor.DoctorId,
                        CreatedAt = DateTime.UtcNow,
                        DeleteStatus = false
                    };
                    _context.DoctorPayeeKycs.Add(kyc);
                }
                kyc.AccountHolder = request.Kyc.AccountHolder;
                kyc.BankName = request.Kyc.BankName;
                kyc.AccountNumber = request.Kyc.AccountNumber;
                kyc.Ifsc = request.Kyc.Ifsc;
                kyc.Pan = request.Kyc.Pan;
                kyc.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = await MapAsync(doctor) });
        }

        [HttpPost("Me/Photo")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Photo(IFormFile file)
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Photo file is required." });

            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });

            var folder = Path.Combine(_env.ContentRootPath, "Data", "DoctorPhotos");
            Directory.CreateDirectory(folder);
            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
                ext = ".jpg";
            var name = $"doctor_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            doctor.PhotoPath = Path.Combine("DoctorPhotos", name).Replace("\\", "/");
            doctor.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = new { doctor.PhotoPath } });
        }

        [HttpGet("Me/Credentials")]
        public async Task<IActionResult> Credentials()
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found for this user." });

            var verification = await EnsurePendingVerificationAsync(doctor.DoctorId);
            var docs = await _context.DoctorCredentialDocuments.AsNoTracking()
                .Where(d => d.DoctorId == doctor.DoctorId && !d.DeleteStatus)
                .OrderByDescending(d => d.EnteredDate)
                .Select(d => new DoctorCredentialDocumentDto
                {
                    DoctorCredentialDocumentId = d.DoctorCredentialDocumentId,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    EnteredDate = d.EnteredDate
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = new DoctorCredentialsMeDto
                {
                    DoctorId = doctor.DoctorId,
                    DoctorVerificationId = verification.DoctorVerificationId,
                    Status = verification.Status,
                    Documents = docs
                }
            });
        }

        [HttpPost("Me/CredentialDocuments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCredentialDocument(IFormFile file, [FromForm] string documentType = "Other")
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            if (file == null || file.Length == 0)
                return BadRequest(new { success = false, message = "Document file is required." });

            var type = (documentType ?? "Other").Trim();
            if (type.Length == 0)
                type = "Other";
            if (!type.Equals("Qualification", StringComparison.OrdinalIgnoreCase)
                && !type.Equals("Registration", StringComparison.OrdinalIgnoreCase)
                && !type.Equals("Other", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "documentType must be Qualification, Registration, or Other." });
            }

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? string.Empty;
            if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png"))
                return BadRequest(new { success = false, message = "Only PDF, JPG, or PNG files are accepted." });

            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });

            var verification = await EnsurePendingVerificationAsync(doctor.DoctorId);
            var folder = Path.Combine(_env.ContentRootPath, "Data", "DoctorCredentials");
            Directory.CreateDirectory(folder);
            var name = $"doc_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            var canonical = type.Equals("Qualification", StringComparison.OrdinalIgnoreCase)
                ? "Qualification"
                : type.Equals("Registration", StringComparison.OrdinalIgnoreCase)
                    ? "Registration"
                    : "Other";

            var row = new DoctorCredentialDocument
            {
                DoctorId = doctor.DoctorId,
                DoctorVerificationId = verification.DoctorVerificationId,
                DocumentType = canonical,
                FileName = Path.GetFileName(file.FileName),
                FilePath = Path.Combine("DoctorCredentials", name).Replace("\\", "/"),
                ContentType = file.ContentType,
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            };

            _context.DoctorCredentialDocuments.Add(row);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                data = new DoctorCredentialDocumentDto
                {
                    DoctorCredentialDocumentId = row.DoctorCredentialDocumentId,
                    DocumentType = row.DocumentType,
                    FileName = row.FileName,
                    FilePath = row.FilePath,
                    EnteredDate = row.EnteredDate
                }
            });
        }

        private async Task<DoctorVerification> EnsurePendingVerificationAsync(int doctorId)
        {
            var row = await _context.DoctorVerifications
                .FirstOrDefaultAsync(v => v.DoctorId == doctorId && !v.DeleteStatus);
            if (row != null)
                return row;

            row = new DoctorVerification
            {
                DoctorId = doctorId,
                Status = "Pending",
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            };
            _context.DoctorVerifications.Add(row);
            await _context.SaveChangesAsync();
            return row;
        }

        private async Task<Doctor?> ResolveDoctorAsync()
        {
            var jwtDoctor = DoctorOwnership.GetDoctorId(User);
            if (jwtDoctor.HasValue)
            {
                return await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == jwtDoctor.Value && !d.DeleteStatus);
            }

            var userId = User.GetUserId();
            return await _context.Doctors.FirstOrDefaultAsync(d => d.UserId == userId && !d.DeleteStatus);
        }

        private async Task<DoctorProfileMeDto> MapAsync(Doctor doctor)
        {
            var qual = doctor.QualificationId == null
                ? null
                : await _context.QualificationMasters.AsNoTracking()
                    .Where(q => q.QualificationId == doctor.QualificationId)
                    .Select(q => q.QualificationName)
                    .FirstOrDefaultAsync();
            var kyc = await _context.DoctorPayeeKycs.AsNoTracking()
                .FirstOrDefaultAsync(k => k.DoctorId == doctor.DoctorId && !k.DeleteStatus);
            return new DoctorProfileMeDto
            {
                DoctorId = doctor.DoctorId,
                UserId = doctor.UserId ?? 0,
                FirstName = doctor.FirstName,
                MiddleName = doctor.MiddleName,
                LastName = doctor.LastName,
                ClinicName = doctor.ClinicName,
                EmailId = doctor.EmailId,
                MobileNo = doctor.MobileNo,
                City = doctor.City,
                QualificationId = doctor.QualificationId,
                QualificationName = qual,
                PassingUniversity = doctor.PassingUniversity,
                PassingCertNo = doctor.PassingCertNo,
                ConsultFeeInClinic = doctor.ConsultFeeInClinic,
                ConsultFeeTele = doctor.ConsultFeeTele,
                PhotoPath = doctor.PhotoPath,
                WorkingHoursNote = doctor.WorkingHoursNote,
                IsOnline = doctor.IsOnline,
                VerificationStatus = doctor.VerificationStatus,
                DirectoryVisible = doctor.DirectoryVisible,
                Kyc = kyc == null ? null : new DoctorPayeeKycDto
                {
                    AccountHolder = kyc.AccountHolder,
                    BankName = kyc.BankName,
                    AccountNumber = kyc.AccountNumber,
                    Ifsc = kyc.Ifsc,
                    Pan = kyc.Pan
                }
            };
        }
    }
}
