using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers
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

        /// <summary>The file name changes on every upload, so ?v= makes browsers fetch a replaced photo.</summary>
        private string? PhotoUrlFor(Doctor doctor)
        {
            if (string.IsNullOrWhiteSpace(doctor.PhotoPath))
                return null;
            var version = Uri.EscapeDataString(Path.GetFileNameWithoutExtension(doctor.PhotoPath));
            return PublicApiUrl.For(Request, $"/api/Profile/Photo/{doctor.DoctorId}?v={version}");
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
                dto.FollowUpFeeInClinic = null;
                dto.FollowUpFeeTele = null;
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
            // REC-02.02 — reception may PUT own staff fields. Clinic fee, bank, and qualifications are refused.
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
            if (request.TeleDisabled == true)
            {
                doctor.ConsultFeeTele = null;
                doctor.FollowUpFeeTele = null;
                doctor.FreeFollowUpDaysTele = null;
            }
            if (request.FollowUpFeeInClinic.HasValue) doctor.FollowUpFeeInClinic = request.FollowUpFeeInClinic;
            if (request.FollowUpFeeTele.HasValue && request.TeleDisabled != true) doctor.FollowUpFeeTele = request.FollowUpFeeTele;
            if (request.FreeFollowUpDaysInClinic.HasValue) doctor.FreeFollowUpDaysInClinic = request.FreeFollowUpDaysInClinic;
            if (request.FreeFollowUpDaysTele.HasValue && request.TeleDisabled != true) doctor.FreeFollowUpDaysTele = request.FreeFollowUpDaysTele;
            if (!string.IsNullOrWhiteSpace(request.FeeCurrency))
            {
                var currency = request.FeeCurrency.Trim().ToUpperInvariant();
                if (currency.Length != 3)
                    return BadRequest(new { success = false, message = "FeeCurrency must be a 3-letter code." });
                doctor.FeeCurrency = currency;
            }
            if (request.GoogleMapsLink != null)
                doctor.GoogleMapsLink = string.IsNullOrWhiteSpace(request.GoogleMapsLink) ? null : request.GoogleMapsLink.Trim();
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
                if (request.Kyc.Pan != null) kyc.Pan = request.Kyc.Pan;
                if (request.Kyc.BranchName != null)
                    kyc.BranchName = string.IsNullOrWhiteSpace(request.Kyc.BranchName) ? null : request.Kyc.BranchName.Trim();
                if (request.Kyc.AccountType != null)
                    kyc.AccountType = string.IsNullOrWhiteSpace(request.Kyc.AccountType) ? null : request.Kyc.AccountType.Trim();
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
            if (!CredentialFileRules.TryGetPhotoType(file.FileName, out var ext, out _))
                return BadRequest(new { success = false, message = "Photo must be a jpg, jpeg, png or webp image." });
            if (file.Length > CredentialFileRules.MaxPhotoBytes)
                return BadRequest(new { success = false, message = "Photo must be 5 MB or smaller." });

            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });

            var folder = UploadedMedia.Folder(_env.ContentRootPath, UploadedMedia.DoctorPhotos);
            Directory.CreateDirectory(folder);
            var name = $"doctor_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            doctor.PhotoPath = UploadedMedia.MediaRelative(UploadedMedia.DoctorPhotos, name);
            doctor.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = new { doctor.PhotoPath, photoUrl = PhotoUrlFor(doctor) } });
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
                    Degree = d.Degree,
                    Specialization = d.Specialization,
                    Institution = d.Institution,
                    PassingYear = d.PassingYear,
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
        public async Task<IActionResult> UploadCredentialDocument(
            IFormFile file,
            [FromForm] string documentType = "Other",
            [FromForm] string? degree = null,
            [FromForm] string? specialization = null,
            [FromForm] string? institution = null,
            [FromForm] int? passingYear = null)
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            if (file == null || file.Length == 0)
                return Ok(new { success = true, saved = false, message = "No document was uploaded." });

            var reject = CredentialFileRules.RejectReason(file.FileName, file.Length);
            if (reject != null)
                return BadRequest(new { success = false, saved = false, message = reject });
            CredentialFileRules.TryGetExtension(file.FileName, out var ext);

            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });

            var verification = await EnsurePendingVerificationAsync(doctor.DoctorId);
            var folder = UploadedMedia.Folder(_env.ContentRootPath, UploadedMedia.DoctorCredentials);
            Directory.CreateDirectory(folder);
            var name = $"doc_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(folder, name);
            await using (var stream = System.IO.File.Create(path))
                await file.CopyToAsync(stream);

            var canonical = CredentialFileRules.CanonicalType(documentType);

            var row = new DoctorCredentialDocument
            {
                DoctorId = doctor.DoctorId,
                DoctorVerificationId = verification.DoctorVerificationId,
                DocumentType = canonical,
                FileName = Path.GetFileName(file.FileName),
                FilePath = UploadedMedia.MediaRelative(UploadedMedia.DoctorCredentials, name),
                ContentType = file.ContentType,
                Degree = TrimTo(degree, 50),
                Specialization = TrimTo(specialization, 150),
                Institution = TrimTo(institution, 200),
                PassingYear = passingYear is > 1900 and < 2200 ? passingYear : null,
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            };

            _context.DoctorCredentialDocuments.Add(row);
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = MapCredential(row) });
        }

        /// <summary>Edit qualification details on an uploaded document, optionally replacing the file.</summary>
        [HttpPut("Me/CredentialDocuments/{id:int}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateCredentialDocument(
            int id,
            IFormFile? file,
            [FromForm] string? degree = null,
            [FromForm] string? specialization = null,
            [FromForm] string? institution = null,
            [FromForm] int? passingYear = null)
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });
            var row = await _context.DoctorCredentialDocuments
                .FirstOrDefaultAsync(d => d.DoctorCredentialDocumentId == id && d.DoctorId == doctor.DoctorId && !d.DeleteStatus);
            if (row == null)
                return NotFound(new { success = false, message = "Document not found." });

            if (file != null && file.Length > 0)
            {
                var reject = CredentialFileRules.RejectReason(file.FileName, file.Length);
                if (reject != null)
                    return BadRequest(new { success = false, message = reject });
                CredentialFileRules.TryGetExtension(file.FileName, out var ext);
                var folder = UploadedMedia.Folder(_env.ContentRootPath, UploadedMedia.DoctorCredentials);
                Directory.CreateDirectory(folder);
                var name = $"doc_{doctor.DoctorId}_{Guid.NewGuid():N}{ext}";
                await using (var stream = System.IO.File.Create(Path.Combine(folder, name)))
                    await file.CopyToAsync(stream);
                row.FileName = Path.GetFileName(file.FileName);
                row.FilePath = UploadedMedia.MediaRelative(UploadedMedia.DoctorCredentials, name);
                row.ContentType = file.ContentType;
            }

            row.Degree = TrimTo(degree, 50);
            row.Specialization = TrimTo(specialization, 150);
            row.Institution = TrimTo(institution, 200);
            row.PassingYear = passingYear is > 1900 and < 2200 ? passingYear : null;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, data = MapCredential(row) });
        }

        [HttpDelete("Me/CredentialDocuments/{id:int}")]
        public async Task<IActionResult> DeleteCredentialDocument(int id)
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });
            var row = await _context.DoctorCredentialDocuments
                .FirstOrDefaultAsync(d => d.DoctorCredentialDocumentId == id && d.DoctorId == doctor.DoctorId && !d.DeleteStatus);
            if (row == null)
                return NotFound(new { success = false, message = "Document not found." });
            row.DeleteStatus = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Document removed." });
        }

        /// <summary>Doctor photo stored under Data/UploadedMedia/DoctorPhotos. Public: the same photo is shown on the doctor directory.</summary>
        [AllowAnonymous]
        [HttpGet("Photo/{doctorId:int}")]
        public async Task<IActionResult> DoctorPhoto(int doctorId)
        {
            var photoPath = await _context.Doctors.AsNoTracking()
                .Where(d => d.DoctorId == doctorId && !d.DeleteStatus)
                .Select(d => d.PhotoPath)
                .FirstOrDefaultAsync();
            var full = UploadedMedia.MediaRelativeToFull(_env.ContentRootPath, UploadedMedia.DoctorPhotos, photoPath);
            if (full == null)
                return NotFound(new { success = false, message = "No photo." });
            if (!System.IO.File.Exists(full))
                return NotFound(new { success = false, message = "Photo file is missing on the server." });

            return PhysicalFile(full, CredentialFileRules.PhotoContentType(full));
        }

        [HttpDelete("Me/Photo")]
        public async Task<IActionResult> RemovePhoto()
        {
            var deny = DoctorOwnership.ForbidIfReception(User);
            if (deny != null)
                return deny;
            var doctor = await ResolveDoctorAsync();
            if (doctor == null)
                return NotFound(new { success = false, message = "Doctor profile not found." });
            doctor.PhotoPath = null;
            doctor.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Photo removed." });
        }

        private static DoctorCredentialDocumentDto MapCredential(DoctorCredentialDocument row) => new()
        {
            DoctorCredentialDocumentId = row.DoctorCredentialDocumentId,
            DocumentType = row.DocumentType,
            FileName = row.FileName,
            FilePath = row.FilePath,
            Degree = row.Degree,
            Specialization = row.Specialization,
            Institution = row.Institution,
            PassingYear = row.PassingYear,
            EnteredDate = row.EnteredDate
        };

        private static string? TrimTo(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var text = value.Trim();
            return text.Length > max ? text[..max] : text;
        }

        /// <summary>Owning doctor or admin opens a credential file stored under Data/UploadedMedia/DoctorCredentials.</summary>
        [HttpGet("CredentialDocuments/{id:int}/File")]
        public async Task<IActionResult> DownloadCredentialDocument(int id)
        {
            var row = await _context.DoctorCredentialDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.DoctorCredentialDocumentId == id && !d.DeleteStatus);
            if (row == null)
                return NotFound(new { success = false, message = "Document not found." });

            if (!DoctorOwnership.IsGlobalAdminPortalUser(User))
            {
                var deny = DoctorOwnership.ForbidIfReception(User);
                if (deny != null)
                    return deny;
                var doctor = await ResolveDoctorAsync();
                if (doctor == null || doctor.DoctorId != row.DoctorId)
                    return Forbid();
            }

            var full = UploadedMedia.MediaRelativeToFull(_env.ContentRootPath, UploadedMedia.DoctorCredentials, row.FilePath);
            if (full == null)
                return BadRequest(new { success = false, message = "Document path is not valid." });
            if (!System.IO.File.Exists(full))
                return NotFound(new { success = false, message = "Document file is missing on the server." });

            var contentType = string.IsNullOrWhiteSpace(row.ContentType) ? "application/octet-stream" : row.ContentType;
            return PhysicalFile(full, contentType, row.FileName);
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
        /// Clinic name, fees, qualifications, and bank details are refused.
        /// </summary>
        private async Task<IActionResult> ReceptionUpdateAsync(DoctorProfileUpdateRequest? request)
        {
            if (request == null)
                return BadRequest(new { success = false, message = "Body required." });

            if (ReceptionSentDoctorOwnedFields(request))
            {
                return new ObjectResult(new
                {
                    success = false,
                    message = "Reception cannot change the doctor's clinic name, fees, qualifications, or bank details."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

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

        /// <summary>Doctor-owned fields on Profile/Me. Reception may send only their own staff contact fields.</summary>
        private static bool ReceptionSentDoctorOwnedFields(DoctorProfileUpdateRequest request)
        {
            return request.ClinicName != null
                || request.MiddleName != null
                || request.AddressLine2 != null
                || request.StateId.HasValue
                || request.Pincode != null
                || request.QualificationId.HasValue
                || request.PassingUniversity != null
                || request.PassingCertNo != null
                || request.ConsultFeeInClinic.HasValue
                || request.ConsultFeeTele.HasValue
                || request.TeleDisabled.HasValue
                || request.FollowUpFeeInClinic.HasValue
                || request.FollowUpFeeTele.HasValue
                || request.FreeFollowUpDaysInClinic.HasValue
                || request.FreeFollowUpDaysTele.HasValue
                || request.FeeCurrency != null
                || request.GoogleMapsLink != null
                || request.WorkingHoursNote != null
                || request.Kyc != null;
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
                PhotoUrl = PhotoUrlFor(doctor),
                WorkingHoursNote = doctor.WorkingHoursNote,
                FollowUpFeeInClinic = doctor.FollowUpFeeInClinic,
                FollowUpFeeTele = doctor.FollowUpFeeTele,
                FreeFollowUpDaysInClinic = doctor.FreeFollowUpDaysInClinic,
                FreeFollowUpDaysTele = doctor.FreeFollowUpDaysTele,
                FeeCurrency = doctor.FeeCurrency,
                GoogleMapsLink = doctor.GoogleMapsLink,
                IsOnline = doctor.IsOnline,
                VerificationStatus = doctor.VerificationStatus,
                DirectoryVisible = doctor.DirectoryVisible,
                Kyc = kyc == null ? null : new DoctorPayeeKycDto
                {
                    AccountHolder = kyc.AccountHolder,
                    BankName = kyc.BankName,
                    AccountNumber = kyc.AccountNumber,
                    Ifsc = kyc.Ifsc,
                    Pan = kyc.Pan,
                    BranchName = kyc.BranchName,
                    AccountType = kyc.AccountType
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
