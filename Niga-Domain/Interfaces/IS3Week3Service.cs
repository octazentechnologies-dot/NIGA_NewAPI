using Niga_Domain.DTOs;

namespace Niga_Domain.Interfaces
{
    public interface IS3Week3Service
    {
        Task<S3ActionResult> JoinWaitlistAsync(JoinWaitlistRequest request);
        Task<S3ActionResult> GetWaitlistAsync(int doctorId);

        Task<S3ActionResult> GetReceptionProfileAsync(int receptionStaffId);
        Task<S3ActionResult> UpdateReceptionProfileAsync(int receptionStaffId, ReceptionProfileUpdate request);

        Task<S3ActionResult> SaveCasePaperAsync(CasePaperRequest request, int doctorId, long userId);
        Task<S3ActionResult> GetCasePapersAsync(int doctorId, int patientId);

        Task<S3ActionResult> SetTeleAvailabilityAsync(int doctorId, bool isOnline);
        Task<S3ActionResult> GetTeleAvailabilityAsync(int doctorId);

        Task<S3ActionResult> GetTeleQueueAsync(int doctorId);
        Task<S3ActionResult> CreateSessionAsync(int patientAppId, int doctorId);
        Task<S3ActionResult> StartSessionAsync(int sessionId, int doctorId);
        Task<S3ActionResult> EndSessionAsync(int sessionId, int doctorId);
        Task<S3ActionResult> IssueTokenAsync(int sessionId, S3Caller caller, bool rejoin);
        Task<S3ActionResult> GetSessionAsync(int sessionId, S3Caller caller);
        Task<S3ActionResult> CaptureConsentAsync(TeleConsentRequest request, S3Caller caller);
        Task<S3ActionResult> LogJoinFailureAsync(int sessionId, string? code, S3Caller caller);
        Task<S3ActionResult> PostChatAsync(TeleChatRequest request, S3Caller caller);
        Task<S3ActionResult> ListChatAsync(int sessionId, S3Caller caller);
        Task<S3ActionResult> SaveSummaryAsync(ConsultationSummaryRequest request, int doctorId);
        Task<S3ActionResult> GetSummaryAsync(int patientAppId, S3Caller caller);

        Task<S3ActionResult> RequestInstantAsync(InstantConsultRequestBody request);
        Task<S3ActionResult> ListInstantOffersAsync(int doctorId);
        Task<S3ActionResult> AcceptInstantAsync(int requestId, int doctorId);

        Task<S3ActionResult> CreateTicketAsync(SupportTicketCreate request, long userId, string role);
        Task<S3ActionResult> ListMyTicketsAsync(long userId);
        Task<S3ActionResult> ListAdminTicketsAsync(string? status, string? priority);
        Task<S3ActionResult> UpdateTicketAsync(int ticketId, SupportTicketUpdate request);
        Task<S3ActionResult> AddMessageAsync(int ticketId, SupportMessageCreate request, long userId, string role, bool isAdmin);
        Task<S3ActionResult> ListMessagesAsync(int ticketId, long userId, bool isAdmin);

        Task<S3ActionResult> ListHelpAsync(bool includeUnpublished);
        Task<S3ActionResult> GetHelpAsync(string slug, bool includeUnpublished);
        Task<S3ActionResult> SaveHelpAsync(HelpArticleWrite request);

        Task<S3ActionResult> AssistedBookAsync(AssistedBookRequest request, long userId);
        Task<S3ActionResult> GetDoctorContextAsync(int patientAppId, S3Caller caller);

        Task<S3ActionResult> ListRefillsAsync();
        Task<S3ActionResult> DecideRefillAsync(int refillId, bool approve, string? reason);
    }

    public class S3Caller
    {
        public long UserId { get; set; }
        public int? DoctorId { get; set; }
        public string Role { get; set; } = "";
        public bool IsAdmin { get; set; }
    }

    public class S3ActionResult
    {
        public int StatusCode { get; set; }
        public object? Body { get; set; }

        public static S3ActionResult Ok(object body) => new() { StatusCode = 200, Body = body };

        public static S3ActionResult Fail(int code, string message)
            => new() { StatusCode = code, Body = new { success = false, message } };
    }
}
