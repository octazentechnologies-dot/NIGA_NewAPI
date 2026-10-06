using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

/// <summary>Doctor profile "My Reviews" (list, public reply) and admin review moderation.</summary>
public partial class S5Week5Service
{
    private const int MaxReplyLength = 500;

    public Task<S4ActionResult> ListDoctorReviewsAsync(S4Caller caller) => ReviewGuard(async () =>
    {
        if (!caller.DoctorId.HasValue)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Reviews are for a doctor account.");
        var doctorId = caller.DoctorId.Value;
        var rows = await _context.Database.SqlQuery<DoctorReviewRow>($@"
            SELECT r.ReviewId, r.PatientAppId, r.PatientId, p.PatientName, r.Rating, r.Text, r.Status, r.At,
                   r.DoctorReply, r.RepliedAt,
                   (SELECT TOP 1 ra.Status FROM dbo.ReviewAppeal ra WHERE ra.ReviewId = r.ReviewId ORDER BY ra.ReviewAppealId DESC) AS AppealStatus
            FROM dbo.Review r
            LEFT JOIN dbo.Patient p ON p.PatientID = r.PatientId
            WHERE r.DoctorId = {doctorId}
            ORDER BY r.At DESC").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> SaveReviewReplyAsync(int reviewId, ReviewReplyWrite request, S4Caller caller) => ReviewGuard(async () =>
    {
        if (!caller.DoctorId.HasValue)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Only the reviewed doctor can reply.");
        var doctorId = caller.DoctorId.Value;
        var reply = string.IsNullOrWhiteSpace(request?.Reply) ? null : request!.Reply.Trim();
        if (reply != null && reply.Length > MaxReplyLength)
            return S4ActionResult.Fail(400, "VALIDATION", $"Reply must be {MaxReplyLength} characters or fewer.");
        DateTime? at = reply == null ? null : DateTime.Now;
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Review SET DoctorReply = {reply}, RepliedAt = {at}
            WHERE ReviewId = {reviewId} AND DoctorId = {doctorId}");
        if (updated == 0)
            return S4ActionResult.Fail(404, "NOT_FOUND", "Review not found.");
        return S4ActionResult.Ok(new { success = true, reviewId, reply, repliedAt = at });
    });

    public Task<S4ActionResult> ListAdminReviewsAsync(S4Caller caller) => ReviewGuard(async () =>
    {
        if (!caller.IsAdmin)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Review moderation is for admin.");
        var rows = await _context.Database.SqlQuery<AdminReviewRow>($@"
            SELECT TOP 1000 r.ReviewId, r.PatientAppId, r.DoctorId, r.PatientId, p.PatientName,
                   LTRIM(RTRIM(CONCAT(N'Dr. ', d.FirstName, N' ', d.LastName))) AS DoctorName,
                   a.ConsultMode, r.Rating, r.Text, r.Status, r.At, r.DoctorReply, r.ModerationNote, r.ModeratedAt,
                   ap.ReviewAppealId AS OpenAppealId, ap.Reason AS OpenAppealReason
            FROM dbo.Review r
            LEFT JOIN dbo.Patient p ON p.PatientID = r.PatientId
            LEFT JOIN dbo.Doctor d ON d.DoctorID = r.DoctorId
            LEFT JOIN dbo.PatientAppointment a ON a.PatientAppId = r.PatientAppId
            OUTER APPLY (SELECT TOP 1 x.ReviewAppealId, x.Reason FROM dbo.ReviewAppeal x
                         WHERE x.ReviewId = r.ReviewId AND x.Status = N'OPEN' ORDER BY x.ReviewAppealId DESC) ap
            ORDER BY r.At DESC").ToListAsync();
        return S4ActionResult.Ok(new { success = true, data = rows });
    });

    public Task<S4ActionResult> SetReviewStatusAsync(int reviewId, ReviewStatusWrite request, S4Caller caller) => ReviewGuard(async () =>
    {
        if (!caller.IsAdmin)
            return S4ActionResult.Fail(403, "FORBIDDEN", "Only admin can moderate reviews.");
        var status = (request?.Status ?? "").Trim().ToUpperInvariant();
        if (status is not ("APPROVED" or "REJECTED"))
            return S4ActionResult.Fail(400, "VALIDATION", "Status must be APPROVED or REJECTED.");
        var note = string.IsNullOrWhiteSpace(request?.Note) ? null : request!.Note.Trim();
        if (note != null && note.Length > MaxReplyLength)
            return S4ActionResult.Fail(400, "VALIDATION", $"Note must be {MaxReplyLength} characters or fewer.");
        var at = DateTime.Now;
        var updated = await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Review SET Status = {status}, ModerationNote = {note}, ModeratedAt = {at}
            WHERE ReviewId = {reviewId}");
        if (updated == 0)
            return S4ActionResult.Fail(404, "NOT_FOUND", "Review not found.");
        return S4ActionResult.Ok(new { success = true, reviewId, status, note, moderatedAt = at });
    });

    private async Task<S4ActionResult> ReviewGuard(Func<Task<S4ActionResult>> work)
    {
        try
        {
            return await Guard(work);
        }
        catch (SqlException ex) when (ex.Number == 207)
        {
            return S4ActionResult.Fail(503, "SCHEMA", "Run ScriptsAndFiles/S5_Week5/Database scripts/06_S5_Review_Doctor_Reply.sql.");
        }
    }

    private sealed class DoctorReviewRow
    {
        public int ReviewId { get; set; }
        public int? PatientAppId { get; set; }
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
        public string? Status { get; set; }
        public DateTime At { get; set; }
        public string? DoctorReply { get; set; }
        public DateTime? RepliedAt { get; set; }
        public string? AppealStatus { get; set; }
    }

    private sealed class AdminReviewRow
    {
        public int ReviewId { get; set; }
        public int? PatientAppId { get; set; }
        public int? DoctorId { get; set; }
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public string? DoctorName { get; set; }
        public string? ConsultMode { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
        public string? Status { get; set; }
        public DateTime At { get; set; }
        public string? DoctorReply { get; set; }
        public string? ModerationNote { get; set; }
        public DateTime? ModeratedAt { get; set; }
        public int? OpenAppealId { get; set; }
        public string? OpenAppealReason { get; set; }
    }
}
