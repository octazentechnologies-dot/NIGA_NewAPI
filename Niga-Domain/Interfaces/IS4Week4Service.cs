using Microsoft.AspNetCore.Http;
using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces;

public interface IS4Week4Service
{
    Task<bool> PayAtClinicEnabledAsync(int doctorId);
    Task<ConsultFeeQuote> ResolveConsultFeesAsync(int doctorId);
    Task<S4ActionResult> GetPublicFeeAsync(int doctorId);
    Task<S4ActionResult> UpsertFeeAsync(FeeUpsertRequest request, S4Caller caller);
    Task<S4ActionResult> FeeHistoryAsync(int doctorId, S4Caller caller);

    Task<S4ActionResult> CreateConsultOrderAsync(CreateConsultOrderRequest request, S4Caller caller);
    Task<S4ActionResult> VerifyPaymentAsync(VerifyPaymentRequest request, S4Caller caller);
    Task<S4ActionResult> AppointmentPaymentAsync(int patientAppId, S4Caller caller);
    Task<S4ActionResult> HandleWebhookAsync(string rawBody, string? signature, string? eventId);
    Task<S4ActionResult> CollectAtReceptionAsync(CollectAtReceptionRequest request, S4Caller caller);
    Task<S4ActionResult> CreateMedicinePaymentAsync(CreateMedicinePaymentRequest request, S4Caller caller);
    Task<S4ActionResult> PatientPaymentsAsync(S4Caller caller);

    Task<S4ActionResult> PreviewRefundPolicyAsync(long paymentOrderId, S4Caller caller);
    Task<S4ActionResult> CreateRefundAsync(CreateRefundRequest request, S4Caller caller);
    Task<S4ActionResult> ListRefundsAsync(S4Caller caller);
    Task<S4ActionResult> GetOrCreateInvoiceAsync(long paymentOrderId, S4Caller caller, bool create);

