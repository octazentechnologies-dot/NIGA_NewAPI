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
            if (string.Equals(DoctorOwnership.GetRoleName(User), "Reception", StringComparison.OrdinalIgnoreCase))
                return await ReceptionMeAsync();

            var deny = DoctorOwnership.ForbidIfReception(User);
            // Reception may read the doctor's public-facing clinic name but not bank KYC — still doctor-owned.
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found for this user." });

            var dto = await MapAsync(doctor);
            if (deny != null)
            {
                dto.Kyc = null;
                dto.ConsultFeeInClinic = null;
                dto.ConsultFeeTele = null;
                dto.QualificationId = null;
                dto.QualificationName = null;
                dto.PassingUniversity = null;
                dto.PassingCertNo = null;
            }
            return Ok(new { success = true, data = dto });
        }

        [HttpPut("Me")]
        public async Task<IActionResult> Update([FromBody] DoctorProfileUpdateRequest request)
        {
            // REC-02.02 — reception may PUT own staff fields; clinic fee / bank / qualifications are ignored.
            if (string.Equals(DoctorOwnership.GetRoleName(User), "Reception", StringComparison.OrdinalIgnoreCase))
                return await ReceptionUpdateAsync(request);

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
            if (request.AddressLine1 != null || request.AddressLine2 != null || request.Pincode != null)
            {
                doctor.PermanantAddress = FormatClinicAddress(
                    request.AddressLine1 ?? ParseClinicAddress(doctor.PermanantAddress).Line1,
                    request.AddressLine2 ?? ParseClinicAddress(doctor.PermanantAddress).Line2,
                    request.Pincode ?? ParseClinicAddress(doctor.PermanantAddress).Pincode);
            }
            if (request.StateId.HasValue)
                doctor.StateId = request.StateId;
            else if (!string.IsNullOrWhiteSpace(request.State))
            {
                var stateName = request.State.Trim();
                var stateId = await _context.StateMasters.AsNoTracking()
                    .Where(s => s.StateName != null && s.StateName.ToLower() == stateName.ToLower())
                    .Select(s => (int?)s.StateId)
                    .FirstOrDefaultAsync();
                if (stateId.HasValue)
                    doctor.StateId = stateId;
            }
            if (request.QualificationId.HasValue) doctor.QualificationId = request.QualificationId;
            if (request.PassingUniversity != null) doctor.PassingUniversity = request.PassingUniversity.Trim();
            if (request.PassingCertNo != null) doctor.PassingCertNo = request.PassingCertNo.Trim();
            if (request.ConsultFeeInClinic.HasValue) doctor.ConsultFeeInClinic = request.ConsultFeeInClinic;
            if (request.ConsultFeeTele.HasValue) doctor.ConsultFeeTele = request.ConsultFeeTele;
            if (request.WorkingHoursNote != null) doctor.WorkingHoursNote = request.WorkingHoursNote.Trim();
            doctor.ChangedDate = DateTime.UtcNow;

            if (doctor.UserId.HasValue)
            {
                var user = await _context.UserMasters.FirstOrDefaultAsync(u => u.UserId == doctor.UserId.Value && !u.DeleteStatus);
                if (user != null)
                {
                    if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName.Trim();
                    if (!string.IsNullOrWhiteSpace(request.LastName)) user.LastName = request.LastName.Trim();
                    if (request.EmailId != null) user.EmailId = request.EmailId.Trim();
                    if (request.MobileNo != null) user.MobileNo = PhoneNormalizer.Digits(request.MobileNo);
                    if (doctor.StateId.HasValue) user.StateId = doctor.StateId;
                    user.ChangedDate = DateTime.UtcNow;
                }
            }

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

        private async Task<IActionResult> ReceptionMeAsync()
        {
            var staffId = User.GetUserId();
            var staff = await _context.DoctorReceptionStaffs.AsNoTracking()
                .FirstOrDefaultAsync(s => s.ReceptionStaffId == staffId && !s.DeleteStatus && s.IsActive);
            if (staff == null)
                return NotFound(new { success = false, message = "Reception profile not found for this user." });

            return Ok(new { success = true, data = MapReception(staff) });
        }

        /// <summary>
        /// REC-02.02 — update DoctorReceptionStaff contact fields only.
        /// ConsultFee*, Kyc, Qualification*, ClinicName, WorkingHoursNote are not applied.
        /// </summary>
        private async Task<IActionResult> ReceptionUpdateAsync(DoctorProfileUpdateRequest? request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Body required." });

            var staffId = User.GetUserId();
            var staff = await _context.DoctorReceptionStaffs
                .FirstOrDefaultAsync(s => s.ReceptionStaffId == staffId && !s.DeleteStatus && s.IsActive);
            if (staff == null)
                return NotFound(new { success = false, message = "Reception profile not found for this user." });

            var name = $"{request.FirstName} {request.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(name))
                staff.FullName = name;
            if (request.EmailId != null)
                staff.EmailId = string.IsNullOrWhiteSpace(request.EmailId) ? null : request.EmailId.Trim();
            if (request.MobileNo != null)
            {
                var mobile = request.MobileNo.Trim();
                if (string.IsNullOrWhiteSpace(mobile))
                    return BadRequest(new { success = false, message = "MobileNo cannot be empty." });
                staff.ContactNumber = mobile;
            }
            if (request.AddressLine1 != null)
                staff.Address = string.IsNullOrWhiteSpace(request.AddressLine1) ? null : request.AddressLine1.Trim();
            if (request.City != null)
                staff.City = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();
            if (request.State != null)
                staff.State = string.IsNullOrWhiteSpace(request.State) ? null : request.State.Trim();
            if (request.Country != null)
                staff.Country = string.IsNullOrWhiteSpace(request.Country) ? null : request.Country.Trim();

            staff.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, data = MapReception(staff) });
        }

        private static ReceptionProfileMeDto MapReception(DoctorReceptionStaff staff) => new()
        {
            Role = "Reception",
            ReceptionStaffId = staff.ReceptionStaffId,
            DoctorId = staff.DoctorId,
            LoginId = staff.UserId,
            FullName = staff.FullName,
            EmailId = staff.EmailId,
            MobileNo = staff.ContactNumber,
            Address = staff.Address,
            City = staff.City,
            State = staff.State,
            Country = staff.Country
        };

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
            var parsed = ParseClinicAddress(doctor.PermanantAddress);
            string? stateName = null;
            if (doctor.StateId.HasValue)
            {
                stateName = await _context.StateMasters.AsNoTracking()
                    .Where(s => s.StateId == doctor.StateId.Value)
                    .Select(s => s.StateName)
                    .FirstOrDefaultAsync();
            }
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
                AddressLine1 = parsed.Line1,
                AddressLine2 = parsed.Line2,
                State = stateName,
                StateId = doctor.StateId,
                Pincode = parsed.Pincode,
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

        private static (string Line1, string Line2, string Pincode) ParseClinicAddress(string? raw)
        {
            var text = (raw ?? string.Empty).Replace("\r\n", "\n").Trim();
            if (text.Length == 0)
                return ("", "", "");
            var lines = text.Split('\n', StringSplitOptions.None).Select(x => x.Trim()).ToList();
            var pin = "";
            var pinLine = lines.LastOrDefault(l => l.StartsWith("PIN:", StringComparison.OrdinalIgnoreCase));
            if (pinLine != null)
            {
                pin = pinLine[4..].Trim();
                lines.Remove(pinLine);
            }
            var line1 = lines.Count > 0 ? lines[0] : "";
            var line2 = lines.Count > 1 ? string.Join(" ", lines.Skip(1).Where(l => l.Length > 0)) : "";
            return (line1, line2, pin);
        }

        private static string FormatClinicAddress(string? line1, string? line2, string? pincode)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(line1))
                parts.Add(line1.Trim());
            if (!string.IsNullOrWhiteSpace(line2))
                parts.Add(line2.Trim());
            if (!string.IsNullOrWhiteSpace(pincode))
                parts.Add("PIN:" + pincode.Trim());
            return string.Join("\n", parts);
        }
    }
}
