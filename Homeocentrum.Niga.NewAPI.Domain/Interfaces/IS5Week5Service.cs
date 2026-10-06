using Homeocentrum.Niga.NewAPI.Domain.DTOs;

namespace Homeocentrum.Niga.NewAPI.Domain.Interfaces;

public interface IS5Week5Service
{
    Task<S4ActionResult> ListSmsTemplatesAsync(S4Caller caller);
    Task<S4ActionResult> SaveSmsTemplateAsync(SmsTemplateWrite request, S4Caller caller);
    Task<S4ActionResult> SendSmsAsync(SmsSendRequest request, S4Caller caller);
    Task<S4ActionResult> SmsHistoryAsync(S4Caller caller);
    Task<S4ActionResult> ListSmsEventsAsync(S4Caller caller);
    Task<S4ActionResult> SaveSmsPreferenceAsync(DoctorSmsPreferenceWrite request, S4Caller caller);
    Task<S4ActionResult> RegisterDeviceAsync(DeviceRegisterRequest request, S4Caller caller);
    Task<S4ActionResult> WhatsAppReceiptAsync(WhatsAppReceiptRequest request);
    Task<S4ActionResult> WhatsAppBulkAsync(S4Caller caller);
    Task<S4ActionResult> WhatsAppAudienceAsync(S4Caller caller);
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
    Task<S4ActionResult> ListRemindersAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> SaveReminderAsync(int? reminderId, DoctorReminderWrite request, S4Caller caller);
    Task<S4ActionResult> DeleteReminderAsync(int reminderId, S4Caller caller);
    Task<S4ActionResult> PracticeReportAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> FollowUpReportAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> UpdateFollowUpTaskAsync(int taskId, FollowUpTaskWrite request, S4Caller caller);
    Task<S4ActionResult> EarningsReportAsync(DateTime? from, DateTime? to, S4Caller caller);
    Task<S4ActionResult> ListDoctorReviewsAsync(S4Caller caller);
    Task<S4ActionResult> SaveReviewReplyAsync(int reviewId, ReviewReplyWrite request, S4Caller caller);
    Task<S4ActionResult> ListAdminReviewsAsync(S4Caller caller);
    Task<S4ActionResult> SetReviewStatusAsync(int reviewId, ReviewStatusWrite request, S4Caller caller);
    Task<S4ActionResult> GetPharmacyConfigAsync(int pharmacyPartnerId, S4Caller caller);
    Task<S4ActionResult> SavePharmacyConfigAsync(int pharmacyPartnerId, PharmacyConfigWrite request, S4Caller caller);

    Task<S4ActionResult> AdminSummaryAsync(DateTime? from, DateTime? to, int? doctorId, S4Caller caller);
    Task<S4ActionResult> PatientMedicineHistoryAsync(S4Caller caller);
    Task<S4ActionResult> ReviewMedicineOrderAsync(int orderId, MedicineOrderReviewWrite request, S4Caller caller);
}

public class MedicineOrderReviewWrite
{
    /// <summary>1 to 5.</summary>
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class PharmacyConfigWrite
{
    /// <summary>HH:mm.</summary>
    public string? OpenTime { get; set; }
    /// <summary>HH:mm, after OpenTime.</summary>
    public string? CloseTime { get; set; }
    /// <summary>Mon..Sun short names.</summary>
    public List<string>? Days { get; set; }
    public List<string>? Areas { get; set; }
    public decimal? DeliveryCharge { get; set; }
    public decimal? FreeAbove { get; set; }
    public int? Capacity { get; set; }
}

public class ReviewStatusWrite
{
    /// <summary>APPROVED (published) or REJECTED (hidden from the profile).</summary>
    public string? Status { get; set; }
    public string? Note { get; set; }
}

public class ReviewReplyWrite
{
    /// <summary>Empty removes the reply.</summary>
    public string? Reply { get; set; }
}

public class FollowUpTaskWrite
{
    public DateTime? DueDate { get; set; }
    public string? Title { get; set; }
    /// <summary>OPEN, DONE or CANCELLED.</summary>
    public string? Status { get; set; }
}

public class DoctorReminderWrite
{
    public DateTime? ReminderDate { get; set; }
    /// <summary>24-hour "HH:mm"; empty means an all-day / call reminder.</summary>
    public string? ReminderTime { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ContactNumber { get; set; }
    public bool IsDone { get; set; }
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

public class DoctorSmsPreferenceWrite
{
    public string? TemplateCode { get; set; }
    public bool Enabled { get; set; } = true;
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