    Task<S4ActionResult> LedgerAsync(string? stream, DateTime? from, DateTime? to, int? doctorId, int page, S4Caller caller);
    Task<S4ActionResult> LedgerExportAsync(string? stream, DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> ReconciliationAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> MedicineLedgerAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> CreateSettlementAsync(SettlementCreateRequest request, S4Caller caller);
    Task<S4ActionResult> ListSettlementsAsync(S4Caller caller);
    Task<S4ActionResult> SettlementDetailAsync(long id, S4Caller caller);
    Task<S4ActionResult> RequestPayoutOtpAsync(long payoutId, S4Caller caller);
    Task<S4ActionResult> ApprovePayoutAsync(long payoutId, PayoutDecisionRequest request, S4Caller caller);
    Task<S4ActionResult> RejectPayoutAsync(long payoutId, PayoutDecisionRequest request, S4Caller caller);
    Task<S4ActionResult> ListExceptionsAsync(string? status, S4Caller caller);
    Task<S4ActionResult> ExceptionDetailAsync(long id, S4Caller caller);
    Task<S4ActionResult> RetryExceptionAsync(long id, S4Caller caller);
    Task<S4ActionResult> ResolveExceptionAsync(long id, ExceptionResolveRequest request, S4Caller caller);
    Task<S4ActionResult> TaxReportAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> ListPayeesAsync(S4Caller caller);
    Task<S4ActionResult> UpdatePayeeAsync(int payeeId, PayeeUpdateRequest request, S4Caller caller);
    Task<S4ActionResult> RequestPayeeBankOtpAsync(int payeeId, S4Caller caller);
    Task<S4ActionResult> ClinicCollectionsAsync(DateTime? from, DateTime? to, int? doctorId, S4Caller caller);
    Task<S4ActionResult> TrailAsync(int? patientAppId, long? paymentOrderId, int? doctorId, S4Caller caller);

    Task<S4ActionResult> MyVerificationAsync(S4Caller caller);
    Task<S4ActionResult> VerificationQueueAsync(string? status, S4Caller caller);
    Task<S4ActionResult> VerificationDetailAsync(int doctorId, S4Caller caller);
    Task<S4ActionResult> DecideVerificationAsync(int doctorId, VerificationDecisionRequest request, S4Caller caller);
    Task<S4ActionResult> CreateReviewAsync(ReviewCreateRequest request, S4Caller caller);
    Task<S4ActionResult> ListPublicReviewsAsync(int doctorId);
    Task<S4ActionResult> ListMyReviewsAsync(S4Caller caller);
    Task<S4ActionResult> AppealReviewAsync(int reviewId, ReviewAppealRequest request, S4Caller caller);
    Task<S4ActionResult> ListAppealsAsync(S4Caller caller);
    Task<S4ActionResult> ResolveAppealAsync(int appealId, AppealResolveRequest request, S4Caller caller);
    Task<S4ActionResult> RankingExplainAsync(int doctorId);

    Task<S4ActionResult> ListPotenciesAsync();
    Task<S4ActionResult> UpdateRemedyLineAsync(int prescriptionRemedyId, RemedyLineUpdateRequest request, S4Caller caller);
    Task<S4ActionResult> GetErxByAppointmentAsync(int patientAppId, S4Caller caller, bool patientView);
    Task<S4ActionResult> SignErxAsync(SignErxRequest request, S4Caller caller);
    Task<S4ActionResult> ErxHistoryAsync(int? patientId, int? patientAppId, S4Caller caller);
    Task<S4ActionResult> ErxPdfAsync(int erxId, S4Caller caller);
    Task<S4ActionResult> RequestRefillAsync(RefillCreateRequest request, S4Caller caller);
    Task<S4ActionResult> ListRefillsAsync(S4Caller caller);
    Task<S4ActionResult> DecideRefillAsync(int refillId, bool approve, string? reason, S4Caller caller);

    Task<S4ActionResult> OnboardPharmacyAsync(PharmacyOnboardRequest request, S4Caller caller);
    Task<S4ActionResult> ActivatePharmacyAsync(int pharmacyId, S4Caller caller);
    Task<S4ActionResult> SweepLicencesAsync(S4Caller caller);
    Task<S4ActionResult> ListSellersAsync(string? area);
    Task<S4ActionResult> CreateMedicineOrderAsync(MedicineOrderCreateRequest request, S4Caller caller);
    Task<S4ActionResult> GrantMedicineConsentAsync(int orderId, S4Caller caller);
    Task<S4ActionResult> RequestMedicineAcceptOtpAsync(int orderId, S4Caller caller);
    Task<S4ActionResult> AcceptMedicineOrderAsync(PharmacyAcceptRequest request, S4Caller caller);
    Task<S4ActionResult> RejectMedicineOrderAsync(int orderId, MedicineRejectRequest request, S4Caller caller);
    Task<S4ActionResult> QuoteMedicineOrderAsync(int orderId, MedicineQuoteRequest request, S4Caller caller);
    Task<S4ActionResult> AcceptQuoteAsync(int orderId, S4Caller caller);
    Task<S4ActionResult> MarkMedicineReadyAsync(int orderId, S4Caller caller);
    Task<S4ActionResult> DispatchMedicineAsync(int orderId, S4Caller caller);
    Task<S4ActionResult> MedicineTrackingAsync(int orderId, S4Caller caller);
    Task<S4ActionResult> PatientMedicineOrdersAsync(S4Caller caller);
    Task<S4ActionResult> CloneRefillOrderAsync(int refillId, S4Caller caller);
    Task<S4ActionResult> ListMedicineExceptionsAsync(S4Caller caller);
    Task<S4ActionResult> RerouteMedicineAsync(int orderId, int pharmacyId, S4Caller caller);
    Task<S4ActionResult> SaveRoutingAsync(RoutingRuleRequest request, S4Caller caller);

    Task<S4ActionResult> TimelineAsync(int? patientId, S4Caller caller);
    Task<S4ActionResult> ConsultationNoteAsync(int patientAppId, S4Caller caller);
    Task<S4ActionResult> SavePatientDocumentAsync(IFormFile file, S4Caller caller);
    Task<S4ActionResult> SetFollowUpAsync(FollowUpCreateRequest request, S4Caller caller);
    Task<S4ActionResult> ListFollowUpsAsync(int? patientId, S4Caller caller);
    Task<S4ActionResult> CompleteFollowUpAsync(int taskId, S4Caller caller);
    Task<S4ActionResult> SaveDiaryAsync(DiaryWriteRequest request, S4Caller caller);
    Task<S4ActionResult> ListDiaryAsync(int? patientId, S4Caller caller);
    Task<S4ActionResult> UpdateDiaryAsync(int diaryId, DiaryWriteRequest request, S4Caller caller);
    Task<S4ActionResult> DeleteDiaryAsync(int diaryId, S4Caller caller);
    Task<S4ActionResult> ProgressAsync(int? patientId, S4Caller caller);
    Task<S4ActionResult> ListConsentsAsync(S4Caller caller);
    Task<S4ActionResult> WithdrawConsentAsync(long consentId, S4Caller caller);
    Task<S4ActionResult> CreateDataRequestAsync(DataRequestCreate request, S4Caller caller);
    Task<S4ActionResult> GetHealthProfileAsync(S4Caller caller);
    Task<S4ActionResult> UpdateHealthProfileAsync(HealthProfileUpdate request, S4Caller caller);
}

public class S4Caller
{
    public long UserId { get; set; }
    public int? DoctorId { get; set; }
    public int? PatientId { get; set; }
    public string Role { get; set; } = "";
    public bool IsAdmin { get; set; }

    public bool IsAccount => IsAdmin || Eq("Account");
    public bool IsDoctor => Eq("Doctor");
    public bool IsReception => Eq("Reception");
    public bool IsPatient => Eq("Patient");
    public bool IsPharmacy => Eq("PharmacyPartner");

    public bool Eq(string role) => Role.Equals(role, StringComparison.OrdinalIgnoreCase);

    public bool OwnsDoctor(int doctorId) => IsAdmin || (DoctorId.HasValue && DoctorId.Value == doctorId);
}

public class S4ActionResult
{
    public int StatusCode { get; set; }
    public object? Body { get; set; }

    public static S4ActionResult Ok(object body) => new() { StatusCode = 200, Body = body };

    public static S4ActionResult Fail(int status, string code, string message)
        => new() { StatusCode = status, Body = new { success = false, code, message } };
}
