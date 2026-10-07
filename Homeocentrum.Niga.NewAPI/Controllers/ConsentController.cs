using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Authorization;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Master;
using Homeocentrum.Niga.NewAPI.Domain.Privacy;
using Homeocentrum.Niga.NewAPI.Domain.Security;
using Homeocentrum.Niga.NewAPI.Domain.Security.Audit;

namespace Homeocentrum.Niga.NewAPI.Controllers
{
    /// <summary>
    /// SEC-06.02 — Consent grant / withdraw / list / admin audit (no clinical content).
    /// Every consent records the notice version it was given against; for patients under 18 a parent or guardian
    /// consents and the way that adult was verified is recorded (DPDP Act 2023 s.5, s.6, s.9).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [ForbidMoneyRoles]
    public class ConsentController : ControllerBase
    {
        private const string SubjectUser = "User";
        private const string SubjectPatient = "Patient";
        private const string PrivacyCode = "Privacy";
        private const string DefaultLanguage = "en";
        private static readonly TimeSpan GuardianOtpValidFor = TimeSpan.FromMinutes(30);

        private readonly NIGACentrumContext _context;
        private readonly IPatientAccessGuard _patientAccess;
        private readonly ISecurityAuditLog _audit;

        public ConsentController(NIGACentrumContext context, IPatientAccessGuard patientAccess, ISecurityAuditLog audit)
        {
            _context = context;
            _patientAccess = patientAccess;
            _audit = audit;
        }

        /// <summary>Current notice for a consent type (falls back to English).</summary>
        [HttpGet("Notice/{consentTypeCode}")]
        public async Task<IActionResult> Notice(string consentTypeCode, [FromQuery] string? language = null)
        {
            var type = await ActiveTypeAsync(consentTypeCode);
            if (type == null)
                return NotFound(new { success = false, message = "Unknown or inactive consent type." });
            var notice = await CurrentNoticeAsync(type.ConsentTypeId, language);
            if (notice == null)
                return NotFound(new { success = false, code = "NOTICE_NOT_PUBLISHED", message = "No notice is published for this consent type." });
            return Ok(new { success = true, data = NoticeDto(type, notice) });
        }

        /// <summary>All published versions of a notice, newest first.</summary>
        [HttpGet("Notice/{consentTypeCode}/History")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> NoticeHistory(string consentTypeCode)
        {
            var type = await ActiveTypeAsync(consentTypeCode);
            if (type == null)
                return NotFound(new { success = false, message = "Unknown or inactive consent type." });
            var rows = await _context.ConsentNotices.AsNoTracking()
                .Where(n => n.ConsentTypeId == type.ConsentTypeId)
                .OrderByDescending(n => n.EffectiveFrom).ThenBy(n => n.Language)
                .ToListAsync();
            return Ok(new { success = true, data = rows.Select(n => NoticeDto(type, n)) });
        }

        /// <summary>Publishes a new notice version and makes it current for its language. Published text never changes.</summary>
        [SecurityAudit(SecurityAuditEvents.ConsentNoticePublished)]
        [HttpPost("Notice")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> PublishNotice([FromBody] ConsentNoticePublishRequest request)
        {
            var type = await ActiveTypeAsync(request.ConsentTypeCode);
            if (type == null)
                return BadRequest(new { success = false, message = "Unknown or inactive consent type." });

            var version = request.Version.Trim();
            var language = NormalizeLanguage(request.Language);
            if (await _context.ConsentNotices.AnyAsync(n => n.ConsentTypeId == type.ConsentTypeId && n.Version == version && n.Language == language))
                return Conflict(new { success = false, code = "NOTICE_VERSION_EXISTS", message = "This version already exists. Use a new version number." });

            var now = DateTime.UtcNow;
            await using var tx = await _context.Database.BeginTransactionAsync();
            var previous = await _context.ConsentNotices
                .Where(n => n.ConsentTypeId == type.ConsentTypeId && n.Language == language && n.IsCurrent)
                .ToListAsync();
            foreach (var p in previous)
                p.IsCurrent = false;
            await _context.SaveChangesAsync();

            var notice = new ConsentNotice
            {
                ConsentTypeId = type.ConsentTypeId,
                Version = version,
                Language = language,
                Title = request.Title.Trim(),
                Body = request.Body,
                BodySha256 = ConsentRules.NoticeSha256(request.Body),
                EffectiveFrom = now,
                RequiresReconsent = request.RequiresReconsent,
                IsCurrent = true,
                CreatedByUserId = User.GetUserId(),
                CreatedAt = now
            };
            _context.ConsentNotices.Add(notice);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new { success = true, data = NoticeDto(type, notice) });
        }

        [HttpPost("Grant")]
        public async Task<IActionResult> Grant([FromBody] ConsentGrantRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ConsentTypeCode))
                return BadRequest(new { success = false, message = "ConsentTypeCode is required." });

