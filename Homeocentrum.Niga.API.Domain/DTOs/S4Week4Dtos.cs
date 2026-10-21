using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Homeocentrum.Niga.API.Domain.DTOs;

public class ConsultFeeQuote
{
    public decimal InClinicFee { get; set; }
    public decimal TeleFee { get; set; }
    /// <summary>ConsultFeeConfig.Currency, else Doctor.FeeCurrency. Null when neither is stored.</summary>
    public string? Currency { get; set; }
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

    [StringLength(64)]
    public string? BookingToken { get; set; }

    /// <summary>Ignored as the charge source. Rejected when it does not match the configured fee.</summary>
    public decimal? Amount { get; set; }
}

public class VerifyPaymentRequest
{
    [Range(1, long.MaxValue)]
    public long PaymentOrderId { get; set; }

    [StringLength(80)]
    public string? GatewayPaymentId { get; set; }

    [JsonPropertyName("razorpay_order_id")]
    [StringLength(80)]
    public string? RazorpayOrderId { get; set; }

    [JsonPropertyName("razorpay_payment_id")]
    [StringLength(80)]
    public string? RazorpayPaymentId { get; set; }

    [JsonPropertyName("razorpay_signature")]
    [StringLength(200)]
    public string? RazorpaySignature { get; set; }
}

/// <summary>PAT — pay anytime / pay later after hold or pay-at-clinic. Mobile sends Razorpay Checkout ids.</summary>
public class UpdateBookingPaymentRequest
{
    [Range(1, long.MaxValue)]
    public long UserId { get; set; }

    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }

    [Required]
    [JsonPropertyName("razorpay_order_id")]
    [StringLength(80, MinimumLength = 3)]
    public string RazorpayOrderId { get; set; } = "";

    [Required]
    [JsonPropertyName("razorpay_payment_id")]
    [StringLength(80, MinimumLength = 3)]
    public string RazorpayPaymentId { get; set; } = "";

    [Range(1, long.MaxValue)]
    public long? PaymentOrderId { get; set; }
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
    /// <summary>Optional. Omit or null when the doctor does not set a potency. Existing potency is left as-is when omitted.</summary>
    public int? PotencyId { get; set; }

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

/// <summary>Draft prescription. Potency on each line is optional. An empty items list is rejected.</summary>
public class ErxDraftRequest
{
    [Range(1, int.MaxValue)]
    public int PatientAppId { get; set; }

    /// <summary>Free-text note stored on the draft. Null when omitted.</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<ErxDraftItem> Items { get; set; } = new();
}

public class ErxDraftItem
{
    /// <summary>RemedyMaster.RemedyId. The id must exist.</summary>
    [Range(1, int.MaxValue)]
    public int RemedyId { get; set; }

    /// <summary>Optional. Leave empty when the doctor does not set a potency.</summary>
    [StringLength(40)]
    public string? PotencyCode { get; set; }

    [StringLength(80)]
    public string? Dose { get; set; }

    [StringLength(80)]
    public string? Frequency { get; set; }

    [StringLength(80)]
    public string? Duration { get; set; }

    [StringLength(500)]
    public string? Instructions { get; set; }
}

public class PharmacyAcceptRequest
{
    /// <summary>Taken from the route; a body value is ignored.</summary>
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

    /// <summary>Optional delivery address from GET /api/Patient/Addresses (B-023).</summary>
    public int? AddressId { get; set; }
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
