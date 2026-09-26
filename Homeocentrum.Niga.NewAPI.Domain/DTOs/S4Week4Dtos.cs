using System.ComponentModel.DataAnnotations;

namespace Homeocentrum.Niga.NewAPI.Domain.DTOs;

public class ConsultFeeQuote
{
    public decimal InClinicFee { get; set; }
    public decimal TeleFee { get; set; }
}

public class FeeUpsertRequest
{
    [Range(1, int.MaxValue)]
    public int DoctorId { get; set; }

    [Range(0, 100000)]
    public decimal InClinicFee { get; set; }

    [Range(0, 100000)]
    public decimal TeleFee { get; set; }

    [Range(0, 100000)]
    public decimal InstantSurcharge { get; set; }

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = "INR";

    public bool PayAtClinicEnabled { get; set; } = true;

    [Required]
    public DateTime? EffectiveFrom { get; set; }
}

public class CreateConsultOrderRequest
{
    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }

    [StringLength(80)]
    public string? IdempotencyKey { get; set; }

    public bool PayAtClinic { get; set; }

    /// <summary>Ignored as the charge source. Rejected when it does not match the configured fee.</summary>
    public decimal? Amount { get; set; }
}

public class VerifyPaymentRequest
{
    [Range(1, long.MaxValue)]
    public long PaymentOrderId { get; set; }

    [StringLength(80)]
    public string? GatewayPaymentId { get; set; }
}

public class CollectAtReceptionRequest
{
    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }

    [Required]
    public string Method { get; set; } = "";

    public decimal? Amount { get; set; }
}

public class CreateMedicinePaymentRequest
{
    [Range(1, int.MaxValue)]
    public int MedicineOrderId { get; set; }

    [Required]
    public string PayMode { get; set; } = "";

    [StringLength(80)]
    public string? IdempotencyKey { get; set; }
}

public class CreateRefundRequest
{
    [Range(1, long.MaxValue)]
    public long PaymentOrderId { get; set; }

    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Reason { get; set; } = "";

    public decimal? Amount { get; set; }
}

public class SettlementCreateRequest
{
    public bool DryRun { get; set; } = true;
    public bool Confirm { get; set; }
}

public class PayoutDecisionRequest
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = "";

    [StringLength(500)]
    public string? Reason { get; set; }
}

public class ExceptionResolveRequest
{
    [Required]
    [StringLength(500, MinimumLength = 3)]
    public string Note { get; set; } = "";
}

public class PayeeUpdateRequest
{
    [StringLength(200)]
    public string? AccountName { get; set; }

    [StringLength(40)]
    public string? BankAccount { get; set; }

    [StringLength(20)]
    public string? Ifsc { get; set; }

    [StringLength(20)]
    public string? Pan { get; set; }

    [StringLength(6)]
    public string? Otp { get; set; }
}

public class VerificationDecisionRequest
{
    [Required]
    public string Decision { get; set; } = "";

    [StringLength(500)]
    public string? Note { get; set; }
}

public class ReviewCreateRequest
{
    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Text { get; set; }
}

public class ReviewAppealRequest
{
    [Required]
    [StringLength(1000, MinimumLength = 3)]
    public string Reason { get; set; } = "";
}

public class AppealResolveRequest
{
    [Required]
    public string Decision { get; set; } = "";

    [StringLength(500)]
    public string? Note { get; set; }
}

public class RemedyLineUpdateRequest
{
    [Range(1, int.MaxValue)]
    public int PotencyId { get; set; }

    [StringLength(80)]
    public string? Frequency { get; set; }

    [StringLength(80)]
    public string? Duration { get; set; }

    [StringLength(500)]
    public string? Instructions { get; set; }
}

public class SignErxRequest
{
    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }
}

public class PharmacyAcceptRequest
{
    [Range(1, int.MaxValue)]
    public int MedicineOrderId { get; set; }

    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Otp { get; set; } = "";
}

public class RefillCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ErxSnapshotId { get; set; }
}

public class RefillDecisionRequest
{
    [StringLength(500)]
    public string? Reason { get; set; }
}

public class PharmacyOnboardRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(20, MinimumLength = 8)]
    public string Mobile { get; set; } = "";

    [Required]
    [StringLength(80, MinimumLength = 3)]
    public string LicenceNumber { get; set; } = "";

    [Required]
    public DateTime? ExpiryDate { get; set; }

    [StringLength(120)]
    public string? Area { get; set; }
}

public class MedicineOrderCreateRequest
{
    [Range(1, int.MaxValue)]
    public int ErxSnapshotId { get; set; }

    public int? PharmacyPartnerId { get; set; }
}

public class MedicineRejectRequest
{
    [Required]
    public string Reason { get; set; } = "";
}

public class MedicineQuoteRequest
{
    [Range(0.01, 1000000)]
    public decimal Amount { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public class RoutingRuleRequest
{
    [Range(1, int.MaxValue)]
    public int PharmacyPartnerId { get; set; }

    [StringLength(120)]
    public string? Area { get; set; }

    public string? OpenTime { get; set; }
    public string? CloseTime { get; set; }

    [Range(1, 500)]
    public int Capacity { get; set; } = 20;
}

public class FollowUpCreateRequest
{
    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Title { get; set; } = "";

    [Required]
    public DateTime? DueDate { get; set; }
}

public class DiaryWriteRequest
{
    [Required]
    public DateTime? EntryDate { get; set; }

    [Range(0, 10)]
    public int Severity { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }
}

public class DataRequestCreate
{
    [Required]
    public string RequestType { get; set; } = "";
}

public class HealthProfileUpdate
{
    [StringLength(8)]
    public string? BloodGroup { get; set; }

    [StringLength(500)]
    public string? Allergies { get; set; }

    [StringLength(500)]
    public string? ChronicConditions { get; set; }

    [StringLength(120)]
    public string? EmergencyContactName { get; set; }

    [StringLength(20)]
    public string? EmergencyContactMobile { get; set; }
}
