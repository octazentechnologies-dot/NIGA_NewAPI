namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

public partial class S4Week4Service
{
    private sealed class FeeRow
    {
        public int ConsultFeeConfigId { get; set; }
        public int DoctorId { get; set; }
        public decimal InClinicFee { get; set; }
        public decimal TeleFee { get; set; }
        public decimal InstantSurcharge { get; set; }
        public string Currency { get; set; } = "";
        public bool PayAtClinicEnabled { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class OrderRow
    {
        public long PaymentOrderId { get; set; }
        public string Stream { get; set; } = "";
        public int? PatientAppId { get; set; }
        public int? MedicineOrderId { get; set; }
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Method { get; set; }
        public string? GatewayOrderId { get; set; }
        public string? GatewayPaymentId { get; set; }
        public string? IdempotencyKey { get; set; }
        public string CorrelationId { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    private sealed class CountRow
    {
        public int Value { get; set; }
    }

    private sealed class MoneyRow
    {
        public decimal Value { get; set; }
    }

    private sealed class RefundRow
    {
        public long RefundId { get; set; }
        public long PaymentOrderId { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; } = "";
        public string Policy { get; set; } = "";
        public string Status { get; set; } = "";
        public string? GatewayRefundId { get; set; }
        public long ByUserId { get; set; }
        public DateTime At { get; set; }
    }

    private sealed class InvoiceRow
    {
        public long InvoiceId { get; set; }
        public string Number { get; set; } = "";
        public string Series { get; set; } = "";
        public string Stream { get; set; } = "";
        public long PaymentOrderId { get; set; }
        public string? GstBreakup { get; set; }
        public string? PdfPath { get; set; }
        public long? SecureDocumentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class TaxRow
    {
        public decimal GstRate { get; set; }
        public bool TreatmentExempt { get; set; }
    }

    private sealed class LedgerRow
    {
        public long LedgerEntryId { get; set; }
        public string Stream { get; set; } = "";
        public string Direction { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Gst { get; set; }
        public string EntityType { get; set; } = "";
        public string EntityId { get; set; } = "";
        public long? PaymentOrderId { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime At { get; set; }
    }

    private sealed class SettlementRow
    {
        public long SettlementRunId { get; set; }
        public string Status { get; set; } = "";
        public long CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class SettlementLineRow
    {
        public long SettlementLineId { get; set; }
        public long SettlementRunId { get; set; }
        public string PayeeType { get; set; } = "";
        public int PayeeId { get; set; }
        public decimal Amount { get; set; }
        public long? LedgerEntryId { get; set; }
    }

    private sealed class PayoutRow
    {
        public long PayoutId { get; set; }
        public string PayeeType { get; set; } = "";
        public int PayeeId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public long? SettlementRunId { get; set; }
    }

    private sealed class ExceptionRow
    {
        public long PaymentExceptionId { get; set; }
        public long? PaymentOrderId { get; set; }
        public string Kind { get; set; } = "";
        public string Detail { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    private sealed class TaxConfigRow
    {
        public int TaxConfigId { get; set; }
        public decimal GstRate { get; set; }
        public bool TreatmentExempt { get; set; }
    }

    private sealed class PayeeRow
    {
        public int PayeeId { get; set; }
        public string PayeeType { get; set; } = "";
        public int? DoctorId { get; set; }
        public int? PharmacyId { get; set; }
        public string? AccountName { get; set; }
        public string? BankAccount { get; set; }
        public string? Ifsc { get; set; }
        public string? Pan { get; set; }
        public string KycStatus { get; set; } = "";
    }

    private sealed class MedicineRow
    {
        public int MedicineOrderId { get; set; }
        public int ErxSnapshotId { get; set; }
        public int PatientId { get; set; }
        public int? PharmacyPartnerId { get; set; }
        public string Status { get; set; } = "";
        public bool ConsentGranted { get; set; }
        public decimal? QuoteAmount { get; set; }
        public string? PayMode { get; set; }
    }

    private sealed class OpenLedgerRow
    {
        public long LedgerEntryId { get; set; }
        public decimal Amount { get; set; }
        public string EntityType { get; set; } = "";
        public string EntityId { get; set; } = "";
    }
}
