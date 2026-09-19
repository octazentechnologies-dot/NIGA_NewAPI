using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    public class FamilyMemberCreateRequest
    {
        /// <summary>Primary/self PatientId for the logged-in patient account. Required on first family use.</summary>
        [Required]
        public int OwnerPatientId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Relation { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string PatientName { get; set; } = null!;

        public string? MobileNo { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public int? Age { get; set; }

        /// <summary>Optional: link an existing PatientId instead of creating a new row.</summary>
        public int? ExistingMemberPatientId { get; set; }
    }

    public class FamilyMemberUpdateRequest
    {
        [MaxLength(50)]
        public string? Relation { get; set; }

        [MaxLength(200)]
        public string? PatientName { get; set; }

        public string? MobileNo { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public int? Age { get; set; }
    }

    public class FamilyMemberDto
    {
        public long FamilyMemberId { get; set; }
        public int OwnerPatientId { get; set; }
        public int MemberPatientId { get; set; }
        public string Relation { get; set; } = null!;
        public string? PatientName { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }
    }

    public class CaregiverGrantRequest
    {
        [Required]
        public int PatientId { get; set; }

        [Required]
        public long CaregiverUserId { get; set; }

        [MaxLength(100)]
        public string Scope { get; set; } = "booking";

        /// <summary>CON-02.04 — OTP challenge from POST /api/Otp/RequestOtp (Action=GrantCaregiver). AdminPortal may omit.</summary>
        public long OtpChallengeId { get; set; }

        /// <summary>CON-02.04 — 6-digit code. AdminPortal may omit.</summary>
        [MaxLength(12)]
        public string? OtpCode { get; set; }
    }

    public class CaregiverRevokeRequest
    {
        [Required]
        public long CaregiverAuthorizationId { get; set; }
    }

    public class CaregiverAuthorizationDto
    {
        public long CaregiverAuthorizationId { get; set; }
        public int PatientId { get; set; }
        public long CaregiverUserId { get; set; }
        public DateTime GrantedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string Scope { get; set; } = "booking";
        public bool IsActive { get; set; }
    }

    public class LinkPrimaryPatientRequest
    {
        [Required]
        public int PatientId { get; set; }
    }
}
