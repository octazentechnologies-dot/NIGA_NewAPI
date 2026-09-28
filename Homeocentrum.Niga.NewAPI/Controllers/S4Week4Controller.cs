using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers;

/// <summary>
/// S4 Week 4 HTTP: fees, payments, ledger, trust, eRx, medicine orders, and patient records.
/// Razorpay keys stay in configuration and are never returned except the public key id when checkout is ready.
/// Appointment paid status changes only from a verified webhook or reception collection.
/// </summary>
[ApiController]
[Authorize]
public class S4Week4Controller : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<S4Week4Controller> _logger;

    public S4Week4Controller(IS4Week4Service s4, NIGACentrumContext context, ILogger<S4Week4Controller> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    /// <summary>PAT-18.02 — public consult fee for patient checkout (anonymous).</summary>
    [AllowAnonymous]
    [HttpGet("/api/Fees/Public/{doctorId:int}")]
    public Task<IActionResult> PublicFee(int doctorId) => Done(_s4.GetPublicFeeAsync(doctorId));

    [HttpPut("/api/Fees")]
    public Task<IActionResult> SaveFee([FromBody] FeeUpsertRequest request) => Done(_s4.UpsertFeeAsync(request, Caller()));

    [HttpGet("/api/Fees/History")]
    public Task<IActionResult> FeeHistory([FromQuery] int doctorId) => Done(_s4.FeeHistoryAsync(doctorId, Caller()));

    /// <summary>
    /// PAT-18.02 — patient/reception consult checkout order (pay at clinic or gateway).
    /// Does not accept client PaymentStatus=PAID; gateway/webhook/reception collection sets paid.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("/api/Payments/ConsultOrders")]
    public Task<IActionResult> ConsultOrder([FromBody] CreateConsultOrderRequest request) => Done(_s4.CreateConsultOrderAsync(request, Caller()));

    [HttpPost("/api/Payments/Verify")]
    public Task<IActionResult> Verify([FromBody] VerifyPaymentRequest request) => Done(_s4.VerifyPaymentAsync(request, Caller()));

    /// <summary>PAT-18.02 / PAT-19 — poll appointment payment state for checkout UI.</summary>
    [HttpGet("/api/Payments/Appointments/{patientAppId:int}")]
    public Task<IActionResult> AppointmentPayment(int patientAppId) => Done(_s4.AppointmentPaymentAsync(patientAppId, Caller()));

    [AllowAnonymous]
    [HttpPost("/api/Payments/Webhook")]
    public async Task<IActionResult> Webhook()
    {
        string raw;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            raw = await reader.ReadToEndAsync();
        var signature = Request.Headers["X-Razorpay-Signature"].ToString();
        var eventId = Request.Headers["X-Razorpay-Event-Id"].ToString();
        return await Done(_s4.HandleWebhookAsync(raw, signature, eventId));
    }

    [HttpPost("/api/Payments/CollectAtReception")]
    public Task<IActionResult> Collect([FromBody] CollectAtReceptionRequest request) => Done(_s4.CollectAtReceptionAsync(request, Caller()));

    [HttpPost("/api/Payments/MedicineOrders")]
    public Task<IActionResult> MedicinePay([FromBody] CreateMedicinePaymentRequest request) => Done(_s4.CreateMedicinePaymentAsync(request, Caller()));

    [HttpGet("/api/Patient/Payments")]
    public Task<IActionResult> PatientPayments() => Done(_s4.PatientPaymentsAsync(Caller()));

    [HttpGet("/api/Refunds/Policy/{paymentOrderId:long}")]
    public Task<IActionResult> Policy(long paymentOrderId) => Done(_s4.PreviewRefundPolicyAsync(paymentOrderId, Caller()));

    [HttpPost("/api/Refunds")]
    public Task<IActionResult> Refund([FromBody] CreateRefundRequest request) => Done(_s4.CreateRefundAsync(request, Caller()));

    [HttpGet("/api/Refunds")]
    [HttpGet("/api/Account/Refunds")]
    public Task<IActionResult> Refunds() => Done(_s4.ListRefundsAsync(Caller()));

    [HttpGet("/api/Invoices/ByPayment/{paymentOrderId:long}")]
    public Task<IActionResult> Invoice(long paymentOrderId) => Done(_s4.GetOrCreateInvoiceAsync(paymentOrderId, Caller(), false));

    [HttpPost("/api/Invoices/ByPayment/{paymentOrderId:long}")]
    public Task<IActionResult> CreateInvoice(long paymentOrderId) => Done(_s4.GetOrCreateInvoiceAsync(paymentOrderId, Caller(), true));

    [HttpGet("/api/Account/Ledger")]
    public Task<IActionResult> Ledger([FromQuery] string? stream, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId, [FromQuery] int page = 1)
        => Done(_s4.LedgerAsync(stream, from, to, doctorId, page, Caller()));

    [HttpGet("/api/Account/Ledger/Export")]
    public Task<IActionResult> LedgerExport([FromQuery] string? stream, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s4.LedgerExportAsync(stream, from, to, Caller()));

    [HttpGet("/api/Account/Reconciliation")]
    public Task<IActionResult> Reconciliation([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s4.ReconciliationAsync(from, to, Caller()));

    [HttpGet("/api/Account/MedicineLedger")]
    public Task<IActionResult> MedicineLedger([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s4.MedicineLedgerAsync(from, to, Caller()));

    [HttpPost("/api/Account/Settlements")]
    public Task<IActionResult> Settle([FromBody] SettlementCreateRequest request) => Done(_s4.CreateSettlementAsync(request, Caller()));

    [HttpGet("/api/Account/Settlements")]
    public Task<IActionResult> Settlements() => Done(_s4.ListSettlementsAsync(Caller()));

    [HttpGet("/api/Account/Settlements/{id:long}")]
    public Task<IActionResult> Settlement(long id) => Done(_s4.SettlementDetailAsync(id, Caller()));

    [HttpGet("/api/Account/Payouts")]
    public Task<IActionResult> Payouts() => Done(_s4.ListPayoutsAsync(Caller()));

    [HttpPost("/api/Account/Payouts/{id:long}/Otp")]
    public Task<IActionResult> PayoutOtp(long id) => Done(_s4.RequestPayoutOtpAsync(id, Caller()));

    [HttpPost("/api/Account/Payouts/{id:long}/Approve")]
    public Task<IActionResult> ApprovePayout(long id, [FromBody] PayoutDecisionRequest request) => Done(_s4.ApprovePayoutAsync(id, request, Caller()));

    [HttpPost("/api/Account/Payouts/{id:long}/Reject")]
    public Task<IActionResult> RejectPayout(long id, [FromBody] PayoutDecisionRequest request) => Done(_s4.RejectPayoutAsync(id, request, Caller()));

    [HttpGet("/api/Account/Exceptions")]
    public Task<IActionResult> Exceptions([FromQuery] string? status) => Done(_s4.ListExceptionsAsync(status, Caller()));

    [HttpGet("/api/Account/Exceptions/{id:long}")]
    public Task<IActionResult> ExceptionDetail(long id) => Done(_s4.ExceptionDetailAsync(id, Caller()));

    [HttpPost("/api/Account/Exceptions/{id:long}/Retry")]
    public Task<IActionResult> Retry(long id) => Done(_s4.RetryExceptionAsync(id, Caller()));

    [HttpPost("/api/Account/Exceptions/{id:long}/Resolve")]
    public Task<IActionResult> Resolve(long id, [FromBody] ExceptionResolveRequest request) => Done(_s4.ResolveExceptionAsync(id, request, Caller()));

    [HttpGet("/api/Account/Tax")]
    public Task<IActionResult> Tax([FromQuery] DateTime? from, [FromQuery] DateTime? to) => Done(_s4.TaxReportAsync(from, to, Caller()));

    [HttpGet("/api/Account/Tax/Export")]
    public Task<IActionResult> TaxExport([FromQuery] DateTime? from, [FromQuery] DateTime? to) => Done(_s4.TaxExportAsync(from, to, Caller()));

    [HttpGet("/api/Account/Payees")]
    public Task<IActionResult> Payees() => Done(_s4.ListPayeesAsync(Caller()));

    [HttpPut("/api/Account/Payees/{id:int}")]
    public Task<IActionResult> UpdatePayee(int id, [FromBody] PayeeUpdateRequest request) => Done(_s4.UpdatePayeeAsync(id, request, Caller()));

    [HttpPost("/api/Account/Payees/{id:int}/BankOtp")]
    public Task<IActionResult> BankOtp(int id) => Done(_s4.RequestPayeeBankOtpAsync(id, Caller()));

    [HttpGet("/api/Account/ClinicCollections")]
    public Task<IActionResult> Collections([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId)
        => Done(_s4.ClinicCollectionsAsync(from, to, doctorId, Caller()));

    /// <summary>DMO-10.02 — doctor mobile earnings rollup (own clinic).</summary>
    [HttpGet("/api/Earnings/Summary")]
    public Task<IActionResult> EarningsSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s4.EarningsSummaryAsync(from, to, Caller()));

    [HttpGet("/api/Account/Trail")]
    public Task<IActionResult> Trail([FromQuery] int? patientAppId, [FromQuery] long? paymentOrderId, [FromQuery] int? doctorId)
        => Done(_s4.TrailAsync(patientAppId, paymentOrderId, doctorId, Caller()));

    [HttpGet("/api/Trust/MyStatus")]
    public Task<IActionResult> MyStatus() => Done(_s4.MyVerificationAsync(Caller()));

    [HttpGet("/api/Trust/Queue")]
    public Task<IActionResult> Queue([FromQuery] string? status) => Done(_s4.VerificationQueueAsync(status, Caller()));

    [HttpGet("/api/Trust/{doctorId:int}")]
    public Task<IActionResult> Trust(int doctorId) => Done(_s4.VerificationDetailAsync(doctorId, Caller()));

    [HttpPost("/api/Trust/{doctorId:int}/Decide")]
    public Task<IActionResult> Decide(int doctorId, [FromBody] VerificationDecisionRequest request)
        => Done(_s4.DecideVerificationAsync(doctorId, request, Caller()));

    [HttpPost("/api/Reviews")]
    public Task<IActionResult> Review([FromBody] ReviewCreateRequest request) => Done(_s4.CreateReviewAsync(request, Caller()));

    [AllowAnonymous]
    [HttpGet("/api/Reviews/Doctor/{doctorId:int}")]
    public Task<IActionResult> PublicReviews(int doctorId) => Done(_s4.ListPublicReviewsAsync(doctorId));

    [HttpGet("/api/Reviews/Mine")]
    public Task<IActionResult> MyReviews() => Done(_s4.ListMyReviewsAsync(Caller()));

    [HttpPost("/api/Reviews/{id:int}/Appeal")]
    public Task<IActionResult> Appeal(int id, [FromBody] ReviewAppealRequest request) => Done(_s4.AppealReviewAsync(id, request, Caller()));

    [HttpGet("/api/Reviews/Appeals")]
    public Task<IActionResult> Appeals() => Done(_s4.ListAppealsAsync(Caller()));

    [HttpPost("/api/Reviews/Appeals/{id:int}/Resolve")]
    public Task<IActionResult> ResolveAppeal(int id, [FromBody] AppealResolveRequest request) => Done(_s4.ResolveAppealAsync(id, request, Caller()));

    [AllowAnonymous]
    [HttpGet("/api/Doctors/RankingExplain/{doctorId:int}")]
    public Task<IActionResult> Ranking(int doctorId) => Done(_s4.RankingExplainAsync(doctorId));

    [HttpGet("/api/Erx/Potencies")]
    public Task<IActionResult> Potencies() => Done(_s4.ListPotenciesAsync());

    [HttpPut("/api/Erx/RemedyLines/{id:int}")]
    public Task<IActionResult> RemedyLine(int id, [FromBody] RemedyLineUpdateRequest request)
        => Done(_s4.UpdateRemedyLineAsync(id, request, Caller()));

    [HttpGet("/api/Erx/ByAppointment/{id:int}")]
    public Task<IActionResult> ErxByAppointment(int id) => Done(_s4.GetErxByAppointmentAsync(id, Caller(), false));

    [HttpPost("/api/Erx/Sign")]
    public Task<IActionResult> Sign([FromBody] SignErxRequest request) => Done(_s4.SignErxAsync(request, Caller()));

    [HttpGet("/api/Erx/History")]
    public Task<IActionResult> History([FromQuery] int? patientId, [FromQuery] int? patientAppId)
        => Done(_s4.ErxHistoryAsync(patientId, patientAppId, Caller()));

    [HttpGet("/api/Erx/Patient/{id:int}")]
    public Task<IActionResult> PatientErx(int id) => Done(_s4.GetErxByAppointmentAsync(id, Caller(), true));

    [HttpGet("/api/Erx/{id:int}/Pdf")]
    public Task<IActionResult> ErxPdf(int id) => Done(_s4.ErxPdfAsync(id, Caller()));

    [HttpPost("/api/Erx/Refills")]
    public Task<IActionResult> RequestRefill([FromBody] RefillCreateRequest request) => Done(_s4.RequestRefillAsync(request, Caller()));

    [HttpGet("/api/Erx/Refills")]
    public Task<IActionResult> Refills() => Done(_s4.ListRefillsAsync(Caller()));

    [HttpPost("/api/Erx/Refills/{id:int}/Approve")]
    public Task<IActionResult> ApproveRefill(int id) => Done(_s4.DecideRefillAsync(id, true, null, Caller()));

    [HttpPost("/api/Erx/Refills/{id:int}/Reject")]
    public Task<IActionResult> RejectRefill(int id, [FromBody] RefillDecisionRequest request)
        => Done(_s4.DecideRefillAsync(id, false, request?.Reason, Caller()));

    [HttpPost("/api/Pharmacy/Onboard")]
    public Task<IActionResult> Onboard([FromBody] PharmacyOnboardRequest request) => Done(_s4.OnboardPharmacyAsync(request, Caller()));

    [HttpPost("/api/Pharmacy/{id:int}/Activate")]
    public Task<IActionResult> Activate(int id) => Done(_s4.ActivatePharmacyAsync(id, Caller()));

    [HttpPost("/api/Pharmacy/Licences/Sweep")]
    public Task<IActionResult> Sweep() => Done(_s4.SweepLicencesAsync(Caller()));

    [AllowAnonymous]
    [HttpGet("/api/Pharmacy/Sellers")]
    public Task<IActionResult> Sellers([FromQuery] string? area) => Done(_s4.ListSellersAsync(area));

    [HttpGet("/api/Pharmacy/Partners")]
    public Task<IActionResult> Partners() => Done(_s4.ListPharmacyPartnersAsync(Caller()));

    [HttpGet("/api/Pharmacy/Orders")]
    public Task<IActionResult> PharmacyOrders() => Done(_s4.PharmacyQueueAsync(Caller()));

    [HttpPost("/api/MedicineOrders")]
    public Task<IActionResult> CreateOrder([FromBody] MedicineOrderCreateRequest request) => Done(_s4.CreateMedicineOrderAsync(request, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Consent")]
    public Task<IActionResult> Consent(int id) => Done(_s4.GrantMedicineConsentAsync(id, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/AcceptOtp")]
    public Task<IActionResult> AcceptOtp(int id) => Done(_s4.RequestMedicineAcceptOtpAsync(id, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Accept")]
    public Task<IActionResult> Accept(int id, [FromBody] PharmacyAcceptRequest request)
    {
        request ??= new PharmacyAcceptRequest();
        request.MedicineOrderId = id;
        return Done(_s4.AcceptMedicineOrderAsync(request, Caller()));
    }

    [HttpPost("/api/MedicineOrders/{id:int}/Reject")]
    public Task<IActionResult> RejectOrder(int id, [FromBody] MedicineRejectRequest request)
        => Done(_s4.RejectMedicineOrderAsync(id, request, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Quote")]
    public Task<IActionResult> Quote(int id, [FromBody] MedicineQuoteRequest request)
        => Done(_s4.QuoteMedicineOrderAsync(id, request, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/AcceptQuote")]
    public Task<IActionResult> AcceptQuote(int id) => Done(_s4.AcceptQuoteAsync(id, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Ready")]
    public Task<IActionResult> Ready(int id) => Done(_s4.MarkMedicineReadyAsync(id, Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Dispatch")]
    public Task<IActionResult> Dispatch(int id) => Done(_s4.DispatchMedicineAsync(id, Caller()));

    [HttpGet("/api/MedicineOrders/{id:int}/Tracking")]
    public Task<IActionResult> Tracking(int id) => Done(_s4.MedicineTrackingAsync(id, Caller()));

    [HttpGet("/api/Patient/MedicineOrders")]
    public Task<IActionResult> MyOrders() => Done(_s4.PatientMedicineOrdersAsync(Caller()));

    [HttpPost("/api/MedicineOrders/Refill/{refillId:int}")]
    public Task<IActionResult> RefillOrder(int refillId) => Done(_s4.CloneRefillOrderAsync(refillId, Caller()));

    [HttpGet("/api/Admin/HomemedsExceptions")]
    public Task<IActionResult> MedExceptions() => Done(_s4.ListMedicineExceptionsAsync(Caller()));

    [HttpPost("/api/Admin/HomemedsExceptions/{orderId:int}/Reroute")]
    public Task<IActionResult> Reroute(int orderId, [FromQuery] int pharmacyId) => Done(_s4.RerouteMedicineAsync(orderId, pharmacyId, Caller()));

    [HttpPut("/api/Pharmacy/Routing")]
    public Task<IActionResult> Routing([FromBody] RoutingRuleRequest request) => Done(_s4.SaveRoutingAsync(request, Caller()));

    [HttpGet("/api/Patient/Timeline")]
    public Task<IActionResult> Timeline([FromQuery] int? patientId) => Done(_s4.TimelineAsync(patientId, Caller()));

    [HttpGet("/api/Patient/Consultations/{patientAppId:int}/Note")]
    public Task<IActionResult> Note(int patientAppId) => Done(_s4.ConsultationNoteAsync(patientAppId, Caller()));

    [HttpPost("/api/Patient/Documents")]
    [RequestSizeLimit(10_000_000)]
    public Task<IActionResult> Document([FromForm] IFormFile file) => Done(_s4.SavePatientDocumentAsync(file, Caller()));

    [HttpPost("/api/Patient/FollowUps")]
    public Task<IActionResult> FollowUp([FromBody] FollowUpCreateRequest request) => Done(_s4.SetFollowUpAsync(request, Caller()));

    [HttpGet("/api/Patient/FollowUps")]
    public Task<IActionResult> FollowUps([FromQuery] int? patientId) => Done(_s4.ListFollowUpsAsync(patientId, Caller()));

    [HttpPost("/api/Patient/FollowUps/{taskId:int}/Complete")]
    public Task<IActionResult> Complete(int taskId) => Done(_s4.CompleteFollowUpAsync(taskId, Caller()));

    [HttpPost("/api/Patient/Diary")]
    public Task<IActionResult> Diary([FromBody] DiaryWriteRequest request) => Done(_s4.SaveDiaryAsync(request, Caller()));

    [HttpGet("/api/Patient/Diary")]
    public Task<IActionResult> DiaryList([FromQuery] int? patientId) => Done(_s4.ListDiaryAsync(patientId, Caller()));

    [HttpPut("/api/Patient/Diary/{id:int}")]
    public Task<IActionResult> DiaryUpdate(int id, [FromBody] DiaryWriteRequest request) => Done(_s4.UpdateDiaryAsync(id, request, Caller()));

    [HttpDelete("/api/Patient/Diary/{id:int}")]
    public Task<IActionResult> DiaryDelete(int id) => Done(_s4.DeleteDiaryAsync(id, Caller()));

    [HttpGet("/api/Patient/Progress")]
    public Task<IActionResult> Progress([FromQuery] int? patientId) => Done(_s4.ProgressAsync(patientId, Caller()));

    [HttpGet("/api/Patient/Consents")]
    public Task<IActionResult> Consents() => Done(_s4.ListConsentsAsync(Caller()));

    [HttpPost("/api/Patient/Consents/{id:long}/Withdraw")]
    public Task<IActionResult> Withdraw(long id) => Done(_s4.WithdrawConsentAsync(id, Caller()));

    [HttpPost("/api/Patient/DataRequests")]
    public Task<IActionResult> DataRequest([FromBody] DataRequestCreate request) => Done(_s4.CreateDataRequestAsync(request, Caller()));

    [HttpGet("/api/Patient/Profile")]
    public Task<IActionResult> Profile() => Done(_s4.GetHealthProfileAsync(Caller()));

    [HttpPut("/api/Patient/Profile")]
    public Task<IActionResult> UpdateProfile([FromBody] HealthProfileUpdate request) => Done(_s4.UpdateHealthProfileAsync(request, Caller()));

    private async Task<IActionResult> Done(Task<S4ActionResult> work)
    {
        try
        {
            var result = await work;
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S4 request failed for {Path}", HttpContext.Request.Path);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                success = false,
                code = "SERVER",
                message = "The request could not be completed."
            });
        }
    }

    private S4Caller Caller()
    {
        var role = DoctorOwnership.GetRoleName(User) ?? "";
        int? patientId = null;
        if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        {
            var owner = PatientPortalOwnerResolver.ResolveAsync(_context, User.GetUserId(), createIfMissing: false).GetAwaiter().GetResult();
            patientId = owner?.PatientId;
        }
        return new S4Caller
        {
            UserId = User.GetUserId(),
            DoctorId = DoctorOwnership.GetDoctorId(User),
            PatientId = patientId,
            Role = role,
            IsAdmin = DoctorOwnership.IsGlobalAdminPortalUser(User)
        };
    }

}
