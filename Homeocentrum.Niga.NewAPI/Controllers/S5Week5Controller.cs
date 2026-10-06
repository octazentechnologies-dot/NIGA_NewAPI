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

    [HttpGet("/api/WhatsApp/Audience")]
    public Task<IActionResult> WhatsAppAudience() => Done(_s5.WhatsAppAudienceAsync(Caller()));

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

    [HttpGet("/api/AdminDashboard/Summary")]
    public Task<IActionResult> AdminSummary([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? doctorId)
        => Done(_s5.AdminSummaryAsync(from, to, doctorId, Caller()));

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

    [HttpGet("/api/Doctor/Reminders")]
    public Task<IActionResult> Reminders([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.ListRemindersAsync(from, to, Caller()));

    [HttpPost("/api/Doctor/Reminders")]
    public Task<IActionResult> CreateReminder([FromBody] DoctorReminderWrite request)
        => Done(_s5.SaveReminderAsync(null, request, Caller()));

    [HttpPut("/api/Doctor/Reminders/{id:int}")]
    public Task<IActionResult> UpdateReminder(int id, [FromBody] DoctorReminderWrite request)
        => Done(_s5.SaveReminderAsync(id, request, Caller()));

    [HttpDelete("/api/Doctor/Reminders/{id:int}")]
    public Task<IActionResult> DeleteReminder(int id) => Done(_s5.DeleteReminderAsync(id, Caller()));

    [HttpGet("/api/Reports/Doctor/Practice")]
    public Task<IActionResult> PracticeReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.PracticeReportAsync(from, to, Caller()));

    [HttpGet("/api/Reports/Doctor/FollowUps")]
    public Task<IActionResult> FollowUpReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.FollowUpReportAsync(from, to, Caller()));

    [HttpPut("/api/Reports/Doctor/FollowUps/{taskId:int}")]
    public Task<IActionResult> UpdateFollowUpTask(int taskId, [FromBody] FollowUpTaskWrite request)
        => Done(_s5.UpdateFollowUpTaskAsync(taskId, request, Caller()));

    [HttpGet("/api/Doctor/Reviews")]
    public Task<IActionResult> DoctorReviews() => Done(_s5.ListDoctorReviewsAsync(Caller()));

    [HttpPut("/api/Doctor/Reviews/{reviewId:int}/Reply")]
    public Task<IActionResult> ReviewReply(int reviewId, [FromBody] ReviewReplyWrite request)
        => Done(_s5.SaveReviewReplyAsync(reviewId, request, Caller()));

    [HttpGet("/api/Pharmacy/{pharmacyPartnerId:int}/Config")]
    public Task<IActionResult> PharmacyConfig(int pharmacyPartnerId)
        => Done(_s5.GetPharmacyConfigAsync(pharmacyPartnerId, Caller()));

    [HttpPut("/api/Pharmacy/{pharmacyPartnerId:int}/Config")]
    public Task<IActionResult> SavePharmacyConfig(int pharmacyPartnerId, [FromBody] PharmacyConfigWrite request)
        => Done(_s5.SavePharmacyConfigAsync(pharmacyPartnerId, request, Caller()));

    [HttpGet("/api/Patient/MedicineOrders/History")]
    public Task<IActionResult> PatientMedicineHistory() => Done(_s5.PatientMedicineHistoryAsync(Caller()));

    [HttpPost("/api/MedicineOrders/{id:int}/Review")]
    public Task<IActionResult> ReviewMedicineOrder(int id, [FromBody] MedicineOrderReviewWrite request)
        => Done(_s5.ReviewMedicineOrderAsync(id, request, Caller()));

    [HttpGet("/api/Admin/Reviews")]
    public Task<IActionResult> AdminReviews() => Done(_s5.ListAdminReviewsAsync(Caller()));

    [HttpPut("/api/Admin/Reviews/{reviewId:int}/Status")]
    public Task<IActionResult> AdminReviewStatus(int reviewId, [FromBody] ReviewStatusWrite request)
        => Done(_s5.SetReviewStatusAsync(reviewId, request, Caller()));

    [HttpGet("/api/Reports/Doctor/Earnings")]
    public Task<IActionResult> EarningsReport([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Done(_s5.EarningsReportAsync(from, to, Caller()));

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
