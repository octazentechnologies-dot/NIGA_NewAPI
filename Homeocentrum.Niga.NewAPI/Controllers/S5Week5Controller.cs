using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Homeocentrum.Niga.NewAPI.Domain.Data;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Extensions;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Security;

namespace Homeocentrum.Niga.NewAPI.Controllers;

/// <summary>
/// S5 Week 5 HTTP: SMS, notifications, receipt email, clinic reports, and security posture.
/// Provider keys stay in appsettings.json and are never returned.
/// </summary>
[ApiController]
[Authorize]
public class S5Week5Controller : ControllerBase
{
    private readonly IS5Week5Service _s5;
    private readonly NIGACentrumContext _context;
    private readonly ILogger<S5Week5Controller> _logger;

    public S5Week5Controller(IS5Week5Service s5, NIGACentrumContext context, ILogger<S5Week5Controller> logger)
    {
        _s5 = s5;
        _context = context;
        _logger = logger;
    }

    [HttpGet("/api/Sms/Templates")]
    public Task<IActionResult> Templates() => Done(_s5.ListSmsTemplatesAsync(Caller()));

    [HttpPost("/api/Sms/Templates")]
    public Task<IActionResult> SaveTemplate([FromBody] SmsTemplateWrite request) => Done(_s5.SaveSmsTemplateAsync(request, Caller()));

    [HttpPost("/api/Sms/Send")]
    public Task<IActionResult> SendSms([FromBody] SmsSendRequest request) => Done(_s5.SendSmsAsync(request, Caller()));

    [HttpGet("/api/Sms/History")]
    public Task<IActionResult> SmsHistory() => Done(_s5.SmsHistoryAsync(Caller()));

    [HttpGet("/api/Sms/Events")]
    public Task<IActionResult> SmsEvents() => Done(_s5.ListSmsEventsAsync(Caller()));

    [HttpPut("/api/Sms/Preferences")]
    public Task<IActionResult> SmsPreference([FromBody] DoctorSmsPreferenceWrite request)
        => Done(_s5.SaveSmsPreferenceAsync(request, Caller()));

    [HttpPost("/api/Devices/Register")]
    public Task<IActionResult> RegisterDevice([FromBody] DeviceRegisterRequest request)
        => Done(_s5.RegisterDeviceAsync(request, Caller()));

    [AllowAnonymous]
    [HttpPost("/api/WhatsApp/Receipts")]
    public Task<IActionResult> Receipt([FromBody] WhatsAppReceiptRequest request) => Done(_s5.WhatsAppReceiptAsync(request));

    [HttpGet("/api/WhatsApp/Bulk")]
    public Task<IActionResult> Bulk() => Done(_s5.WhatsAppBulkAsync(Caller()));

    [HttpPost("/api/Notifications/Send")]
    public Task<IActionResult> SendNote([FromBody] NotificationSendRequest request) => Done(_s5.SendNotificationAsync(request, Caller()));

    [HttpGet("/api/Notifications")]
    public Task<IActionResult> Notes() => Done(_s5.ListNotificationsAsync(Caller()));

    [HttpPatch("/api/Notifications/{id:long}")]
    public Task<IActionResult> PatchNote(long id, [FromQuery] bool isRead = true) => Done(_s5.PatchNotificationAsync(id, isRead, Caller()));

    [HttpPost("/api/Email/Receipts/{paymentOrderId:long}")]
    public Task<IActionResult> ReceiptEmail(long paymentOrderId) => Done(_s5.SendReceiptEmailAsync(paymentOrderId, Caller()));

    [HttpGet("/api/Email/History")]
    public Task<IActionResult> EmailHistory() => Done(_s5.EmailHistoryAsync(Caller()));

    [HttpGet("/api/AdminDashboard/Overview")]
    public Task<IActionResult> Overview() => Done(_s5.OverviewAsync(Caller()));

    [HttpGet("/api/Reports/FollowUpDue")]
    public Task<IActionResult> FollowDue() => Done(_s5.FollowUpDueAsync(Caller()));

    [HttpGet("/api/Reports/FollowUpSummary")]
    public Task<IActionResult> FollowSummary() => Done(_s5.FollowUpSummaryAsync(Caller()));

    [HttpGet("/api/Reports/ClinicPerformance")]
    public Task<IActionResult> Performance([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.ClinicPerformanceAsync(from, to, Caller()));

    [HttpGet("/api/Reports/Reconciliation/Export")]
    public Task<IActionResult> ReconExport() => Done(_s5.ExportReconciliationAsync(Caller()));

    [HttpGet("/api/Reports/Settlements/Export")]
    public Task<IActionResult> SettlementExport() => Done(_s5.ExportSettlementsAsync(Caller()));

    [HttpGet("/api/Reports/Payouts/Export")]
    public Task<IActionResult> PayoutExport() => Done(_s5.ExportPayoutsAsync(Caller()));

    [HttpGet("/api/Reports/MedicineOrders")]
    public Task<IActionResult> Medicine() => Done(_s5.MedicineReportAsync(Caller()));

    [HttpGet("/api/Earnings/Buckets")]
    public Task<IActionResult> Buckets([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.EarningsBucketsAsync(from, to, Caller()));

    [HttpGet("/api/Admin/Users/Export")]
    public Task<IActionResult> UsersExport() => Done(_s5.ExportUsersAsync(Caller()));

    [HttpPost("/api/Admin/Users/Import")]
    public Task<IActionResult> UsersImport([FromBody] UserImportRequest request) => Done(_s5.ImportUsersAsync(request, Caller()));

    [HttpGet("/api/Security/Posture")]
    public Task<IActionResult> Posture() => Done(_s5.SecurityPostureAsync(Caller()));

    private async Task<IActionResult> Done(Task<S4ActionResult> work)
    {
        try
        {
            var result = await work;
            return StatusCode(result.StatusCode, result.Body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "S5 request failed for {Path}", HttpContext.Request.Path);
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
