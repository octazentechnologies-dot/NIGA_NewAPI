using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    public class FamilyMemberCreateRequest
    {
        /// <summary>Ignored for patient JWT — owner is resolved from login. Admin may pass it.</summary>
        public int? OwnerPatientId { get; set; }

        public int? RelationId { get; set; }

        [MaxLength(50)]
        public string? Relation { get; set; }

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
        public int? RelationId { get; set; }

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
        public int? RelationId { get; set; }
        public string Relation { get; set; } = null!;
        public string? PatientName { get; set; }
        public string? MobileNo { get; set; }
        public string? Email { get; set; }
    }

    public class FamilyRelationDto
    {
        public int RelationId { get; set; }
        public string RelationName { get; set; } = null!;
        public int SortOrder { get; set; }
    }

    public class FamilyRelationCreateRequest
    {
        [Required]
        [MaxLength(50)]
        public string RelationName { get; set; } = null!;
    }

    public class FamilyOwnerDto
    {
        public int OwnerPatientId { get; set; }
        public string? OwnerPatientName { get; set; }
        public bool Linked { get; set; }
        public bool IsActingAsCaregiver { get; set; }
    }

    public class CaregiverGrantRequest
    {
        /// <summary>Ignored for patient JWT — owner is resolved from login. Admin may pass it.</summary>
        public int? PatientId { get; set; }

        /// <summary>Optional when CaregiverContact (mobile/email) is sent.</summary>
        public long? CaregiverUserId { get; set; }

        /// <summary>Caregiver login mobile or email. Preferred over CaregiverUserId in the UI.</summary>
        [MaxLength(200)]
        public string? CaregiverContact { get; set; }

        [MaxLength(100)]
        public string Scope { get; set; } = "booking";

        /// <summary>CON-02.04 — OTP challenge from POST /api/Otp/RequestOtp (Action=GrantCaregiver). AdminPortal may omit.</summary>
        public long OtpChallengeId { get; set; }

        /// <summary>CON-02.04 — 6-digit code. AdminPortal may omit.</summary>
        [MaxLength(12)]
        public string? OtpCode { get; set; }
    }

    public class CaregiverMeDto
    {
        public int OwnerPatientId { get; set; }
        public string? OwnerPatientName { get; set; }
        public string? OtpDestination { get; set; }
        public bool Linked { get; set; }
        public bool IsActingAsCaregiver { get; set; }
    }

    public class CaregiverLookupDto
    {
        public long CaregiverUserId { get; set; }
        public string? DisplayName { get; set; }
        public string? Contact { get; set; }
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
        public string? PatientName { get; set; }
        public long CaregiverUserId { get; set; }
        public string? CaregiverName { get; set; }
        public string? CaregiverContact { get; set; }
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

    public class FamilyBookAsRequest
    {
        [Required]
        public int MemberPatientId { get; set; }

        [Required]
        public int DoctorId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        /// <summary>HH:mm</summary>
        [Required]
        [MaxLength(8)]
        public string AppointmentTime { get; set; } = null!;

        [MaxLength(50)]
        public string? VisitType { get; set; }

        [MaxLength(50)]
        public string? ConsultMode { get; set; }

        public bool IsTele { get; set; }
    }
}
