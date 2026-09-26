using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;
using Homeocentrum.Niga.NewAPI.Domain.Master;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

public partial class S4Week4Service
{
    public async Task<S4ActionResult> MyVerificationAsync(S4Caller caller)
    {
        if (!caller.DoctorId.HasValue)
            return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.DoctorId == caller.DoctorId && !d.DeleteStatus);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor not found.");
        return Ok(new { success = true, data = new { doctor.DoctorId, doctor.VerificationStatus, isVerified = IsVerified(doctor.VerificationStatus) } });
    }

    public async Task<S4ActionResult> VerificationQueueAsync(string? status, S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "The credential queue is for admin.");
        var filter = string.IsNullOrWhiteSpace(status) ? "Pending" : status.Trim();
        var rows = await _context.Doctors.AsNoTracking()
            .Where(d => !d.DeleteStatus && d.VerificationStatus == filter)
            .OrderBy(d => d.DoctorId)
            .Select(d => new { d.DoctorId, d.FirstName, d.LastName, d.VerificationStatus })
            .Take(200)
            .ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> VerificationDetailAsync(int doctorId, S4Caller caller)
    {
        if (!caller.IsAdmin && !caller.OwnsDoctor(doctorId))
            return Fail(403, "FORBIDDEN", "You cannot read this credential file.");
        var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor not found.");
        var events = await _context.Database.SqlQuery<VerificationEventRow>($@"
            SELECT DoctorVerificationEventId, DoctorId, Status, Note, ByUserId, At
            FROM dbo.DoctorVerificationEvent WHERE DoctorId = {doctorId} ORDER BY At DESC").ToListAsync();
        return Ok(new { success = true, data = new { doctor.DoctorId, doctor.VerificationStatus, events } });
    }

    public async Task<S4ActionResult> DecideVerificationAsync(int doctorId, VerificationDecisionRequest request, S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Only admin can decide credentialing.");
        var decision = (request?.Decision ?? "").Trim();
        var status = decision.ToUpperInvariant() switch
        {
            "APPROVE" => "Verified",
            "REJECT" => "Rejected",
            "NEEDSINFO" => "NeedsInfo",
            _ => ""
        };
        if (status.Length == 0)
            return Fail(400, "VALIDATION", "Decision must be Approve, Reject, or NeedsInfo.");
        if (status != "Verified" && string.IsNullOrWhiteSpace(request!.Note))
            return Fail(400, "VALIDATION", "Reject and NeedsInfo require a note.");
        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor not found.");
        doctor.VerificationStatus = status;
        doctor.ChangedDate = DateTime.Now;
        await _context.SaveChangesAsync();
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.DoctorVerificationEvent (DoctorId, Status, Note, ByUserId, At)
            VALUES ({doctorId}, {status}, {request!.Note}, {caller.UserId}, {DateTime.Now})");
        return Ok(new { success = true, data = new { doctorId, verificationStatus = status, isVerified = IsVerified(status) } });
    }

    public async Task<S4ActionResult> CreateReviewAsync(ReviewCreateRequest request, S4Caller caller)
    {
        if (!caller.IsPatient || caller.PatientId is null or <= 0)
            return Fail(403, "FORBIDDEN", "Only a patient can write a review.");
        if (request == null || request.PatientAppId <= 0)
            return Fail(400, "VALIDATION", "PatientAppId is required.");
        if (request.Rating is < 1 or > 5)
            return Fail(400, "VALIDATION", "Rating must be from 1 to 5.");
        if (request.Text != null && request.Text.Length > 1000)
            return Fail(400, "VALIDATION", "Review text cannot exceed 1000 characters.");
        var appointment = await LoadAppointmentAsync(request.PatientAppId);
        if (appointment == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (appointment.PatientId != caller.PatientId)
            return Fail(403, "FORBIDDEN", "You can review only your own visit.");
        if (S3AppointmentRulesCancelled(appointment.Status))
            return Fail(409, "CONFLICT", "A cancelled visit cannot be reviewed.");
        try
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.Review (PatientAppId, DoctorId, PatientId, Rating, Text, Status, At)
                VALUES ({appointment.PatientAppId}, {appointment.DoctorId}, {appointment.PatientId}, {request.Rating}, {request.Text}, N'APPROVED', {DateTime.Now})");
        }
        catch (Exception ex) when (IsDuplicate(ex))
        {
            return Fail(409, "CONFLICT", "This visit already has a review.");
        }
        return Ok(new { success = true, message = "Review saved." });
    }

    public async Task<S4ActionResult> ListPublicReviewsAsync(int doctorId)
    {
        if (doctorId <= 0) return Fail(400, "VALIDATION", "DoctorId is required.");
        var rows = await _context.Database.SqlQuery<ReviewRow>($@"
            SELECT ReviewId, PatientAppId, DoctorId, PatientId, Rating, Text, Status, At
            FROM dbo.Review
            WHERE DoctorId = {doctorId} AND Status = N'APPROVED'
            ORDER BY At DESC").ToListAsync();
        return Ok(new { success = true, data = rows.Select(r => new { r.ReviewId, r.Rating, r.Text, r.At }) });
    }

    public async Task<S4ActionResult> ListMyReviewsAsync(S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Doctor context is required.");
        var doctorId = caller.DoctorId.Value;
        var rows = await _context.Database.SqlQuery<ReviewRow>($@"
            SELECT ReviewId, PatientAppId, DoctorId, PatientId, Rating, Text, Status, At
            FROM dbo.Review WHERE DoctorId = {doctorId} ORDER BY At DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> AppealReviewAsync(int reviewId, ReviewAppealRequest request, S4Caller caller)
    {
        if (!caller.DoctorId.HasValue) return Fail(403, "FORBIDDEN", "Only the reviewed doctor can appeal.");
        if (request == null || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 3)
            return Fail(400, "VALIDATION", "An appeal reason is required.");
        var review = await _context.Database.SqlQuery<ReviewRow>($@"
            SELECT ReviewId, PatientAppId, DoctorId, PatientId, Rating, Text, Status, At
            FROM dbo.Review WHERE ReviewId = {reviewId}").FirstOrDefaultAsync();
        if (review == null) return Fail(404, "NOT_FOUND", "Review not found.");
        if (review.DoctorId != caller.DoctorId) return Fail(403, "FORBIDDEN", "This review is for another doctor.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.ReviewAppeal (ReviewId, DoctorId, Reason, Status, At)
            VALUES ({reviewId}, {review.DoctorId}, {request.Reason.Trim()}, N'OPEN', {DateTime.Now})");
        return Ok(new { success = true, message = "Appeal opened." });
    }

    public async Task<S4ActionResult> ListAppealsAsync(S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Appeal queue is for admin.");
        var rows = await _context.Database.SqlQuery<AppealRow>($@"
            SELECT ReviewAppealId, ReviewId, DoctorId, Reason, Status, Resolution, At
            FROM dbo.ReviewAppeal WHERE Status = N'OPEN' ORDER BY At").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> ResolveAppealAsync(int appealId, AppealResolveRequest request, S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Only admin can resolve an appeal.");
        var decision = (request?.Decision ?? "").Trim().ToUpperInvariant();
        if (decision is not ("UPHOLD" or "REMOVE"))
            return Fail(400, "VALIDATION", "Decision must be Uphold or Remove.");
        var appeal = await _context.Database.SqlQuery<AppealRow>($@"
            SELECT ReviewAppealId, ReviewId, DoctorId, Reason, Status, Resolution, At
            FROM dbo.ReviewAppeal WHERE ReviewAppealId = {appealId}").FirstOrDefaultAsync();
        if (appeal == null) return Fail(404, "NOT_FOUND", "Appeal not found.");
        if (!string.Equals(appeal.Status, "OPEN", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "This appeal is already closed.");
        var reviewStatus = decision == "REMOVE" ? "HIDDEN" : "APPROVED";
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.Review SET Status = {reviewStatus} WHERE ReviewId = {appeal.ReviewId}");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.ReviewAppeal
            SET Status = {decision}, Resolution = {request!.Note}
            WHERE ReviewAppealId = {appealId}");
        return Ok(new { success = true, data = new { appealId, decision, reviewStatus } });
    }

    public async Task<S4ActionResult> RankingExplainAsync(int doctorId)
    {
        if (doctorId <= 0) return Fail(400, "VALIDATION", "DoctorId is required.");
        var doctor = await _context.Doctors.AsNoTracking().FirstOrDefaultAsync(d => d.DoctorId == doctorId && !d.DeleteStatus);
        if (doctor == null) return Fail(404, "NOT_FOUND", "Doctor not found.");
        var weights = await _context.Database.SqlQuery<WeightRow>($@"
            SELECT TOP 1 VerifiedWeight, RatingWeight, FeeWeight FROM dbo.RankingWeight ORDER BY RankingWeightId").FirstOrDefaultAsync()
            ?? new WeightRow { VerifiedWeight = 40, RatingWeight = 40, FeeWeight = 20 };
        var reviews = await _context.Database.SqlQuery<ReviewRow>($@"
            SELECT ReviewId, PatientAppId, DoctorId, PatientId, Rating, Text, Status, At
            FROM dbo.Review WHERE DoctorId = {doctorId} AND Status = N'APPROVED'").ToListAsync();
        var average = reviews.Count == 0 ? 0 : reviews.Average(r => r.Rating);
        var fee = doctor.ConsultFeeInClinic ?? doctor.ConsultFeeTele ?? 0;
        var feeFactor = 1m - Math.Min(fee / 5000m, 1m);
        var verified = IsVerified(doctor.VerificationStatus) ? 1m : 0m;
        var score = Math.Round(weights.VerifiedWeight * verified + weights.RatingWeight * (decimal)(average / 5d) + weights.FeeWeight * feeFactor, 2);
        return Ok(new
        {
            success = true,
            data = new
            {
                doctorId,
                score,
                weights,
                parts = new
                {
                    verified,
                    averageRating = Math.Round(average, 2),
                    reviewCount = reviews.Count,
                    fee,
                    feeFactor = Math.Round(feeFactor, 4),
                    feeNote = "Lower fee scores higher. feeFactor = 1 - min(fee / 5000, 1)."
                }
            }
        });
    }

    public async Task<S4ActionResult> ListPotenciesAsync()
    {
        var rows = await _context.Database.SqlQuery<PotencyRow>($@"
            SELECT PotencyId, Code, SortOrder FROM dbo.PotencyMaster ORDER BY SortOrder").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> UpdateRemedyLineAsync(int prescriptionRemedyId, RemedyLineUpdateRequest request, S4Caller caller)
    {
        if (!caller.IsDoctor && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the treating doctor can update a remedy line.");
        if (request == null || request.PotencyId <= 0)
            return Fail(400, "VALIDATION", "PotencyId is required.");
        var potency = await _context.Database.SqlQuery<PotencyRow>($@"
            SELECT PotencyId, Code, SortOrder FROM dbo.PotencyMaster WHERE PotencyId = {request.PotencyId}").FirstOrDefaultAsync();
        if (potency == null) return Fail(400, "VALIDATION", "PotencyId is not in the potency list.");
        var line = await _context.PrescriptionRemedyDetails.AsNoTracking()
            .FirstOrDefaultAsync(r => r.PrescriptionRemedyId == prescriptionRemedyId && r.DeletedStatus != true);
        if (line == null) return Fail(404, "NOT_FOUND", "Remedy line not found.");
        var appointment = await LoadAppointmentAsync(line.AppointmentId);
        if (appointment == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (!caller.OwnsDoctor(appointment.DoctorId))
            return Fail(403, "FORBIDDEN", "This remedy line belongs to another clinic.");
        var signed = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.ErxSnapshot
            WHERE PatientAppId = {appointment.PatientAppId} AND Status = N'SIGNED'").FirstAsync();
        if (signed.Value > 0)
            return Fail(409, "CONFLICT", "A signed prescription cannot be edited.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PrescriptionRemedyDetail
            SET PotencyId = {request.PotencyId},
                Frequency = {TrimOrNull(request.Frequency, 80)},
                Duration = {TrimOrNull(request.Duration, 80)},
                Instructions = {TrimOrNull(request.Instructions, 500)}
            WHERE PrescriptionRemedyId = {prescriptionRemedyId}");
        return Ok(new { success = true, message = "Remedy line updated.", data = new { prescriptionRemedyId, potency.Code } });
    }

    public async Task<S4ActionResult> GetErxByAppointmentAsync(int patientAppId, S4Caller caller, bool patientView)
    {
        var snapshot = await LoadSnapshotByAppointmentAsync(patientAppId);
        if (snapshot == null) return Fail(404, "NOT_FOUND", "No signed prescription for this appointment.");
        var access = await EnsureSnapshotAccessAsync(snapshot, caller, patientView);
        if (access != null) return access;
        var items = await LoadSnapshotItemsAsync(snapshot.ErxSnapshotId, revealNames: !patientView);
        return Ok(new { success = true, data = new { snapshot.ErxSnapshotId, snapshot.PatientAppId, snapshot.Status, snapshot.SignedAt, items, notesIncluded = false } });
    }

    public async Task<S4ActionResult> SignErxAsync(SignErxRequest request, S4Caller caller)
    {
        if (!caller.IsDoctor && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the treating doctor can sign.");
        if (request == null || request.PatientAppId <= 0)
            return Fail(400, "VALIDATION", "PatientAppId is required.");
        var appointment = await LoadAppointmentAsync(request.PatientAppId);
        if (appointment == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (!caller.OwnsDoctor(appointment.DoctorId))
            return Fail(403, "FORBIDDEN", "This visit belongs to another clinic.");
        var existing = await LoadSnapshotByAppointmentAsync(appointment.PatientAppId);
        if (existing != null && existing.Status == "SIGNED")
            return Fail(409, "CONFLICT", "This visit is already signed.");

        var lines = await _context.Database.SqlQuery<RemedyLineRow>($@"
            SELECT r.PrescriptionRemedyId, r.RemedyId, m.RemedyName, r.Dose, p.Code AS PotencyCode, r.Frequency, r.Duration, r.Instructions
            FROM dbo.PrescriptionRemedyDetail r
            INNER JOIN dbo.RemedyMaster m ON m.RemedyId = r.RemedyId
            LEFT JOIN dbo.PotencyMaster p ON p.PotencyId = r.PotencyId
            WHERE r.AppointmentId = {appointment.PatientAppId} AND ISNULL(r.DeletedStatus, 0) = 0").ToListAsync();
        if (lines.Count == 0)
            return Fail(400, "VALIDATION", "Add at least one remedy before signing.");
        if (lines.Any(l => string.IsNullOrWhiteSpace(l.PotencyCode)))
            return Fail(400, "VALIDATION", "Every remedy line needs a potency before signing.");

        var labs = await _context.PatientLabOrders.AsNoTracking()
            .Where(l => l.PatientId == appointment.PatientId && !l.DeleteStatus)
            .Select(l => new { l.PatientOrderedTestId, l.LabName })
            .ToListAsync();
        var labJson = JsonSerializer.Serialize(labs);
        var snapshotId = existing?.ErxSnapshotId ?? 0;
        if (snapshotId == 0)
        {
            snapshotId = (int)await InsertAsync(
                @"INSERT INTO dbo.ErxSnapshot (PatientAppId, PatientId, DoctorId, Status, SignedAt, SignedBy, LabOrdersJson)
                  VALUES (@App, @Patient, @Doctor, N'SIGNED', @At, @By, @Labs);
                  SELECT CAST(SCOPE_IDENTITY() AS bigint);",
                P("@App", appointment.PatientAppId),
                P("@Patient", appointment.PatientId),
                P("@Doctor", appointment.DoctorId),
                P("@At", DateTime.Now),
                P("@By", caller.UserId),
                P("@Labs", labJson, -1));
        }
        else
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE dbo.ErxSnapshot
                SET Status = N'SIGNED', SignedAt = {DateTime.Now}, SignedBy = {caller.UserId}, LabOrdersJson = {labJson}
                WHERE ErxSnapshotId = {snapshotId}");
            await _context.Database.ExecuteSqlInterpolatedAsync($@"DELETE FROM dbo.ErxSnapshotItem WHERE ErxSnapshotId = {snapshotId}");
        }

        var sequence = 1;
        foreach (var line in lines)
        {
            var code = "R" + sequence.ToString("D4");
            sequence++;
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.ErxSnapshotItem
                    (ErxSnapshotId, RemedyId, RemedyCode, RemedyName, PotencyCode, Dose, Frequency, Duration, Instructions)
                VALUES
                    ({snapshotId}, {line.RemedyId}, {code}, {line.RemedyName}, {line.PotencyCode}, {line.Dose}, {line.Frequency}, {line.Duration}, {line.Instructions})");
        }
        return Ok(new { success = true, message = "Prescription signed and locked.", data = new { erxSnapshotId = snapshotId } });
    }

    public async Task<S4ActionResult> ErxHistoryAsync(int? patientId, int? patientAppId, S4Caller caller)
    {
        if (caller.IsPatient)
            patientId = caller.PatientId;
        if (patientId is null or <= 0 && patientAppId is null or <= 0)
            return Fail(400, "VALIDATION", "Provide a patient or appointment id.");
        if (caller.IsDoctor && patientId.HasValue)
        {
            var owns = await _context.PatientAppointments.AsNoTracking()
                .AnyAsync(a => a.PatientId == patientId && a.DoctorId == caller.DoctorId && a.DeleteStatus != true);
            if (!owns && !caller.IsAdmin)
                return Fail(403, "FORBIDDEN", "This patient is not on your list.");
        }
        var rows = await _context.Database.SqlQuery<SnapshotRow>($@"
            SELECT ErxSnapshotId, PatientAppId, PatientId, DoctorId, Status, SignedAt
            FROM dbo.ErxSnapshot
            WHERE Status = N'SIGNED'
              AND ({patientId ?? 0} = 0 OR PatientId = {patientId ?? 0})
              AND ({patientAppId ?? 0} = 0 OR PatientAppId = {patientAppId ?? 0})
            ORDER BY SignedAt DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> ErxPdfAsync(int erxId, S4Caller caller)
    {
        var snapshot = await LoadSnapshotAsync(erxId);
        if (snapshot == null) return Fail(404, "NOT_FOUND", "Prescription not found.");
        var access = await EnsureSnapshotAccessAsync(snapshot, caller, caller.IsPatient);
        if (access != null) return access;
        if (snapshot.Status != "SIGNED")
            return Fail(409, "CONFLICT", "Only a signed prescription can be printed.");
        var reveal = !caller.IsPatient || await PharmacyAcceptedAsync(snapshot.ErxSnapshotId);
        var items = await LoadSnapshotItemsAsync(snapshot.ErxSnapshotId, reveal);
        var lines = items.Select(i => (reveal ? i.RemedyName : i.RemedyCode) + " " + i.PotencyCode + " " + i.Dose);
        var pdf = Helpers.ClinicalCasePdfBuilder.Build("Signed prescription " + snapshot.ErxSnapshotId, lines);
        var stored = await StorePdfAsync(pdf, "erx-" + snapshot.ErxSnapshotId + ".pdf", "Erx", snapshot.ErxSnapshotId, caller.UserId);
        return Ok(new { success = true, data = new { stored.DocumentId, stored.Path } });
    }

    public async Task<S4ActionResult> RequestRefillAsync(RefillCreateRequest request, S4Caller caller)
    {
        if (!caller.IsPatient || caller.PatientId is null)
            return Fail(403, "FORBIDDEN", "Only a patient can request a refill.");
        if (request == null || request.ErxSnapshotId <= 0)
            return Fail(400, "VALIDATION", "ErxSnapshotId is required.");
        var snapshot = await LoadSnapshotAsync(request.ErxSnapshotId);
        if (snapshot == null || snapshot.Status != "SIGNED")
            return Fail(404, "NOT_FOUND", "Signed prescription not found.");
        if (snapshot.PatientId != caller.PatientId)
            return Fail(403, "FORBIDDEN", "This prescription belongs to another patient.");
        var id = await InsertAsync(
            @"INSERT INTO dbo.RefillRequest (ErxSnapshotId, PatientId, DoctorId, Status, CreatedAt)
              VALUES (@Erx, @Patient, @Doctor, N'PENDING', @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@Erx", snapshot.ErxSnapshotId),
            P("@Patient", snapshot.PatientId),
            P("@Doctor", snapshot.DoctorId),
            P("@At", DateTime.Now));
        return Ok(new { success = true, data = new { refillRequestId = id, status = "PENDING" } });
    }

    public async Task<S4ActionResult> ListRefillsAsync(S4Caller caller)
    {
        if (!caller.IsDoctor && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Refill inbox is for the treating doctor.");
        var doctorId = caller.DoctorId ?? 0;
        var rows = await _context.Database.SqlQuery<RefillRow>($@"
            SELECT RefillRequestId, ErxSnapshotId, PatientId, DoctorId, Status, Reason, CreatedAt
            FROM dbo.RefillRequest
            WHERE ({doctorId} = 0 OR DoctorId = {doctorId}) AND Status = N'PENDING'
            ORDER BY CreatedAt").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> DecideRefillAsync(int refillId, bool approve, string? reason, S4Caller caller)
    {
        if (!caller.IsDoctor && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the treating doctor can decide a refill.");
        if (!approve && string.IsNullOrWhiteSpace(reason))
            return Fail(400, "VALIDATION", "A reject reason is required.");
        var row = await _context.Database.SqlQuery<RefillRow>($@"
            SELECT RefillRequestId, ErxSnapshotId, PatientId, DoctorId, Status, Reason, CreatedAt
            FROM dbo.RefillRequest WHERE RefillRequestId = {refillId}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Refill request not found.");
        if (!caller.OwnsDoctor(row.DoctorId))
            return Fail(403, "FORBIDDEN", "This refill is for another clinic.");
        if (!string.Equals(row.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "This refill is already decided.");
        var status = approve ? "APPROVED" : "REJECTED";
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.RefillRequest
            SET Status = {status}, Reason = {reason}, DecidedAt = {DateTime.Now}
            WHERE RefillRequestId = {refillId}");
        return Ok(new { success = true, data = new { refillId, status } });
    }

    public async Task<S4ActionResult> OnboardPharmacyAsync(PharmacyOnboardRequest request, S4Caller caller)
    {
        if (request == null) return Fail(400, "VALIDATION", "Body is required.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 2)
            return Fail(400, "VALIDATION", "Pharmacy name is required.");
        var mobile = Digits(request.Mobile);
        if (mobile.Length is < 8 or > 15)
            return Fail(400, "VALIDATION", "A valid mobile is required.");
        if (string.IsNullOrWhiteSpace(request.LicenceNumber) || request.LicenceNumber.Trim().Length < 3)
            return Fail(400, "VALIDATION", "Licence number is required.");
        if (request.ExpiryDate == null || request.ExpiryDate.Value.Date < DateTime.Today)
            return Fail(400, "VALIDATION", "Licence expiry must be today or later.");
        if (!caller.IsAdmin && !caller.IsPharmacy)
            return Fail(403, "FORBIDDEN", "Pharmacy onboarding is for a pharmacy partner or admin.");

        var id = (int)await InsertAsync(
            @"INSERT INTO dbo.PharmacyPartner (UserId, Name, Mobile, Area, Status, CreatedAt)
              VALUES (@User, @Name, @Mobile, @Area, N'PENDING', @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@User", caller.IsPharmacy ? caller.UserId : null),
            P("@Name", request.Name.Trim(), 200),
            P("@Mobile", mobile, 20),
            P("@Area", TrimOrNull(request.Area, 120), 120),
            P("@At", DateTime.Now));
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.PharmacyLicence (PharmacyPartnerId, LicenceNumber, ExpiryDate, Status)
            VALUES ({id}, {request.LicenceNumber.Trim()}, {request.ExpiryDate.Value.Date}, N'ACTIVE')");
        return Ok(new { success = true, message = "Pharmacy stored as pending until admin activation.", data = new { pharmacyPartnerId = id, status = "PENDING" } });
    }

    public async Task<S4ActionResult> ActivatePharmacyAsync(int pharmacyId, S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Only admin can activate a pharmacy.");
        var count = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.PharmacyPartner WHERE PharmacyPartnerId = {pharmacyId}").FirstAsync();
        if (count.Value == 0) return Fail(404, "NOT_FOUND", "Pharmacy not found.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PharmacyPartner SET Status = N'ACTIVE' WHERE PharmacyPartnerId = {pharmacyId}");
        return Ok(new { success = true, data = new { pharmacyId, status = "ACTIVE" } });
    }

    public async Task<S4ActionResult> SweepLicencesAsync(S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Licence sweep is for admin.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PharmacyLicence SET Status = N'EXPIRED' WHERE ExpiryDate < CONVERT(date, GETDATE()) AND Status = N'ACTIVE'");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.PharmacyPartner SET Status = N'SUSPENDED'
            WHERE PharmacyPartnerId IN (
                SELECT PharmacyPartnerId FROM dbo.PharmacyLicence WHERE Status = N'EXPIRED')
              AND Status = N'ACTIVE'");
        return Ok(new { success = true, message = "Expired licences are suspended and excluded from seller routing." });
    }

    public async Task<S4ActionResult> ListSellersAsync(string? area)
    {
        var filter = (area ?? "").Trim();
        var rows = await _context.Database.SqlQuery<SellerRow>($@"
            SELECT p.PharmacyPartnerId, p.Name, p.Area, p.Status
            FROM dbo.PharmacyPartner p
            WHERE p.Status = N'ACTIVE'
              AND EXISTS (
                    SELECT 1 FROM dbo.PharmacyLicence l
                    WHERE l.PharmacyPartnerId = p.PharmacyPartnerId AND l.Status = N'ACTIVE' AND l.ExpiryDate >= CONVERT(date, GETDATE()))
              AND ({filter} = N'' OR p.Area = {filter})
              AND NOT EXISTS (
                    SELECT 1 FROM dbo.SellerRoutingRule r
                    WHERE r.PharmacyPartnerId = p.PharmacyPartnerId
                      AND r.OpenTime IS NOT NULL AND r.CloseTime IS NOT NULL
                      AND (CONVERT(time, GETDATE()) < r.OpenTime OR CONVERT(time, GETDATE()) > r.CloseTime))").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> CreateMedicineOrderAsync(MedicineOrderCreateRequest request, S4Caller caller)
    {
        if (!caller.IsPatient || caller.PatientId is null)
            return Fail(403, "FORBIDDEN", "Only a patient can start a medicine order.");
        if (request == null || request.ErxSnapshotId <= 0)
            return Fail(400, "VALIDATION", "ErxSnapshotId is required.");
        var snapshot = await LoadSnapshotAsync(request.ErxSnapshotId);
        if (snapshot == null || snapshot.Status != "SIGNED")
            return Fail(404, "NOT_FOUND", "Signed prescription not found.");
        if (snapshot.PatientId != caller.PatientId)
            return Fail(403, "FORBIDDEN", "This prescription belongs to another patient.");
        var id = (int)await InsertAsync(
            @"INSERT INTO dbo.MedicineOrder (ErxSnapshotId, PatientId, PharmacyPartnerId, Status, ConsentGranted, CreatedAt)
              VALUES (@Erx, @Patient, @Pharmacy, N'OFFERED', 0, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@Erx", snapshot.ErxSnapshotId),
            P("@Patient", snapshot.PatientId),
            P("@Pharmacy", request.PharmacyPartnerId),
            P("@At", DateTime.Now));
        var items = await LoadSnapshotItemsAsync(snapshot.ErxSnapshotId, revealNames: true);
        foreach (var item in items)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.MedicineOrderItem (MedicineOrderId, RemedyCode, RemedyName)
                VALUES ({id}, {item.RemedyCode}, {item.RemedyName})");
        }
        await AddMedicineEventAsync(id, "OFFERED", "Order created from signed prescription");
        return Ok(new { success = true, data = new { medicineOrderId = id, status = "OFFERED", namesRevealed = false } });
    }

    public async Task<S4ActionResult> GrantMedicineConsentAsync(int orderId, S4Caller caller)
    {
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (caller.PatientId != order.PatientId) return Fail(403, "FORBIDDEN", "Only the patient can grant consent.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.MedicineOrder SET ConsentGranted = 1 WHERE MedicineOrderId = {orderId}");
        return Ok(new { success = true, message = "Consent granted for pharmacy fulfilment." });
    }

    public async Task<S4ActionResult> RequestMedicineAcceptOtpAsync(int orderId, S4Caller caller)
    {
        if (!caller.IsPharmacy && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the pharmacy can request an accept OTP.");
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (!string.Equals(order.Status, "OFFERED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Accept OTP is only for an offered order.");
        var code = await IssueOtpAsync("PharmacyAccept", "MedicineOrder", orderId.ToString(), caller.UserId);
        return Ok(new { success = true, message = "OTP created for pharmacy accept.", devCode = code });
    }

    public async Task<S4ActionResult> AcceptMedicineOrderAsync(PharmacyAcceptRequest request, S4Caller caller)
    {
        if (!caller.IsPharmacy && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the pharmacy can accept.");
        if (request == null || !IsOtpShape(request.Otp))
            return Fail(400, "VALIDATION", "A 6-digit OTP is required.");
        var order = await LoadMedicineAsync(request.MedicineOrderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (!order.ConsentGranted)
            return Fail(409, "CONFLICT", "Patient consent is required before accept.");
        if (!string.Equals(order.Status, "OFFERED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Only an offered order can be accepted.");
        var pharmacyId = await CallerPharmacyIdAsync(caller);
        if (!caller.IsAdmin && pharmacyId != order.PharmacyPartnerId)
            return Fail(403, "FORBIDDEN", "This order is assigned to another pharmacy.");
        var otp = await ConsumeOtpAsync("PharmacyAccept", "MedicineOrder", order.MedicineOrderId.ToString(), request.Otp);
        if (otp != null) return otp;
        await SetMedicineStatusAsync(order.MedicineOrderId, "ACCEPTED", "Accepted with OTP");
        var items = await _context.Database.SqlQuery<OrderItemRow>($@"
            SELECT RemedyCode, RemedyName FROM dbo.MedicineOrderItem WHERE MedicineOrderId = {order.MedicineOrderId}").ToListAsync();
        return Ok(new { success = true, message = "Names are revealed after OTP accept.", data = items });
    }

    public async Task<S4ActionResult> RejectMedicineOrderAsync(int orderId, MedicineRejectRequest request, S4Caller caller)
    {
        if (!caller.IsPharmacy && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the pharmacy can reject.");
        var reason = (request?.Reason ?? "").Trim().ToUpperInvariant();
        if (reason is not ("OUT_OF_STOCK" or "CLOSED" or "OTHER"))
            return Fail(400, "VALIDATION", "Reason must be OUT_OF_STOCK, CLOSED, or OTHER.");
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (order.Status is not ("OFFERED" or "ACCEPTED"))
            return Fail(409, "CONFLICT", "This order can no longer be rejected.");
        await SetMedicineStatusAsync(orderId, "REJECTED", reason);
        await AddExceptionAsync(null, "MEDICINE_" + reason, "Medicine order " + orderId + " rejected.");
        return Ok(new { success = true, data = new { orderId, status = "REJECTED", reason } });
    }

    public async Task<S4ActionResult> QuoteMedicineOrderAsync(int orderId, MedicineQuoteRequest request, S4Caller caller)
    {
        if (!caller.IsPharmacy && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the pharmacy can quote.");
        if (request == null || request.Amount <= 0 || request.Amount > 1000000)
            return Fail(400, "VALIDATION", "Quote amount must be greater than 0 and at most 1000000.");
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (!string.Equals(order.Status, "ACCEPTED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "Quote is allowed only after accept.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.MedicineQuote (MedicineOrderId, Amount, Note, Status, At)
            VALUES ({orderId}, {request.Amount}, {request.Note}, N'OPEN', {DateTime.Now})");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.MedicineOrder SET QuoteAmount = {request.Amount}, Status = N'QUOTED' WHERE MedicineOrderId = {orderId}");
        await AddMedicineEventAsync(orderId, "QUOTED", request.Amount.ToString("0.00"));
        return Ok(new { success = true, data = new { orderId, status = "QUOTED", amount = request.Amount } });
    }

    public async Task<S4ActionResult> AcceptQuoteAsync(int orderId, S4Caller caller)
    {
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (caller.PatientId != order.PatientId) return Fail(403, "FORBIDDEN", "Only the patient can accept the quote.");
        if (!string.Equals(order.Status, "QUOTED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "There is no open quote to accept.");
        await SetMedicineStatusAsync(orderId, "QUOTED_ACCEPTED", "Patient accepted quote");
        return Ok(new { success = true, data = new { orderId, status = "QUOTED_ACCEPTED" } });
    }

    public async Task<S4ActionResult> MarkMedicineReadyAsync(int orderId, S4Caller caller)
        => await MoveMedicineAsync(orderId, caller, "PAID", "COD_PENDING", "READY");

    public async Task<S4ActionResult> DispatchMedicineAsync(int orderId, S4Caller caller)
        => await MoveMedicineAsync(orderId, caller, "READY", null, "DISPATCHED");

    public async Task<S4ActionResult> MedicineTrackingAsync(int orderId, S4Caller caller)
    {
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (!caller.IsAdmin && caller.PatientId != order.PatientId && !caller.IsPharmacy)
            return Fail(403, "FORBIDDEN", "You cannot track this order.");
        var reveal = !string.Equals(order.Status, "OFFERED", StringComparison.OrdinalIgnoreCase);
        var items = await _context.Database.SqlQuery<OrderItemRow>($@"
            SELECT RemedyCode, RemedyName FROM dbo.MedicineOrderItem WHERE MedicineOrderId = {orderId}").ToListAsync();
        var events = await _context.Database.SqlQuery<MedicineEventRow>($@"
            SELECT Status, Detail, At FROM dbo.MedicineOrderEvent WHERE MedicineOrderId = {orderId} ORDER BY At").ToListAsync();
        return Ok(new
        {
            success = true,
            data = new
            {
                order.MedicineOrderId,
                order.Status,
                items = items.Select(i => new { i.RemedyCode, remedyName = reveal ? i.RemedyName : null }),
                events
            }
        });
    }

    public async Task<S4ActionResult> PatientMedicineOrdersAsync(S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        var patientId = caller.PatientId.Value;
        var rows = await _context.Database.SqlQuery<MedicineRow>($@"
            SELECT MedicineOrderId, ErxSnapshotId, PatientId, PharmacyPartnerId, Status, ConsentGranted, QuoteAmount, PayMode
            FROM dbo.MedicineOrder WHERE PatientId = {patientId} ORDER BY MedicineOrderId DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> CloneRefillOrderAsync(int refillId, S4Caller caller)
    {
        if (!caller.IsPatient) return Fail(403, "FORBIDDEN", "The patient starts the refill order.");
        var refill = await _context.Database.SqlQuery<RefillRow>($@"
            SELECT RefillRequestId, ErxSnapshotId, PatientId, DoctorId, Status, Reason, CreatedAt
            FROM dbo.RefillRequest WHERE RefillRequestId = {refillId}").FirstOrDefaultAsync();
        if (refill == null) return Fail(404, "NOT_FOUND", "Refill not found.");
        if (!string.Equals(refill.Status, "APPROVED", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "The doctor has not approved this refill.");
        if (refill.PatientId != caller.PatientId)
            return Fail(403, "FORBIDDEN", "This refill belongs to another patient.");
        return await CreateMedicineOrderAsync(new MedicineOrderCreateRequest { ErxSnapshotId = refill.ErxSnapshotId }, caller);
    }

    public async Task<S4ActionResult> ListMedicineExceptionsAsync(S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Medicine exceptions are for admin.");
        var rows = await _context.Database.SqlQuery<ExceptionRow>($@"
            SELECT PaymentExceptionId, PaymentOrderId, Kind, Detail, Status, CreatedAt
            FROM dbo.PaymentException
            WHERE Kind LIKE N'MEDICINE_%' AND Status = N'OPEN'
            ORDER BY CreatedAt DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> RerouteMedicineAsync(int orderId, int pharmacyId, S4Caller caller)
    {
        if (!caller.IsAdmin) return Fail(403, "FORBIDDEN", "Reroute is for admin.");
        if (pharmacyId <= 0) return Fail(400, "VALIDATION", "Pharmacy id is required.");
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (order.Status is not ("REJECTED" or "OFFERED"))
            return Fail(409, "CONFLICT", "Only an offered or rejected order can be rerouted.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.MedicineOrder SET PharmacyPartnerId = {pharmacyId}, Status = N'OFFERED' WHERE MedicineOrderId = {orderId}");
        await AddMedicineEventAsync(orderId, "OFFERED", "Rerouted to pharmacy " + pharmacyId);
        return Ok(new { success = true, data = new { orderId, pharmacyId, status = "OFFERED" } });
    }

    public async Task<S4ActionResult> SaveRoutingAsync(RoutingRuleRequest request, S4Caller caller)
    {
        if (!caller.IsAdmin && !caller.IsPharmacy)
            return Fail(403, "FORBIDDEN", "Routing rules are for admin or the pharmacy.");
        if (request == null || request.PharmacyPartnerId <= 0)
            return Fail(400, "VALIDATION", "PharmacyPartnerId is required.");
        if (request.Capacity is < 1 or > 500)
            return Fail(400, "VALIDATION", "Capacity must be from 1 to 500.");
        TimeSpan? open = ParseTime(request.OpenTime);
        TimeSpan? close = ParseTime(request.CloseTime);
        if ((request.OpenTime != null && open == null) || (request.CloseTime != null && close == null))
            return Fail(400, "VALIDATION", "OpenTime and CloseTime must be HH:mm.");
        if (open.HasValue && close.HasValue && close <= open)
            return Fail(400, "VALIDATION", "CloseTime must be after OpenTime.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.SellerRoutingRule (PharmacyPartnerId, Area, OpenTime, CloseTime, Capacity)
            VALUES ({request.PharmacyPartnerId}, {request.Area}, {open}, {close}, {request.Capacity})");
        return Ok(new { success = true, message = "Routing rule saved." });
    }

    public async Task<S4ActionResult> TimelineAsync(int? patientId, S4Caller caller)
    {
        var owned = await ResolvePatientAsync(patientId, caller, write: false);
        if (owned.Error != null) return owned.Error;
        var id = owned.PatientId;
        var appointments = await _context.PatientAppointments.AsNoTracking()
            .Where(a => a.PatientId == id && a.DeleteStatus != true)
            .OrderByDescending(a => a.AppointmentDate)
            .Take(50)
            .Select(a => new { kind = "appointment", at = a.AppointmentDate, refId = a.PatientAppId, title = a.Status })
            .ToListAsync();
        var erx = await _context.Database.SqlQuery<SnapshotRow>($@"
            SELECT ErxSnapshotId, PatientAppId, PatientId, DoctorId, Status, SignedAt
            FROM dbo.ErxSnapshot WHERE PatientId = {id} AND Status = N'SIGNED'").ToListAsync();
        return Ok(new { success = true, data = new { appointments, prescriptions = erx } });
    }

    public async Task<S4ActionResult> ConsultationNoteAsync(int patientAppId, S4Caller caller)
    {
        var appointment = await LoadAppointmentAsync(patientAppId);
        if (appointment == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        var access = await EnsureAppointmentAccessAsync(appointment, caller);
        if (access != null) return access;
        var note = await _context.Database.SqlQuery<NoteRow>($@"
            SELECT TOP 1 Text, At FROM dbo.ConsultationSummary
            WHERE PatientAppId = {patientAppId} ORDER BY At DESC").FirstOrDefaultAsync();
        if (note == null) return Fail(404, "NOT_FOUND", "No consultation note for this visit.");
        return Ok(new { success = true, data = note });
    }

    public async Task<S4ActionResult> SavePatientDocumentAsync(IFormFile file, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        if (file == null || file.Length == 0) return Fail(400, "VALIDATION", "File is required.");
        if (file.Length > 10_000_000) return Fail(400, "VALIDATION", "File must be 10 MB or smaller.");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".pdf" or ".jpg" or ".jpeg" or ".png"))
            return Fail(400, "VALIDATION", "Allowed files are pdf, jpg, jpeg, and png.");
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        var bytes = memory.ToArray();
        var stored = await StorePdfAsync(bytes, Path.GetFileName(file.FileName), "Patient", caller.PatientId.Value, caller.UserId, file.ContentType);
        return Ok(new { success = true, data = new { stored.DocumentId, stored.Path } });
    }

    public async Task<S4ActionResult> SetFollowUpAsync(FollowUpCreateRequest request, S4Caller caller)
    {
        if (!caller.IsDoctor && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the treating doctor can set a follow-up.");
        if (request == null || request.PatientAppId <= 0 || string.IsNullOrWhiteSpace(request.Title))
            return Fail(400, "VALIDATION", "PatientAppId and title are required.");
        if (request.DueDate == null) return Fail(400, "VALIDATION", "DueDate is required.");
        var appointment = await LoadAppointmentAsync(request.PatientAppId);
        if (appointment == null) return Fail(404, "NOT_FOUND", "Appointment not found.");
        if (!caller.OwnsDoctor(appointment.DoctorId))
            return Fail(403, "FORBIDDEN", "This visit belongs to another clinic.");
        var planId = (int)await InsertAsync(
            @"INSERT INTO dbo.FollowUpPlan (PatientAppId, PatientId, DoctorId, Note, CreatedAt)
              VALUES (@App, @Patient, @Doctor, @Note, @At);
              SELECT CAST(SCOPE_IDENTITY() AS bigint);",
            P("@App", appointment.PatientAppId),
            P("@Patient", appointment.PatientId),
            P("@Doctor", appointment.DoctorId),
            P("@Note", TrimOrNull(request.Note, 1000), 1000),
            P("@At", DateTime.Now));
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.FollowUpTask (FollowUpPlanId, Title, DueDate, Status)
            VALUES ({planId}, {request.Title.Trim()}, {request.DueDate.Value.Date}, N'OPEN')");
        return Ok(new { success = true, data = new { followUpPlanId = planId } });
    }

    public async Task<S4ActionResult> ListFollowUpsAsync(int? patientId, S4Caller caller)
    {
        var owned = await ResolvePatientAsync(patientId, caller, write: false);
        if (owned.Error != null) return owned.Error;
        var id = owned.PatientId;
        var rows = await _context.Database.SqlQuery<FollowUpRow>($@"
            SELECT t.FollowUpTaskId, t.Title, t.DueDate, t.Status, p.PatientId, p.DoctorId
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            WHERE p.PatientId = {id}
            ORDER BY t.DueDate").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> CompleteFollowUpAsync(int taskId, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "Only the patient can complete a task.");
        var row = await _context.Database.SqlQuery<FollowUpRow>($@"
            SELECT t.FollowUpTaskId, t.Title, t.DueDate, t.Status, p.PatientId, p.DoctorId
            FROM dbo.FollowUpTask t
            INNER JOIN dbo.FollowUpPlan p ON p.FollowUpPlanId = t.FollowUpPlanId
            WHERE t.FollowUpTaskId = {taskId}").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Follow-up task not found.");
        if (row.PatientId != caller.PatientId) return Fail(403, "FORBIDDEN", "This task belongs to another patient.");
        if (string.Equals(row.Status, "DONE", StringComparison.OrdinalIgnoreCase))
            return Fail(409, "CONFLICT", "This task is already complete.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.FollowUpTask SET Status = N'DONE', CompletedAt = {DateTime.Now} WHERE FollowUpTaskId = {taskId}");
        return Ok(new { success = true, data = new { taskId, status = "DONE" } });
    }

    public async Task<S4ActionResult> SaveDiaryAsync(DiaryWriteRequest request, S4Caller caller)
        => await WriteDiaryAsync(null, request, caller);

    public async Task<S4ActionResult> UpdateDiaryAsync(int diaryId, DiaryWriteRequest request, S4Caller caller)
        => await WriteDiaryAsync(diaryId, request, caller);

    public async Task<S4ActionResult> ListDiaryAsync(int? patientId, S4Caller caller)
    {
        var owned = await ResolvePatientAsync(patientId, caller, write: false);
        if (owned.Error != null) return owned.Error;
        var id = owned.PatientId;
        var rows = await _context.Database.SqlQuery<DiaryRow>($@"
            SELECT SymptomDiaryId, PatientId, EntryDate, Severity, Note
            FROM dbo.SymptomDiary
            WHERE PatientId = {id} AND DeleteStatus = 0
            ORDER BY EntryDate DESC").ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> DeleteDiaryAsync(int diaryId, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "Only the patient can delete a diary entry.");
        var row = await _context.Database.SqlQuery<DiaryRow>($@"
            SELECT SymptomDiaryId, PatientId, EntryDate, Severity, Note
            FROM dbo.SymptomDiary WHERE SymptomDiaryId = {diaryId} AND DeleteStatus = 0").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Diary entry not found.");
        if (row.PatientId != caller.PatientId) return Fail(403, "FORBIDDEN", "This entry belongs to another patient.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.SymptomDiary SET DeleteStatus = 1 WHERE SymptomDiaryId = {diaryId}");
        return Ok(new { success = true, message = "Diary entry removed." });
    }

    public async Task<S4ActionResult> ProgressAsync(int? patientId, S4Caller caller)
    {
        var owned = await ResolvePatientAsync(patientId, caller, write: false);
        if (owned.Error != null) return owned.Error;
        var id = owned.PatientId;
        var diary = await _context.Database.SqlQuery<DiaryRow>($@"
            SELECT SymptomDiaryId, PatientId, EntryDate, Severity, Note
            FROM dbo.SymptomDiary
            WHERE PatientId = {id} AND DeleteStatus = 0
            ORDER BY EntryDate").ToListAsync();
        var visits = await _context.PatientAppointments.AsNoTracking()
            .CountAsync(a => a.PatientId == id && a.DeleteStatus != true && a.Status != "CANCELLED");
        return Ok(new
        {
            success = true,
            data = new
            {
                visitCount = visits,
                series = diary.Select(d => new { date = d.EntryDate, severity = d.Severity })
            }
        });
    }

    public async Task<S4ActionResult> ListConsentsAsync(S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        var patientId = (long)caller.PatientId.Value;
        var rows = await _context.ConsentRecords.AsNoTracking()
            .Where(c => c.SubjectType == "Patient" && c.SubjectId == patientId)
            .OrderByDescending(c => c.GrantedAt)
            .Select(c => new { c.ConsentRecordId, c.ConsentTypeId, c.GrantedAt, c.WithdrawnAt })
            .ToListAsync();
        return Ok(new { success = true, data = rows });
    }

    public async Task<S4ActionResult> WithdrawConsentAsync(long consentId, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        var row = await _context.ConsentRecords.FirstOrDefaultAsync(c => c.ConsentRecordId == consentId);
        if (row == null) return Fail(404, "NOT_FOUND", "Consent record not found.");
        if (row.SubjectType != "Patient" || row.SubjectId != caller.PatientId)
            return Fail(403, "FORBIDDEN", "This consent belongs to another patient.");
        if (row.WithdrawnAt != null) return Fail(409, "CONFLICT", "This consent is already withdrawn.");
        row.WithdrawnAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Consent withdrawn." });
    }

    public async Task<S4ActionResult> CreateDataRequestAsync(DataRequestCreate request, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        var type = (request?.RequestType ?? "").Trim().ToUpperInvariant();
        if (type is not ("EXPORT" or "DELETE"))
            return Fail(400, "VALIDATION", "RequestType must be EXPORT or DELETE.");
        var patientId = caller.PatientId.Value;
        var open = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.DataRequest
            WHERE PatientId = {patientId} AND RequestType = {type} AND Status = N'OPEN'").FirstAsync();
        if (open.Value > 0)
            return Fail(409, "CONFLICT", "An open request of this type already exists.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.DataRequest (PatientId, RequestType, Status, CreatedAt)
            VALUES ({patientId}, {type}, N'OPEN', {DateTime.Now})");
        return Ok(new { success = true, message = "Request recorded. It is not executed automatically.", data = new { type, status = "OPEN" } });
    }

    public async Task<S4ActionResult> GetHealthProfileAsync(S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        var patientId = caller.PatientId.Value;
        var row = await _context.Database.SqlQuery<HealthRow>($@"
            SELECT PatientId, BloodGroup, Allergies, ChronicConditions, EmergencyContactName, EmergencyContactMobile
            FROM dbo.PatientHealthBasics WHERE PatientId = {patientId}").FirstOrDefaultAsync();
        return Ok(new { success = true, data = row });
    }

    public async Task<S4ActionResult> UpdateHealthProfileAsync(HealthProfileUpdate request, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "A patient profile is required.");
        if (request == null) return Fail(400, "VALIDATION", "Body is required.");
        var group = string.IsNullOrWhiteSpace(request.BloodGroup) ? null : request.BloodGroup.Trim();
        if (group != null && !BloodGroups.Contains(group))
            return Fail(400, "VALIDATION", "Blood group must be one of O+, O-, A+, A-, B+, B-, AB+, AB-, Unknown.");
        var mobile = string.IsNullOrWhiteSpace(request.EmergencyContactMobile) ? null : Digits(request.EmergencyContactMobile);
        if (mobile != null && mobile.Length is < 8 or > 15)
            return Fail(400, "VALIDATION", "Emergency mobile must be 8 to 15 digits.");
        var patientId = caller.PatientId.Value;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            MERGE dbo.PatientHealthBasics AS t
            USING (SELECT {patientId} AS PatientId) AS s ON t.PatientId = s.PatientId
            WHEN MATCHED THEN UPDATE SET
                BloodGroup = {group}, Allergies = {TrimOrNull(request.Allergies, 500)},
                ChronicConditions = {TrimOrNull(request.ChronicConditions, 500)},
                EmergencyContactName = {TrimOrNull(request.EmergencyContactName, 120)},
                EmergencyContactMobile = {mobile}, UpdatedAt = {DateTime.Now}
            WHEN NOT MATCHED THEN INSERT
                (PatientId, BloodGroup, Allergies, ChronicConditions, EmergencyContactName, EmergencyContactMobile, UpdatedAt)
            VALUES
                ({patientId}, {group}, {TrimOrNull(request.Allergies, 500)}, {TrimOrNull(request.ChronicConditions, 500)},
                 {TrimOrNull(request.EmergencyContactName, 120)}, {mobile}, {DateTime.Now});");
        return await GetHealthProfileAsync(caller);
    }

    private async Task<S4ActionResult> WriteDiaryAsync(int? diaryId, DiaryWriteRequest request, S4Caller caller)
    {
        if (caller.PatientId is null) return Fail(403, "FORBIDDEN", "Only the patient can write the diary.");
        if (request?.EntryDate == null) return Fail(400, "VALIDATION", "EntryDate is required.");
        if (request.Severity is < 0 or > 10) return Fail(400, "VALIDATION", "Severity must be from 0 to 10.");
        if (request.EntryDate.Value.Date > DateTime.Today)
            return Fail(400, "VALIDATION", "Entry date cannot be in the future.");
        if (diaryId == null)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SymptomDiary (PatientId, EntryDate, Severity, Note, DeleteStatus, CreatedAt)
                VALUES ({caller.PatientId}, {request.EntryDate.Value.Date}, {request.Severity}, {TrimOrNull(request.Note, 1000)}, 0, {DateTime.Now})");
            return Ok(new { success = true, message = "Diary entry saved." });
        }
        var row = await _context.Database.SqlQuery<DiaryRow>($@"
            SELECT SymptomDiaryId, PatientId, EntryDate, Severity, Note
            FROM dbo.SymptomDiary WHERE SymptomDiaryId = {diaryId} AND DeleteStatus = 0").FirstOrDefaultAsync();
        if (row == null) return Fail(404, "NOT_FOUND", "Diary entry not found.");
        if (row.PatientId != caller.PatientId) return Fail(403, "FORBIDDEN", "This entry belongs to another patient.");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.SymptomDiary
            SET EntryDate = {request.EntryDate.Value.Date}, Severity = {request.Severity}, Note = {TrimOrNull(request.Note, 1000)}
            WHERE SymptomDiaryId = {diaryId}");
        return Ok(new { success = true, message = "Diary entry updated." });
    }

    private async Task<S4ActionResult> MoveMedicineAsync(int orderId, S4Caller caller, string from, string? alternate, string to)
    {
        if (!caller.IsPharmacy && !caller.IsAdmin)
            return Fail(403, "FORBIDDEN", "Only the pharmacy can update fulfilment.");
        var order = await LoadMedicineAsync(orderId);
        if (order == null) return Fail(404, "NOT_FOUND", "Medicine order not found.");
        if (!string.Equals(order.Status, from, StringComparison.OrdinalIgnoreCase)
            && (alternate == null || !string.Equals(order.Status, alternate, StringComparison.OrdinalIgnoreCase)))
            return Fail(409, "CONFLICT", "Order status " + order.Status + " cannot move to " + to + ".");
        await SetMedicineStatusAsync(orderId, to, null);
        return Ok(new { success = true, data = new { orderId, status = to } });
    }

    private async Task<(int PatientId, S4ActionResult? Error)> ResolvePatientAsync(int? patientId, S4Caller caller, bool write)
    {
        if (caller.IsPatient)
        {
            if (caller.PatientId is null) return (0, Fail(403, "FORBIDDEN", "A patient profile is required."));
            if (patientId.HasValue && patientId.Value != caller.PatientId)
                return (0, Fail(403, "FORBIDDEN", "You can read only your own record."));
            return (caller.PatientId.Value, null);
        }
        if (patientId is null or <= 0)
            return (0, Fail(400, "VALIDATION", "PatientId is required."));
        if (caller.IsAdmin) return (patientId.Value, null);
        if (caller.IsDoctor)
        {
            var owns = await _context.PatientAppointments.AsNoTracking()
                .AnyAsync(a => a.PatientId == patientId && a.DoctorId == caller.DoctorId && a.DeleteStatus != true);
            if (!owns) return (0, Fail(403, "FORBIDDEN", "This patient is not on your list."));
            if (write) return (0, Fail(403, "FORBIDDEN", "The doctor view is read-only."));
            return (patientId.Value, null);
        }
        return (0, Fail(403, "FORBIDDEN", "You cannot read this patient record."));
    }

    private async Task<MedicineRow?> LoadMedicineAsync(int id)
        => await _context.Database.SqlQuery<MedicineRow>($@"
            SELECT MedicineOrderId, ErxSnapshotId, PatientId, PharmacyPartnerId, Status, ConsentGranted, QuoteAmount, PayMode
            FROM dbo.MedicineOrder WHERE MedicineOrderId = {id}").FirstOrDefaultAsync();

    private async Task SetMedicineStatusAsync(int id, string status, string? detail)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE dbo.MedicineOrder SET Status = {status} WHERE MedicineOrderId = {id}");
        await AddMedicineEventAsync(id, status, detail);
    }

    private async Task AddMedicineEventAsync(int id, string status, string? detail)
        => await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.MedicineOrderEvent (MedicineOrderId, Status, Detail, At)
            VALUES ({id}, {status}, {detail}, {DateTime.Now})");

    private async Task<int?> CallerPharmacyIdAsync(S4Caller caller)
    {
        var row = await _context.Database.SqlQuery<IdIntRow>($@"
            SELECT TOP 1 PharmacyPartnerId AS Id FROM dbo.PharmacyPartner WHERE UserId = {caller.UserId} AND Status = N'ACTIVE'").FirstOrDefaultAsync();
        return row?.Id;
    }

    private async Task<bool> PharmacyAcceptedAsync(int erxId)
    {
        var count = await _context.Database.SqlQuery<CountRow>($@"
            SELECT COUNT(1) AS Value FROM dbo.MedicineOrder
            WHERE ErxSnapshotId = {erxId} AND Status NOT IN (N'OFFERED', N'REJECTED')").FirstAsync();
        return count.Value > 0;
    }

    private async Task<SnapshotRow?> LoadSnapshotAsync(int id)
        => await _context.Database.SqlQuery<SnapshotRow>($@"
            SELECT ErxSnapshotId, PatientAppId, PatientId, DoctorId, Status, SignedAt
            FROM dbo.ErxSnapshot WHERE ErxSnapshotId = {id}").FirstOrDefaultAsync();

    private async Task<SnapshotRow?> LoadSnapshotByAppointmentAsync(int patientAppId)
        => await _context.Database.SqlQuery<SnapshotRow>($@"
            SELECT ErxSnapshotId, PatientAppId, PatientId, DoctorId, Status, SignedAt
            FROM dbo.ErxSnapshot WHERE PatientAppId = {patientAppId}").FirstOrDefaultAsync();

    private async Task<List<SnapshotItemRow>> LoadSnapshotItemsAsync(int id, bool revealNames)
    {
        var rows = await _context.Database.SqlQuery<SnapshotItemRow>($@"
            SELECT RemedyCode, RemedyName, PotencyCode, Dose, Frequency, Duration, Instructions
            FROM dbo.ErxSnapshotItem WHERE ErxSnapshotId = {id}").ToListAsync();
        if (!revealNames)
        {
            foreach (var row in rows)
                row.RemedyName = "";
        }
        return rows;
    }

    private async Task<S4ActionResult?> EnsureSnapshotAccessAsync(SnapshotRow snapshot, S4Caller caller, bool patientView)
    {
        if (caller.IsAdmin) return null;
        if (patientView || caller.IsPatient)
            return caller.PatientId == snapshot.PatientId ? null : Fail(403, "FORBIDDEN", "This prescription belongs to another patient.");
        if (caller.OwnsDoctor(snapshot.DoctorId)) return null;
        return Fail(403, "FORBIDDEN", "You cannot read this prescription.");
    }

    private static bool S3AppointmentRulesCancelled(string? status)
        => Helpers.S3AppointmentRules.IsCancelled(status);

    private static bool IsDuplicate(Exception ex)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
                return true;
        }
        return false;
    }

    private static string? TrimOrNull(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value.Trim();
        return text.Length <= max ? text : text[..max];
    }

    private static string Digits(string? value)
        => new string((value ?? "").Where(char.IsDigit).ToArray());

    private static TimeSpan? ParseTime(string? value)
        => TimeSpan.TryParse(value, out var time) ? time : null;

    private sealed class VerificationEventRow
    {
        public long DoctorVerificationEventId { get; set; }
        public int DoctorId { get; set; }
        public string Status { get; set; } = "";
        public string? Note { get; set; }
        public long ByUserId { get; set; }
        public DateTime At { get; set; }
    }

    private sealed class ReviewRow
    {
        public int ReviewId { get; set; }
        public int PatientAppId { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public int Rating { get; set; }
        public string? Text { get; set; }
        public string Status { get; set; } = "";
        public DateTime At { get; set; }
    }

    private sealed class AppealRow
    {
        public int ReviewAppealId { get; set; }
        public int ReviewId { get; set; }
        public int DoctorId { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = "";
        public string? Resolution { get; set; }
        public DateTime At { get; set; }
    }

    private sealed class WeightRow
    {
        public decimal VerifiedWeight { get; set; }
        public decimal RatingWeight { get; set; }
        public decimal FeeWeight { get; set; }
    }

    private sealed class PotencyRow
    {
        public int PotencyId { get; set; }
        public string Code { get; set; } = "";
        public int SortOrder { get; set; }
    }

    private sealed class RemedyLineRow
    {
        public int PrescriptionRemedyId { get; set; }
        public int RemedyId { get; set; }
        public string RemedyName { get; set; } = "";
        public string? Dose { get; set; }
        public string? PotencyCode { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }

    private sealed class SnapshotRow
    {
        public int ErxSnapshotId { get; set; }
        public int PatientAppId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Status { get; set; } = "";
        public DateTime? SignedAt { get; set; }
    }

    private sealed class SnapshotItemRow
    {
        public string RemedyCode { get; set; } = "";
        public string RemedyName { get; set; } = "";
        public string? PotencyCode { get; set; }
        public string? Dose { get; set; }
        public string? Frequency { get; set; }
        public string? Duration { get; set; }
        public string? Instructions { get; set; }
    }

    private sealed class RefillRow
    {
        public int RefillRequestId { get; set; }
        public int ErxSnapshotId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string Status { get; set; } = "";
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class SellerRow
    {
        public int PharmacyPartnerId { get; set; }
        public string Name { get; set; } = "";
        public string? Area { get; set; }
        public string Status { get; set; } = "";
    }

    private sealed class OrderItemRow
    {
        public string RemedyCode { get; set; } = "";
        public string? RemedyName { get; set; }
    }

    private sealed class MedicineEventRow
    {
        public string Status { get; set; } = "";
        public string? Detail { get; set; }
        public DateTime At { get; set; }
    }

    private sealed class NoteRow
    {
        public string Text { get; set; } = "";
        public DateTime At { get; set; }
    }

    private sealed class FollowUpRow
    {
        public int FollowUpTaskId { get; set; }
        public string Title { get; set; } = "";
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
    }

    private sealed class DiaryRow
    {
        public int SymptomDiaryId { get; set; }
        public int PatientId { get; set; }
        public DateTime EntryDate { get; set; }
        public int Severity { get; set; }
        public string? Note { get; set; }
    }

    private sealed class HealthRow
    {
        public int PatientId { get; set; }
        public string? BloodGroup { get; set; }
        public string? Allergies { get; set; }
        public string? ChronicConditions { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactMobile { get; set; }
    }

    private sealed class IdIntRow
    {
        public int Id { get; set; }
    }
}
