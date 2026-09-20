using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    public class LoginWithOtpRequest
    {
        [Required]
        public long OtpChallengeId { get; set; }

        [Required]
        [MaxLength(12)]
        public string Code { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string MobileNo { get; set; } = null!;
    }

    public class ConfirmMobileRequest
    {
        [Required]
        [MaxLength(20)]
        public string MobileNo { get; set; } = null!;
    }

    public class PatientProfileUpdateRequest
    {
        [MaxLength(200)]
        public string? PatientName { get; set; }

        public DateTime? DateOfBirth { get; set; }
        public int? Gender { get; set; }
        public int? Age { get; set; }

        [MaxLength(20)]
        public string? MobileNo { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        public int? PreferredLanguageId { get; set; }

        [MaxLength(20)]
        public string? WelcomeVersionSeen { get; set; }

        /// <summary>Optional existing PatientId to link as primary (same as Family/LinkPrimary).</summary>
        public int? PatientId { get; set; }
    }

    public class DeviceRegisterRequest
    {
        [Required]
        [MaxLength(20)]
        public string Platform { get; set; } = null!;

        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = null!;

        [MaxLength(100)]
        public string? DeviceId { get; set; }
    }

    public class DeviceUnregisterRequest
    {
        [MaxLength(512)]
        public string? Token { get; set; }

        public long? DevicePushTokenId { get; set; }
    }

    public class SecureFileSignRequest
    {
        [Required]
        public string Root { get; set; } = null!;

        [Required]
        public string Path { get; set; } = null!;

        public int TtlMinutes { get; set; } = 15;
    }
}
