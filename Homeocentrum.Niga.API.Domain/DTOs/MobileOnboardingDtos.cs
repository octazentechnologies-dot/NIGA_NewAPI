using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.API.Domain.DTOs
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
        [MaxLength(100)]
        public string? FirstName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

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

        public int? CountryId { get; set; }
        public int? StateId { get; set; }
        public int? DistrictId { get; set; }
        public int? CityId { get; set; }
        public int? PinCodeId { get; set; }
    }

    public class DeviceRegisterRequest
    {
        /// <summary>FCM token from the web or mobile client.</summary>
        [Required]
        [MaxLength(512)]
        public string Token { get; set; } = null!;

        /// <summary>Website | Android | iOS (also accepts web, mobile android, mobile ios).</summary>
        [MaxLength(40)]
        public string? DeviceType { get; set; }

        /// <summary>Optional. Server uses the JWT user id; body UserId must match when sent.</summary>
        public long? UserId { get; set; }

        /// <summary>Legacy: FCM / APNs. Ignored when DeviceType is set.</summary>
        [MaxLength(20)]
        public string? Platform { get; set; }

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
