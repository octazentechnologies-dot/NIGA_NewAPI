using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Medicine orders: create, consent, pharmacy accept or reject, quote, dispatch, delivery, tracking, patient order lists, reviews, and admin exceptions.
/// </summary>
[ApiController]
[Authorize]
public class MedicineOrdersController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<MedicineOrdersController> _logger;

    public MedicineOrdersController(IS4Week4Service s4, IS5Week5Service s5, NIGACentrumContext context, ILogger<MedicineOrdersController> logger)
    {
        _s4 = s4;
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

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

    [HttpPost("/api/MedicineOrders/{id:int}/Deliver")]
    public Task<IActionResult> Deliver(int id) => Done(_s4.DeliverMedicineAsync(id, Caller()));

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

    [HttpGet("/api/Patient/MedicineOrders/History")]
    public Task<IActionResult> PatientMedicineHistory() => Done(_s5.PatientMedicineHistoryAsync(Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Review")]
    public Task<IActionResult> ReviewMedicineOrder(int id, [FromBody] MedicineOrderReviewWrite request)
        => Done(_s5.ReviewMedicineOrderAsync(id, request, Caller()));

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}
