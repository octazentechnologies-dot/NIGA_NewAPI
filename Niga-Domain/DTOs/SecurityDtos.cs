using System.ComponentModel.DataAnnotations;

namespace Niga_Domain.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
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
