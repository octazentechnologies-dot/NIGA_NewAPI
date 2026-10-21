using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Helpers;
using Homeocentrum.Niga.API.Domain.Master;
using Homeocentrum.Niga.API.Domain.Security;
using Homeocentrum.Niga.API.Domain.Services;

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
        private readonly IStoredFileStore _files;

        public DoctorProfileController(NIGACentrumContext context, IWebHostEnvironment env, IStoredFileStore files)
        {
            _context = context;
            _env = env;
            _files = files;
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
            if (request.CasePaperValidity.HasValue)
                doctor.CasePaperValidity = request.CasePaperValidity.Value > 0 ? request.CasePaperValidity : null;
            if (request.PracticeActivated.HasValue)
                doctor.PracticeActivated = request.PracticeActivated.Value;
            if (request.CountryId.HasValue) doctor.CountryId = request.CountryId;
            if (request.DistrictId.HasValue) doctor.DistrictId = request.DistrictId;
            if (request.CityId.HasValue)
            {
                doctor.CityId = request.CityId;
                if (string.IsNullOrWhiteSpace(request.City))
                {
                    doctor.City = await _context.CityMasters.AsNoTracking()
                        .Where(c => c.CityId == request.CityId.Value)
                        .Select(c => c.CityName)
                        .FirstOrDefaultAsync();
                }
            }
            if (request.PinCodeId.HasValue)
            {
                doctor.PinCodeId = request.PinCodeId;
                if (string.IsNullOrWhiteSpace(request.Pincode))
                {
                    var pin = await _context.PinCodeMasters.AsNoTracking()
                        .Where(p => p.PinCodeId == request.PinCodeId.Value)
                        .Select(p => p.PinCode)
                        .FirstOrDefaultAsync();
                    if (!string.IsNullOrWhiteSpace(pin))
                    {
                        var parsedPin = ParseClinicAddress(doctor.PermanantAddress);
                        doctor.PermanantAddress = FormatClinicAddress(parsedPin.Line1, parsedPin.Line2, pin);
                    }
                }
            }
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
                    if (doctor.CountryId.HasValue) user.CountryId = doctor.CountryId;
                    if (doctor.StateId.HasValue) user.StateId = doctor.StateId;
                    if (doctor.DistrictId.HasValue) user.DistrictId = doctor.DistrictId;
                    if (doctor.CityId.HasValue) user.CityId = doctor.CityId;
                    if (doctor.PinCodeId.HasValue) user.PinCodeId = doctor.PinCodeId;
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
                if (request.Kyc.Pan != null)
                    kyc.Pan = string.IsNullOrWhiteSpace(request.Kyc.Pan) ? null : request.Kyc.Pan.Trim().ToUpperInvariant();
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

            string stored;
            try
            {
                await using var upload = file.OpenReadStream();
                using var memory = new MemoryStream();
                await upload.CopyToAsync(memory);
                stored = await _files.SaveAsync($"doctors/{doctor.DoctorId}/profile-images", ext, memory.ToArray());
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
            }

            var previous = doctor.PhotoPath;
            doctor.PhotoPath = stored;
            doctor.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            if (!string.IsNullOrWhiteSpace(previous) && !string.Equals(previous, stored, StringComparison.Ordinal))
                await _files.DeleteAsync(_env.ContentRootPath, previous);
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
            await FillStoredQualificationAsync(doctor, docs);

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
            string stored;
            try
            {
                await using var upload = file.OpenReadStream();
                using var memory = new MemoryStream();
                await upload.CopyToAsync(memory);
                stored = await _files.SaveAsync($"doctors/{doctor.DoctorId}/credentials", ext, memory.ToArray());
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
            }

            var canonical = CredentialFileRules.CanonicalType(documentType);

            var row = new DoctorCredentialDocument
            {
                DoctorId = doctor.DoctorId,
                DoctorVerificationId = verification.DoctorVerificationId,
                DocumentType = canonical,
                FileName = Path.GetFileName(file.FileName),
                FilePath = stored,
                ContentType = file.ContentType,
                Degree = TrimTo(degree, 50),
                Specialization = TrimTo(specialization, 150),
                Institution = TrimTo(institution, 200),
                PassingYear = passingYear is > 1900 and < 2200 ? passingYear : null,
                EnteredDate = DateTime.UtcNow,
                DeleteStatus = false
            };

            _context.DoctorCredentialDocuments.Add(row);
            var statusResetToPending = await ApplyCredentialsResubmittedAsync(doctor);
            await _context.SaveChangesAsync();
            if (statusResetToPending)
                await InsertDocumentsResubmittedEventAsync(doctor.DoctorId);
            return Ok(new
            {
                success = true,
                data = MapCredential(row),
                verificationStatus = string.IsNullOrWhiteSpace(doctor.VerificationStatus) ? "Pending" : doctor.VerificationStatus,
                statusResetToPending
            });
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
                string stored;
                try
                {
                    await using var upload = file.OpenReadStream();
                    using var memory = new MemoryStream();
                    await upload.CopyToAsync(memory);
                    stored = await _files.SaveAsync($"doctors/{doctor.DoctorId}/credentials", ext, memory.ToArray());
                }
                catch (InvalidOperationException)
                {
                    return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
                }
                var previous = row.FilePath;
                row.FileName = Path.GetFileName(file.FileName);
                row.FilePath = stored;
                row.ContentType = file.ContentType;
                if (!string.IsNullOrWhiteSpace(previous) && !string.Equals(previous, stored, StringComparison.Ordinal))
                    await _files.DeleteAsync(_env.ContentRootPath, previous);
            }

            row.Degree = TrimTo(degree, 50);
            row.Specialization = TrimTo(specialization, 150);
            row.Institution = TrimTo(institution, 200);
            row.PassingYear = passingYear is > 1900 and < 2200 ? passingYear : null;

            // Replacing the file counts as a resubmit when admin asked for more info or rejected.
            var statusResetToPending = false;
            if (file != null && file.Length > 0)
                statusResetToPending = await ApplyCredentialsResubmittedAsync(doctor);

            await _context.SaveChangesAsync();
            if (statusResetToPending)
                await InsertDocumentsResubmittedEventAsync(doctor.DoctorId);
            return Ok(new
            {
                success = true,
                data = MapCredential(row),
                verificationStatus = string.IsNullOrWhiteSpace(doctor.VerificationStatus) ? "Pending" : doctor.VerificationStatus,
                statusResetToPending
            });
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
            byte[]? bytes;
            try
            {
                bytes = await _files.ReadAsync(_env.ContentRootPath, photoPath);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
            }
            if (bytes == null || bytes.Length == 0)
                return NotFound(new { success = false, message = "Photo file is missing." });

            return File(bytes, CredentialFileRules.PhotoContentType(photoPath));
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
            var previous = doctor.PhotoPath;
            doctor.PhotoPath = null;
            doctor.ChangedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _files.DeleteAsync(_env.ContentRootPath, previous);
            return Ok(new { success = true, message = "Photo removed." });
        }

        private static DoctorCredentialDocumentDto MapCredential(DoctorCredentialDocument row) => new()
        {
            DoctorCredentialDocumentId = row.DoctorCredentialDocumentId,
            DocumentType = row.DocumentType,
            FileName = row.FileName,
            FilePath = row.FilePath,
            Degree = TextOrNull(row.Degree),
            Specialization = TextOrNull(row.Specialization),
            Institution = TextOrNull(row.Institution),
            PassingYear = row.PassingYear,
            EnteredDate = row.EnteredDate
        };

        /// <summary>
        /// Qualification files uploaded before degree details were saved still belong to the
        /// doctor's stored qualification. Copy those stored values onto the file row in the response.
        /// </summary>
        private async Task FillStoredQualificationAsync(Doctor doctor, List<DoctorCredentialDocumentDto> docs)
        {
            string? qualificationName = null;
            if (doctor.QualificationId != null)
            {
                qualificationName = await _context.QualificationMasters.AsNoTracking()
                    .Where(q => q.QualificationId == doctor.QualificationId)
                    .Select(q => q.QualificationName)
                    .FirstOrDefaultAsync();
            }
            var university = TextOrNull(doctor.PassingUniversity);
            qualificationName = TextOrNull(qualificationName);
            foreach (var doc in docs)
            {
                doc.Degree = TextOrNull(doc.Degree);
                doc.Specialization = TextOrNull(doc.Specialization);
                doc.Institution = TextOrNull(doc.Institution);
                doc.FileName = TextOrNull(doc.FileName) ?? string.Empty;
                doc.FilePath = TextOrNull(doc.FilePath) ?? string.Empty;
                if (!string.Equals(doc.DocumentType, "Qualification", StringComparison.OrdinalIgnoreCase))
                    continue;
                doc.Degree ??= qualificationName;
                doc.Institution ??= university;
            }
        }

        private static string? TextOrNull(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            return value.Trim();
        }

        private static async Task<string?> NameOrNull(int? id, Func<int, Task<string?>> load)
        {
            if (id is not > 0)
                return null;
            return TextOrNull(await load(id.Value));
        }

        private async Task<string?> PackageNameForAsync(Doctor doctor)
        {
            var lookupIds = new List<int> { doctor.DoctorId };
            if (doctor.UserId is int linkedUser && linkedUser > 0)
                lookupIds.Add(linkedUser);
            var packageId = await _context.PackageEntryDetails.AsNoTracking()
                .Where(p => p.IsActive == true && p.DoctorId != null && lookupIds.Contains(p.DoctorId.Value) && p.PackageId != null)
                .OrderByDescending(p => p.ExpiryDate)
                .Select(p => p.PackageId)
                .FirstOrDefaultAsync();
            if (packageId is not > 0)
                packageId = doctor.PackageId;
            if (packageId is not > 0)
                return null;
            var name = await _context.PackageMasters.AsNoTracking()
                .Where(p => p.PackageId == packageId.Value)
                .Select(p => p.PackageName)
                .FirstOrDefaultAsync();
            return TextOrNull(name);
        }

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

            byte[]? bytes;
            try
            {
                bytes = await _files.ReadAsync(_env.ContentRootPath, row.FilePath);
            }
            catch (InvalidOperationException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "File storage is unavailable." });
            }
            if (bytes == null || bytes.Length == 0)
                return NotFound(new { success = false, message = "Document file is missing." });

            var contentType = string.IsNullOrWhiteSpace(row.ContentType) ? "application/octet-stream" : row.ContentType;
            return File(bytes, contentType, row.FileName);
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

        /// <summary>
        /// After Rejected or NeedsInfo, a new/replaced credential file returns the doctor to Pending
        /// so admin sees them in the open queue again. Does not change Verified or existing Pending.
        /// Call <see cref="InsertDocumentsResubmittedEventAsync"/> after a successful SaveChanges.
        /// </summary>
        private async Task<bool> ApplyCredentialsResubmittedAsync(Doctor doctor)
        {
            var current = (doctor.VerificationStatus ?? "").Trim();
            var resubmittable =
                current.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                || current.Equals("NeedsInfo", StringComparison.OrdinalIgnoreCase);
            if (!resubmittable)
                return false;

            doctor.VerificationStatus = "Pending";
            doctor.ChangedDate = DateTime.UtcNow;

            var verification = await _context.DoctorVerifications
                .FirstOrDefaultAsync(v => v.DoctorId == doctor.DoctorId && !v.DeleteStatus);
            if (verification != null)
            {
                verification.Status = "Pending";
                verification.ChangedDate = DateTime.UtcNow;
            }

            return true;
        }

        private async Task InsertDocumentsResubmittedEventAsync(int doctorId)
        {
            var byUserId = User.GetUserId();
            var status = "Pending";
            var note = "Documents resubmitted";
            var at = DateTime.Now;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.DoctorVerificationEvent (DoctorId, Status, Note, ByUserId, At)
                VALUES ({doctorId}, {status}, {note}, {byUserId}, {at})");
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

            UserMaster account = null;
            string roleName = null;
            if (doctor.UserId is int linkedUserId && linkedUserId > 0)
            {
                account = await _context.UserMasters.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == linkedUserId && !u.DeleteStatus);
                if (account?.RoleId is int roleId)
                {
                    roleName = await _context.RoleMasters.AsNoTracking()
                        .Where(r => r.RoleId == roleId)
                        .Select(r => r.RoleName)
                        .FirstOrDefaultAsync();
                }
            }

            var kycDto = kyc == null ? null : new DoctorPayeeKycDto
            {
                AccountHolder = TextOrNull(kyc.AccountHolder),
                BankName = TextOrNull(kyc.BankName),
                AccountNumber = TextOrNull(kyc.AccountNumber),
                Ifsc = TextOrNull(kyc.Ifsc),
                Pan = TextOrNull(kyc.Pan),
                BranchName = TextOrNull(kyc.BranchName),
                AccountType = TextOrNull(kyc.AccountType)
            };
            if (kycDto != null
                && kycDto.AccountHolder == null
                && kycDto.BankName == null
                && kycDto.AccountNumber == null
                && kycDto.Ifsc == null
                && kycDto.Pan == null
                && kycDto.BranchName == null
                && kycDto.AccountType == null)
            {
                kycDto = null;
            }

            return new DoctorProfileMeDto
            {
                DoctorId = doctor.DoctorId,
                UserId = doctor.UserId ?? 0,
                LoginId = TextOrNull(account?.UserName),
                RoleName = TextOrNull(roleName),
                FirstName = TextOrNull(doctor.FirstName) ?? TextOrNull(account?.FirstName) ?? string.Empty,
                MiddleName = TextOrNull(doctor.MiddleName),
                LastName = TextOrNull(doctor.LastName) ?? TextOrNull(account?.LastName) ?? string.Empty,
                ClinicName = TextOrNull(doctor.ClinicName),
                EmailId = TextOrNull(doctor.EmailId) ?? TextOrNull(account?.EmailId),
                MobileNo = TextOrNull(doctor.MobileNo) ?? TextOrNull(account?.MobileNo),
                City = TextOrNull(doctor.City),
                AddressLine1 = TextOrNull(parsed.Line1),
                AddressLine2 = TextOrNull(parsed.Line2),
                State = TextOrNull(stateName),
                StateId = doctor.StateId,
                Pincode = TextOrNull(parsed.Pincode),
                QualificationId = doctor.QualificationId,
                QualificationName = TextOrNull(qual),
                PassingUniversity = TextOrNull(doctor.PassingUniversity),
                PassingCertNo = TextOrNull(doctor.PassingCertNo),
                RegistrationNo = TextOrNull(doctor.PassingCertNo),
                CasePaperValidity = doctor.CasePaperValidity is > 0 ? doctor.CasePaperValidity : null,
                PracticeActivated = doctor.PracticeActivated,
                PackageName = await PackageNameForAsync(doctor),
                CountryId = doctor.CountryId,
                CountryName = await NameOrNull(doctor.CountryId, id => _context.CountryMasters.AsNoTracking().Where(c => c.CountryId == id).Select(c => c.CountryName).FirstOrDefaultAsync()),
                DistrictId = doctor.DistrictId,
                DistrictName = await NameOrNull(doctor.DistrictId, id => _context.DistrictMasters.AsNoTracking().Where(d => d.DistrictId == id).Select(d => d.DistrictName).FirstOrDefaultAsync()),
                CityId = doctor.CityId,
                CityName = await NameOrNull(doctor.CityId, id => _context.CityMasters.AsNoTracking().Where(c => c.CityId == id).Select(c => c.CityName).FirstOrDefaultAsync()) ?? TextOrNull(doctor.City),
                PinCodeId = doctor.PinCodeId,
                ConsultFeeInClinic = doctor.ConsultFeeInClinic,
                ConsultFeeTele = doctor.ConsultFeeTele,
                PhotoPath = TextOrNull(doctor.PhotoPath),
                PhotoUrl = string.IsNullOrWhiteSpace(doctor.PhotoPath) ? null : PhotoUrlFor(doctor),
                WorkingHoursNote = TextOrNull(doctor.WorkingHoursNote),
                FollowUpFeeInClinic = doctor.FollowUpFeeInClinic,
                FollowUpFeeTele = doctor.FollowUpFeeTele,
                FreeFollowUpDaysInClinic = doctor.FreeFollowUpDaysInClinic,
                FreeFollowUpDaysTele = doctor.FreeFollowUpDaysTele,
                FeeCurrency = TextOrNull(doctor.FeeCurrency),
                GoogleMapsLink = TextOrNull(doctor.GoogleMapsLink),
                IsOnline = doctor.IsOnline,
                VerificationStatus = TextOrNull(doctor.VerificationStatus) ?? "Pending",
                DirectoryVisible = doctor.DirectoryVisible,
                Kyc = kycDto
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
