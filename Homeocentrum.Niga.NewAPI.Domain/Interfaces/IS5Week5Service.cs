namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IS5Week5Service
{
    Task<S4ActionResult> ListSmsTemplatesAsync(S4Caller caller);
    Task<S4ActionResult> SaveSmsTemplateAsync(SmsTemplateWrite request, S4Caller caller);
    Task<S4ActionResult> SendSmsAsync(SmsSendRequest request, S4Caller caller);
    Task<S4ActionResult> SmsHistoryAsync(S4Caller caller);
    Task<S4ActionResult> WhatsAppReceiptAsync(WhatsAppReceiptRequest request);
    Task<S4ActionResult> WhatsAppBulkAsync(S4Caller caller);
    Task<S4ActionResult> SendNotificationAsync(NotificationSendRequest request, S4Caller caller);
    Task<S4ActionResult> ListNotificationsAsync(S4Caller caller);
    Task<S4ActionResult> PatchNotificationAsync(long id, bool isRead, S4Caller caller);
    Task<S4ActionResult> SendReceiptEmailAsync(long paymentOrderId, S4Caller caller);
    Task<S4ActionResult> EmailHistoryAsync(S4Caller caller);
    Task<S4ActionResult> OverviewAsync(S4Caller caller);
    Task<S4ActionResult> FollowUpDueAsync(S4Caller caller);
    Task<S4ActionResult> FollowUpSummaryAsync(S4Caller caller);
    Task<S4ActionResult> ClinicPerformanceAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> ExportReconciliationAsync(S4Caller caller);
    Task<S4ActionResult> ExportSettlementsAsync(S4Caller caller);
    Task<S4ActionResult> ExportPayoutsAsync(S4Caller caller);
    Task<S4ActionResult> MedicineReportAsync(S4Caller caller);
    Task<S4ActionResult> EarningsBucketsAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> ExportUsersAsync(S4Caller caller);
    Task<S4ActionResult> ImportUsersAsync(UserImportRequest request, S4Caller caller);
    Task<S4ActionResult> SecurityPostureAsync(S4Caller caller);
}

public class SmsTemplateWrite
{
    public int? SmsTemplateId { get; set; }
    public string? Code { get; set; }
    public string? Body { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SmsSendRequest
{
    public string? TemplateCode { get; set; }
    public string? Mobile { get; set; }
    public string? Body { get; set; }
}

public class NotificationSendRequest
{
    public long UserId { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
}

public class WhatsAppReceiptRequest
{
    public string? MetaMessageId { get; set; }
    public string? Status { get; set; }
    public string? Payload { get; set; }
}

public class UserImportRequest
{
    public List<UserImportRow>? Users { get; set; }
}

public class UserImportRow
{
    public string? UserName { get; set; }
    public string? EmailId { get; set; }
}
