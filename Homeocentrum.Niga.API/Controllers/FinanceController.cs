using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Accounts back office: ledger, reconciliation, settlements, payouts, payment exceptions, tax, payees, clinic collections, and the payment trail.
/// </summary>
[ApiController]
[Authorize]
public class FinanceController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<FinanceController> _logger;

    public FinanceController(IS4Week4Service s4, NIGACentrumContext context, ILogger<FinanceController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Account/Ledger")]
    public Task<IActionResult> Ledger([FromQuery] string? stream, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId, [FromQuery] int page = 1)
        => Done(_s4.LedgerAsync(stream, from, to, doctorId, page, Caller()));

    [SecurityAudit(SecurityAuditEvents.FinanceExport)]
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

    [SecurityAudit(SecurityAuditEvents.PayoutOtpRequested)]
    [HttpPost("/api/Account/Payouts/{id:long}/Otp")]
    public Task<IActionResult> PayoutOtp(long id) => Done(_s4.RequestPayoutOtpAsync(id, Caller()));

    [SecurityAudit(SecurityAuditEvents.PayoutOtpVerify)]
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

    [SecurityAudit(SecurityAuditEvents.FinanceExport)]
    [HttpGet("/api/Account/Tax/Export")]
    public Task<IActionResult> TaxExport([FromQuery] DateTime? from, [FromQuery] DateTime? to) => Done(_s4.TaxExportAsync(from, to, Caller()));

    [HttpGet("/api/Account/Payees")]
    public Task<IActionResult> Payees() => Done(_s4.ListPayeesAsync(Caller()));

    [HttpPut("/api/Account/Payees/{id:int}")]
    public Task<IActionResult> UpdatePayee(int id, [FromBody] PayeeUpdateRequest request) => Done(_s4.UpdatePayeeAsync(id, request, Caller()));

    [SecurityAudit(SecurityAuditEvents.PayeeBankOtpRequested)]
    [HttpPost("/api/Account/Payees/{id:int}/BankOtp")]
    public Task<IActionResult> BankOtp(int id) => Done(_s4.RequestPayeeBankOtpAsync(id, Caller()));

    [HttpGet("/api/Account/ClinicCollections")]
    public Task<IActionResult> Collections([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId)
        => Done(_s4.ClinicCollectionsAsync(from, to, doctorId, Caller()));

    [HttpGet("/api/Account/Trail")]
    public Task<IActionResult> Trail([FromQuery] int? patientAppId, [FromQuery] long? paymentOrderId, [FromQuery] int? doctorId)
        => Done(_s4.TrailAsync(patientAppId, paymentOrderId, doctorId, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}