            var type = await ActiveTypeAsync(request.ConsentTypeCode);
            if (type == null)
                return BadRequest(new { success = false, message = "Unknown or inactive consent type." });

            var notice = await CurrentNoticeAsync(type.ConsentTypeId, request.NoticeLanguage);
            if (notice == null)
                return Conflict(new { success = false, code = "NOTICE_NOT_PUBLISHED", message = "No notice is published for this consent type." });
            if (!string.IsNullOrWhiteSpace(request.NoticeVersion) && request.NoticeVersion.Trim() != notice.Version)
            {
                return Conflict(new
                {
                    success = false,
                    code = "NOTICE_OUTDATED",
                    message = "The notice has changed. Read the current version and consent again.",
                    currentVersion = notice.Version
                });
            }

            var callerId = (long)User.GetUserId();
            var subjectType = NormalizeSubjectType(request.SubjectType);
            long subjectId;
            int? patientId;
            switch (subjectType)
            {
                case SubjectUser:
                    subjectId = request.SubjectId > 0 ? request.SubjectId : callerId;
                    if (subjectId != callerId && !User.IsAdminPortalUser())
                        return Forbid();
                    patientId = await PrimaryPatientIdAsync(subjectId);
                    break;
                case SubjectPatient:
                    if (request.SubjectId <= 0 || request.SubjectId > int.MaxValue)
                        return BadRequest(new { success = false, message = "SubjectId must be a patient id." });
                    subjectId = request.SubjectId;
                    patientId = (int)subjectId;
                    if (!await _patientAccess.CanAccessPatientAsync(User, patientId.Value))
                        return Forbid();
                    break;
                default:
                    if (!User.IsAdminPortalUser())
                        return Forbid();
                    subjectId = request.SubjectId > 0 ? request.SubjectId : callerId;
                    patientId = null;
                    break;
            }

