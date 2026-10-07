using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required]
        public string Email { get; set; } = null!;

        /// <summary>Required when the email or username matches more than one user.</summary>
        public long? UserId { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = null!;
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = null!;
    }

    public class SetUserPasswordRequest
    {
        [Required]
        public string UserName { get; set; } = null!;

        [Required]
        [MinLength(4)]
        public string NewPassword { get; set; } = null!;
    }

    public class ConsentGrantRequest
    {
        [Required]
        public string ConsentTypeCode { get; set; } = null!;

        [Required]
        public string SubjectType { get; set; } = "User";

        [Required]
        public long SubjectId { get; set; }

        public string? Notes { get; set; }

        /// <summary>Version of the notice shown to the person. A stale version is refused with NOTICE_OUTDATED.</summary>
        [MaxLength(20)]
        public string? NoticeVersion { get; set; }

        [MaxLength(10)]
        public string? NoticeLanguage { get; set; }

        /// <summary>Required when the patient is under 18.</summary>
        public GuardianConsentInput? Guardian { get; set; }
    }

    public class GuardianConsentInput
    {
        /// <summary>FamilyAccount | InClinic | Otp</summary>
        [Required]
        [MaxLength(30)]
        public string Method { get; set; } = null!;

        /// <summary>The adult confirms they are the child's parent or legal guardian and are 18 or older.</summary>
        public bool DeclaresLegalGuardian { get; set; }

        [MaxLength(150)]
        public string? GuardianName { get; set; }

        [MaxLength(50)]
        public string? Relationship { get; set; }

        /// <summary>InClinic: document type checked by staff (Aadhaar, PAN, Passport, ...). Never the number.</summary>
        [MaxLength(30)]
        public string? IdProofType { get; set; }

        /// <summary>Otp: a verified challenge with Action GuardianConsent, EntityType Patient, EntityId = patient id.</summary>
        public long? OtpChallengeId { get; set; }
    }

    public class PrivacyGrantRequest
    {
        [MaxLength(20)]
        public string? NoticeVersion { get; set; }

        [MaxLength(10)]
        public string? NoticeLanguage { get; set; }
    }

    public class ConsentNoticePublishRequest
    {
        [Required]
        [MaxLength(50)]
        public string ConsentTypeCode { get; set; } = null!;

        [Required]
        [RegularExpression(@"^[0-9A-Za-z.\-]{1,20}$")]
        public string Version { get; set; } = null!;

        [RegularExpression(@"^[a-z]{2,3}(-[A-Za-z]{2})?$")]
        public string Language { get; set; } = "en";

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        [MaxLength(20000)]
        public string Body { get; set; } = null!;

        /// <summary>Consents given under earlier versions must be given again.</summary>
        public bool RequiresReconsent { get; set; }
    }

    public class ConsentWithdrawRequest
    {
        [Required]
        public long ConsentRecordId { get; set; }
    }

    public class RequestOtpModel
    {
        [Required]
        public string Action { get; set; } = null!;

        [Required]
        public string EntityType { get; set; } = null!;

        [Required]
        public string EntityId { get; set; } = null!;

        /// <summary>Phone or email destination (will be masked in audit).</summary>
        [Required]
        public string Destination { get; set; } = null!;
    }

    public class VerifyOtpModel
    {
        [Required]
        public long OtpChallengeId { get; set; }

        [Required]
        public string Code { get; set; } = null!;
    }
}
