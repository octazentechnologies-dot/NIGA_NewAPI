using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.DTOs;
using Homeocentrum.Niga.API.Domain.Extensions;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Controllers;

/// <summary>
/// Consultation and medicine payments: checkout orders, gateway verification, the Razorpay webhook, and reception collection.
/// Razorpay keys stay in configuration and are never returned except the public key id when checkout is ready.
/// Appointment paid status changes only from a verified webhook or reception collection.
/// </summary>
[ApiController]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IS4Week4Service _s4;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IS4Week4Service s4, NIGACentrumContext context, ILogger<PaymentsController> logger)
    {
        _s4 = s4;
        _context = context;
        _logger = logger;
    }

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

    private Task<IActionResult> Done(Task<S4ActionResult> work) => this.Respond(work, _logger);
    private S4Caller Caller() => this.S4Caller(_context);
}