            var minor = patientId.HasValue && await IsMinorAsync(patientId.Value) == true;
            GuardianEvidence? guardian = null;
            if (minor)
            {
                if (request.Guardian == null)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, new
                    {
                        success = false,
                        code = "GUARDIAN_CONSENT_REQUIRED",
                        message = "This patient is under 18. A parent or legal guardian must give consent."
                    });
                }
                var (evidence, rejection) = await VerifyGuardianAsync(request.Guardian, patientId!.Value, callerId);
                if (rejection != null)
                    return rejection;
                guardian = evidence;
            }

            var reconsentDates = await ReconsentDatesAsync(type.ConsentTypeId);
            var existing = await _context.ConsentRecords.AsNoTracking()
                .Where(r => r.ConsentTypeId == type.ConsentTypeId && r.SubjectType == subjectType && r.SubjectId == subjectId)
                .OrderByDescending(r => r.GrantedAt)
                .FirstOrDefaultAsync();
            if (existing != null
                && Evaluate(existing, minor ? true : patientId.HasValue ? await IsMinorAsync(patientId.Value) : null, reconsentDates) == ConsentState.Valid)
            {
                return Ok(new { success = true, alreadyGranted = true, data = RecordDto(existing, type.Code) });
            }

            var record = new ConsentRecord
            {
                ConsentTypeId = type.ConsentTypeId,
                SubjectType = subjectType,
                SubjectId = subjectId,
                GrantedByUserId = callerId,
                GrantedAt = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Truncate(Request.Headers.UserAgent.ToString(), 500),
                Notes = request.Notes,
                ConsentNoticeId = notice.ConsentNoticeId,
                NoticeVersion = notice.Version,
                NoticeLanguage = notice.Language,
                NoticeSha256 = notice.BodySha256,
                GrantedForMinor = guardian != null,
                GuardianUserId = guardian?.UserId,
                GuardianName = guardian?.Name,
                GuardianRelationship = guardian?.Relationship,
                GuardianVerificationMethod = guardian?.Method,
                GuardianVerificationRef = guardian?.Reference,
                GuardianMobileMasked = guardian?.MobileMasked
            };
            _context.ConsentRecords.Add(record);
            await _context.SaveChangesAsync();

            if (guardian != null)
            {
                await _audit.WriteAsync(SecurityAuditEvents.GuardianConsent, HttpContext, "SUCCESS", callerId,
                    DoctorOwnership.GetRoleName(User), $"patient:{patientId}",
                    $"consent={type.Code}; notice={notice.Version}; method={guardian.Method}; record={record.ConsentRecordId}");
            }

            return Ok(new { success = true, data = RecordDto(record, type.Code) });
        }

        [HttpPost("Withdraw")]
        public async Task<IActionResult> Withdraw([FromBody] ConsentWithdrawRequest request)
        {
            if (request == null || request.ConsentRecordId <= 0)
                return BadRequest(new { success = false, message = "ConsentRecordId is required." });

            var record = await _context.ConsentRecords
                .FirstOrDefaultAsync(r => r.ConsentRecordId == request.ConsentRecordId);
            if (record == null)
                return NotFound(new { success = false, message = "Consent record not found." });

            if (!await CanManageRecordAsync(record))
                return Forbid();

            if (record.WithdrawnAt != null)
                return Ok(new { success = true, message = "Already withdrawn." });

            record.WithdrawnAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = "Consent withdrawn." });
        }

        [HttpGet("ListMine")]
        public async Task<IActionResult> ListMine()
        {
            var userId = (long)User.GetUserId();
            var patientId = await PrimaryPatientIdAsync(userId);
            var rows = await RecordsQuery(userIds: new[] { userId }, patientId: patientId).ToListAsync();
            return Ok(new { success = true, data = rows.Select(r => RecordDto(r, r.ConsentType!.Code)) });
        }

        /// <summary>Consents for one patient (patient subject plus the patient's own login), for the patient, family or clinic.</summary>
        [HttpGet("Patient/{patientId:int}")]
        public async Task<IActionResult> ListForPatient(int patientId)
        {
            if (!await _patientAccess.CanAccessPatientAsync(User, patientId))
                return Forbid();
            var userIds = await PatientUserIdsAsync(patientId);
            var rows = await RecordsQuery(userIds, patientId).ToListAsync();
            return Ok(new { success = true, data = rows.Select(r => RecordDto(r, r.ConsentType!.Code)) });
        }

        [HttpGet("AdminAudit")]
        [Authorize(Policy = AdminAuthorizationPolicies.AdminPortal)]
        public async Task<IActionResult> AdminAudit([FromQuery] int take = 100)
        {
            take = Math.Clamp(take, 1, 500);
            var rows = await _context.ConsentRecords
                .AsNoTracking()
                .Include(r => r.ConsentType)
                .OrderByDescending(r => r.GrantedAt)
                .Take(take)
                .Select(r => new
                {
                    r.ConsentRecordId,
                    ConsentTypeCode = r.ConsentType!.Code,
                    r.SubjectType,
                    r.SubjectId,
                    r.GrantedByUserId,
                    r.GrantedAt,
                    r.WithdrawnAt,
                    r.IpAddress,
                    r.NoticeVersion,
                    r.NoticeLanguage,
                    r.NoticeSha256,
                    r.GrantedForMinor,
                    r.GuardianName,
                    r.GuardianRelationship,
                    r.GuardianVerificationMethod
                })
                .ToListAsync();

            return Ok(new { success = true, data = rows });
        }

        /// <summary>
        /// PAT-05.02 — Privacy consent status for the JWT user, or for <paramref name="patientId"/> (family / clinic).
        /// Not granted when withdrawn, when a notice version requiring fresh consent came later, when the patient is a
        /// minor without guardian consent, or when a guardian consented and the patient has since turned 18.
        /// </summary>
        [HttpGet("PrivacyStatus")]
        public async Task<IActionResult> PrivacyStatus([FromQuery] int? patientId = null)
        {
            IReadOnlyCollection<long> userIds;
            int? subjectPatientId;
            if (patientId.HasValue)
            {
                if (!await _patientAccess.CanAccessPatientAsync(User, patientId.Value))
                    return Forbid();
                subjectPatientId = patientId;
                userIds = await PatientUserIdsAsync(patientId.Value);
            }
            else
            {
                var userId = (long)User.GetUserId();
                userIds = new[] { userId };
                subjectPatientId = await PrimaryPatientIdAsync(userId);
            }

            var type = await ActiveTypeAsync(PrivacyCode);
            if (type == null)
                return Ok(new { success = true, data = new { required = true, granted = false, missingType = true } });

            var notice = await CurrentNoticeAsync(type.ConsentTypeId, null);
            var reconsentDates = await ReconsentDatesAsync(type.ConsentTypeId);
            var minor = subjectPatientId.HasValue ? await IsMinorAsync(subjectPatientId.Value) : null;

            var latest = await RecordsQuery(userIds, subjectPatientId)
                .Where(r => r.ConsentTypeId == type.ConsentTypeId)
                .FirstOrDefaultAsync();
            var state = latest == null
                ? ConsentState.None
                : Evaluate(latest, minor, reconsentDates);
            var granted = state == ConsentState.Valid;

            return Ok(new
            {
                success = true,
                data = new
                {
                    required = true,
                    granted,
                    status = state.ToString(),
                    patientId = subjectPatientId,
                    isMinor = minor == true,
                    guardianRequired = minor == true && !granted,
                    reconsentRequired = state is ConsentState.NoticeChanged or ConsentState.MajorityReached,
                    grantedForMinor = latest?.GrantedForMinor ?? false,
                    guardianName = latest?.GuardianName,
                    consentRecordId = latest?.ConsentRecordId,
                    grantedAt = latest?.GrantedAt,
                    withdrawnAt = latest?.WithdrawnAt,
                    grantedNoticeVersion = latest?.NoticeVersion,
                    currentNoticeVersion = notice?.Version
                }
            });
        }

        /// <summary>PAT-05.02 — Grant Privacy consent for the caller. Idempotent if already granted.</summary>
        [HttpPost("GrantPrivacy")]
        public Task<IActionResult> GrantPrivacy([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] PrivacyGrantRequest? request = null)
        {
            return Grant(new ConsentGrantRequest
            {
                ConsentTypeCode = PrivacyCode,
                SubjectType = SubjectUser,
                SubjectId = User.GetUserId(),
                NoticeVersion = request?.NoticeVersion,
                NoticeLanguage = request?.NoticeLanguage
            });
        }

        private sealed record GuardianEvidence(string Method, string Name, string Relationship, long? UserId, string Reference, string? MobileMasked);

        private async Task<(GuardianEvidence? Evidence, IActionResult? Rejection)> VerifyGuardianAsync(
            GuardianConsentInput input, int patientId, long callerId)
        {
            if (!input.DeclaresLegalGuardian)
            {
                return (null, BadRequest(new
                {
                    success = false,
                    code = "GUARDIAN_DECLARATION_REQUIRED",
                    message = "Confirm that the person giving consent is the child's parent or legal guardian and is 18 or older."
                }));
            }

            var method = GuardianMethods.Normalize(input.Method);
            if (method == null)
                return (null, BadRequest(new { success = false, message = "Guardian method must be FamilyAccount, InClinic or Otp." }));

            var name = input.GuardianName?.Trim();
            var relationship = input.Relationship?.Trim();

            switch (method)
            {
                case GuardianMethods.FamilyAccount:
                {
                    var family = await _context.PatientFamilyMembers.AsNoTracking()
                        .FirstOrDefaultAsync(f => f.OwnerUserId == callerId && f.MemberPatientId == patientId && !f.DeleteStatus);
                    var caregiver = family != null ? null : await _context.CaregiverAuthorizations.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.CaregiverUserId == callerId && c.PatientId == patientId && c.RevokedAt == null && !c.DeleteStatus);
                    if (family == null && caregiver == null)
                    {
                        return (null, StatusCode(StatusCodes.Status403Forbidden, new
                        {
                            success = false,
                            code = "GUARDIAN_NOT_LINKED",
                            message = "Sign in with the parent's family account that lists this child, or record the consent at the clinic."
                        }));
                    }

                    var callerPatientId = await PrimaryPatientIdAsync(callerId);
                    if (callerPatientId == patientId)
                        return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "GUARDIAN_CONSENT_REQUIRED", message = "A child cannot consent for themselves." }));
                    if (callerPatientId.HasValue && await IsMinorAsync(callerPatientId.Value) == true)
                        return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, code = "GUARDIAN_MINOR", message = "The parent or guardian must be 18 or older." }));

                    var user = await _context.UserMasters.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == callerId);
                    var userName = string.Join(" ", new[] { user?.FirstName, user?.LastName }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
                    return (new GuardianEvidence(
                        method,
                        string.IsNullOrWhiteSpace(name) ? (string.IsNullOrWhiteSpace(userName) ? user?.UserName ?? "Family account" : userName) : name,
                        string.IsNullOrWhiteSpace(relationship) ? family?.Relation ?? "Caregiver" : relationship,
                        callerId,
                        family != null ? $"family:{family.FamilyMemberId}" : $"caregiver:{caregiver!.CaregiverAuthorizationId}",
                        null), null);
                }

                case GuardianMethods.InClinic:
                {
                    if (!DoctorOwnership.GetDoctorId(User).HasValue && !User.IsAdminPortalUser())
                        return (null, StatusCode(StatusCodes.Status403Forbidden, new { success = false, message = "Only clinic staff can record in-clinic guardian consent." }));
                    if (!await _patientAccess.CanAccessPatientAsync(User, patientId))
                        return (null, Forbid());
                    var idProof = GuardianMethods.NormalizeIdProof(input.IdProofType);
                    if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationship) || idProof == null)
                    {
                        return (null, BadRequest(new
                        {
                            success = false,
                            message = "Guardian name, relationship and the identity document type checked are required ("
                                      + string.Join(", ", GuardianMethods.IdProofTypes) + ")."
                        }));
                    }
                    return (new GuardianEvidence(method, name, relationship, null, $"staff:{callerId};idProof:{idProof}", null), null);
                }

                default:
                {
                    if (input.OtpChallengeId is not > 0 || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(relationship))
                        return (null, BadRequest(new { success = false, message = "Guardian name, relationship and a verified OTP challenge are required." }));

                    var challengeId = input.OtpChallengeId.Value;
                    var entityId = patientId.ToString();
                    var challenge = await _context.OtpChallenges.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.OtpChallengeId == challengeId);
                    var fresh = DateTime.UtcNow - GuardianOtpValidFor;
                    if (challenge == null
                        || !string.Equals(challenge.Action, GuardianMethods.OtpAction, StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(challenge.EntityType, SubjectPatient, StringComparison.OrdinalIgnoreCase)
                        || challenge.EntityId != entityId
                        || challenge.VerifiedAt == null
                        || challenge.VerifiedAt < fresh)
                    {
                        return (null, BadRequest(new
                        {
                            success = false,
                            code = "GUARDIAN_OTP_INVALID",
                            message = "The guardian OTP is missing, not verified, expired, or for another patient."
                        }));
                    }

                    var reference = $"otp:{challengeId}";
                    if (await _context.ConsentRecords.AnyAsync(r => r.GuardianVerificationRef == reference))
                        return (null, Conflict(new { success = false, code = "GUARDIAN_OTP_USED", message = "This OTP was already used for a consent. Request a new one." }));

                    return (new GuardianEvidence(method, name, relationship, null, reference, challenge.DestinationMasked), null);
                }
            }
        }

        private async Task<bool> CanManageRecordAsync(ConsentRecord record)
        {
            if (User.IsAdminPortalUser())
                return true;
            if (record.SubjectType.Equals(SubjectUser, StringComparison.OrdinalIgnoreCase))
                return record.SubjectId == User.GetUserId();
            if (record.SubjectType.Equals(SubjectPatient, StringComparison.OrdinalIgnoreCase)
                && record.SubjectId is > 0 and <= int.MaxValue)
                return await _patientAccess.CanAccessPatientAsync(User, (int)record.SubjectId);
            return false;
        }

        private IQueryable<ConsentRecord> RecordsQuery(IReadOnlyCollection<long> userIds, int? patientId)
        {
            var pid = (long)(patientId ?? 0);
            return _context.ConsentRecords.AsNoTracking()
                .Include(r => r.ConsentType)
                .Where(r => (r.SubjectType == SubjectUser && userIds.Contains(r.SubjectId))
                            || (pid > 0 && r.SubjectType == SubjectPatient && r.SubjectId == pid))
                .OrderByDescending(r => r.GrantedAt)
                .ThenByDescending(r => r.ConsentRecordId);
        }

        private Task<ConsentType?> ActiveTypeAsync(string? code)
        {
            var trimmed = code?.Trim() ?? "";
            return _context.ConsentTypes.AsNoTracking().FirstOrDefaultAsync(t => t.Code == trimmed && t.IsActive);
        }

        private async Task<ConsentNotice?> CurrentNoticeAsync(int consentTypeId, string? language)
        {
            var lang = NormalizeLanguage(language);
            var current = await _context.ConsentNotices.AsNoTracking()
                .Where(n => n.ConsentTypeId == consentTypeId && n.IsCurrent && (n.Language == lang || n.Language == DefaultLanguage))
                .ToListAsync();
            return current.FirstOrDefault(n => n.Language == lang) ?? current.FirstOrDefault();
        }

        private async Task<List<DateTime>> ReconsentDatesAsync(int consentTypeId)
            => await _context.ConsentNotices.AsNoTracking()
                .Where(n => n.ConsentTypeId == consentTypeId && n.RequiresReconsent)
                .Select(n => n.EffectiveFrom)
                .ToListAsync();

        private async Task<int?> PrimaryPatientIdAsync(long userId)
        {
            var id = await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.UserId == userId && !m.DeleteStatus)
                .OrderByDescending(m => m.IsPrimary)
                .Select(m => m.PatientId)
                .FirstOrDefaultAsync();
            return id > 0 ? id : null;
        }

        private async Task<IReadOnlyCollection<long>> PatientUserIdsAsync(int patientId)
            => await _context.PatientUserMaps.AsNoTracking()
                .Where(m => m.PatientId == patientId && !m.DeleteStatus && m.IsPrimary)
                .Select(m => m.UserId)
                .ToListAsync();

        private async Task<bool?> IsMinorAsync(int patientId)
        {
            var patient = await _context.Patients.AsNoTracking()
                .Where(p => p.PatientId == patientId)
                .Select(p => new { p.DateOfBirth, p.Age })
                .FirstOrDefaultAsync();
            return patient == null ? null : ConsentRules.IsMinor(patient.DateOfBirth, patient.Age, DateTime.Today);
        }

        private static ConsentState Evaluate(ConsentRecord record, bool? minorNow, IEnumerable<DateTime> reconsentDates)
            => ConsentRules.Evaluate(true, record.GrantedAt, record.WithdrawnAt, record.GrantedForMinor, minorNow, reconsentDates, DateTime.UtcNow);

        private static string NormalizeSubjectType(string? subjectType)
        {
            if (string.IsNullOrWhiteSpace(subjectType) || subjectType.Trim().Equals(SubjectUser, StringComparison.OrdinalIgnoreCase))
                return SubjectUser;
            return subjectType.Trim().Equals(SubjectPatient, StringComparison.OrdinalIgnoreCase) ? SubjectPatient : subjectType.Trim();
        }

        private static string NormalizeLanguage(string? language)
            => string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language.Trim().ToLowerInvariant();

        private static string? Truncate(string? value, int max)
            => value == null || value.Length <= max ? value : value[..max];

        private static object NoticeDto(ConsentType type, ConsentNotice n) => new
        {
            n.ConsentNoticeId,
            ConsentTypeCode = type.Code,
            n.Version,
            n.Language,
            n.Title,
            n.Body,
            n.BodySha256,
            n.EffectiveFrom,
            n.RequiresReconsent,
            n.IsCurrent
        };

        private static object RecordDto(ConsentRecord r, string consentTypeCode) => new
        {
            r.ConsentRecordId,
            ConsentTypeCode = consentTypeCode,
            ConsentTypeName = r.ConsentType?.Name,
            r.SubjectType,
            r.SubjectId,
            r.GrantedAt,
            r.WithdrawnAt,
            IsActive = r.WithdrawnAt == null,
            r.NoticeVersion,
            r.NoticeLanguage,
            r.NoticeSha256,
            r.GrantedForMinor,
            r.GuardianName,
            r.GuardianRelationship,
            r.GuardianVerificationMethod,
            r.GuardianMobileMasked
        };
    }
}
