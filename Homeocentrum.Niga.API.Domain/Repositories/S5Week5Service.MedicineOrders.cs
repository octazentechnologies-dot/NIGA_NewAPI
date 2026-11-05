using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.API.Domain.Interfaces;
using Homeocentrum.Niga.API.Domain.Security;

namespace Homeocentrum.Niga.API.Domain.Repositories;

/// <summary>MED-16.02 / MED-14.02 patient order history with items, pharmacy, doctor and tracking; MED-15.01 order review.</summary>
public partial class S5Week5Service
{
    public Task<S4ActionResult> PatientMedicineHistoryAsync(S4Caller caller) => Guard(async () =>
    {
        if (caller.PatientId is null || caller.UserId <= 0)
            return S4ActionResult.Fail(403, "FORBIDDEN", "A patient profile is required.");
        var patientId = caller.PatientId.Value;
        var householdIds = await HouseholdPatientIdsAsync(caller.UserId, patientId);
        var idList = string.Join(",", householdIds);
        var orders = await _context.Database.SqlQuery<PatientOrderRow>($@"
            SELECT o.MedicineOrderId, o.ErxSnapshotId, o.Status, o.QuoteAmount, o.DeliveryFee AS OrderDeliveryFee,
                   COALESCE(NULLIF(o.PayMode, N''), pay.Method) AS PayMode, o.CreatedAt,
                   o.PharmacyPartnerId, pp.Name AS PharmacyName, pp.Area AS PharmacyArea, pp.Mobile AS PharmacyMobile,
                   NULLIF(LTRIM(RTRIM(CONCAT(N'Dr. ', d.FirstName, N' ', d.LastName))), N'Dr.') AS DoctorName,
                   q.Note AS QuoteNote, q.DeliveryFee AS QuoteDeliveryFee,
                   del.At AS DeliveredAt,
                   lic.LicenceNumber AS PharmacyLicenceNumber,
                   CAST(CASE WHEN lic.LicenceNumber IS NOT NULL THEN 1 ELSE 0 END AS bit) AS PharmacyLicensed,
                   pay.PaymentOrderId, pay.Amount AS PaymentAmount, pay.Status AS PaymentStatus,
                   inv.InvoiceId, inv.Number AS InvoiceNumber, inv.PdfPath, inv.SecureDocumentId
            FROM dbo.MedicineOrder o
            LEFT JOIN dbo.PharmacyPartner pp ON pp.PharmacyPartnerId = o.PharmacyPartnerId
            LEFT JOIN dbo.ErxSnapshot e ON e.ErxSnapshotId = o.ErxSnapshotId
            LEFT JOIN dbo.Doctor d ON d.DoctorID = e.DoctorId
            OUTER APPLY (SELECT TOP 1 mq.Note, mq.DeliveryFee FROM dbo.MedicineQuote mq
                         WHERE mq.MedicineOrderId = o.MedicineOrderId ORDER BY mq.At DESC) q
            OUTER APPLY (SELECT TOP 1 ev.At FROM dbo.MedicineOrderEvent ev
                         WHERE ev.MedicineOrderId = o.MedicineOrderId AND ev.Status = N'DELIVERED'
                         ORDER BY ev.At DESC) del
            OUTER APPLY (SELECT TOP 1 l.LicenceNumber FROM dbo.PharmacyLicence l
                         WHERE l.PharmacyPartnerId = o.PharmacyPartnerId
                           AND l.Status = N'ACTIVE' AND l.ExpiryDate >= CONVERT(date, GETDATE())
                         ORDER BY l.ExpiryDate DESC) lic
            OUTER APPLY (SELECT TOP 1 po.PaymentOrderId, NULLIF(po.Method, N'') AS Method, po.Amount, po.Status
                         FROM dbo.PaymentOrder po
                         WHERE po.MedicineOrderId = o.MedicineOrderId AND po.Stream = N'MEDICINE'
                         ORDER BY po.PaymentOrderId DESC) pay
            OUTER APPLY (SELECT TOP 1 i.InvoiceId, i.Number, i.PdfPath, i.SecureDocumentId
                         FROM dbo.Invoice i WHERE i.PaymentOrderId = pay.PaymentOrderId) inv
            WHERE o.PatientId IN ({idList})
            ORDER BY o.MedicineOrderId DESC").ToListAsync();
        var items = await _context.Database.SqlQuery<PatientOrderItemRow>($@"
            SELECT i.MedicineOrderId, i.RemedyCode, i.RemedyName
            FROM dbo.MedicineOrderItem i
            JOIN dbo.MedicineOrder o ON o.MedicineOrderId = i.MedicineOrderId
            WHERE o.PatientId IN ({idList})
            ORDER BY i.MedicineOrderItemId").ToListAsync();
        var events = await _context.Database.SqlQuery<PatientOrderEventRow>($@"
            SELECT ev.MedicineOrderId, ev.Status, ev.Detail, ev.At
            FROM dbo.MedicineOrderEvent ev
            JOIN dbo.MedicineOrder o ON o.MedicineOrderId = ev.MedicineOrderId
            WHERE o.PatientId IN ({idList})
            ORDER BY ev.At").ToListAsync();
        var reviews = await OrderReviewsAsync(householdIds);

        var itemsByOrder = items.ToLookup(i => i.MedicineOrderId);
        var eventsByOrder = events.ToLookup(e => e.MedicineOrderId);
        var data = orders.Select(o =>
        {
            reviews.TryGetValue(o.MedicineOrderId, out var review);
            var deliveryFee = o.QuoteDeliveryFee ?? o.OrderDeliveryFee;
            var subtotalMedicines = o.QuoteAmount.HasValue && deliveryFee.HasValue
                ? o.QuoteAmount.Value - deliveryFee.Value
                : (decimal?)null;
            var totalPaid = ResolveHistoryTotalPaid(o.QuoteAmount, o.PaymentAmount, o.PaymentStatus, o.PayMode, o.Status);
            var contactMasked = MaskHistoryContact(o.PharmacyMobile);
            return new
            {
                o.MedicineOrderId,
                o.ErxSnapshotId,
                rxLabel = o.ErxSnapshotId is > 0 ? $"eRx #{o.ErxSnapshotId}" : null,
                o.Status,
                o.QuoteAmount,
                subtotalMedicines,
                deliveryFee,
                totalPaid,
                currency = o.QuoteAmount.HasValue || totalPaid.HasValue ? "INR" : null,
                o.QuoteNote,
                o.PayMode,
                o.CreatedAt,
                deliveredAt = o.DeliveredAt,
                o.PharmacyPartnerId,
                o.PharmacyName,
                o.PharmacyArea,
                pharmacyLicenceNumber = o.PharmacyLicenceNumber,
                pharmacyLicensed = o.PharmacyLicensed,
                pharmacyContactMasked = contactMasked,
                o.DoctorName,
                paymentOrderId = o.PaymentOrderId,
                invoiceId = o.InvoiceId,
                invoiceNumber = o.InvoiceNumber,
                invoicePdfAvailable = o.InvoiceId.HasValue
                    && (o.SecureDocumentId.HasValue || !string.IsNullOrWhiteSpace(o.PdfPath)),
                items = itemsByOrder[o.MedicineOrderId].Select(i => new { i.RemedyCode, i.RemedyName }),
                events = eventsByOrder[o.MedicineOrderId].Select(e => new { e.Status, e.Detail, e.At }),
                review = review == null ? null : new { review.Rating, review.Comment, review.CreatedAt },
            };
        });
        return S4ActionResult.Ok(new { success = true, data });
    });

    private static string? MaskHistoryContact(string? mobile)
    {
        var digits = new string((mobile ?? "").Where(char.IsDigit).ToArray());
        if (digits.Length < 4) return null;
        return "******" + digits[^4..];
    }

    private static decimal? ResolveHistoryTotalPaid(
        decimal? quoteAmount,
        decimal? paymentAmount,
        string? paymentStatus,
        string? payMode,
        string? orderStatus)
    {
        var ps = (paymentStatus ?? "").Trim().ToUpperInvariant();
        if (paymentAmount.HasValue && ps is "CAPTURED" or "COLLECTED" or "PAID" or "SUCCESS" or "COD_COLLECTED")
            return paymentAmount;
        var st = (orderStatus ?? "").Trim().ToUpperInvariant();
        var pm = (payMode ?? "").Trim().ToUpperInvariant();
        if (pm is "COD" or "CASH" || st is "PAID" or "COD_PENDING" or "READY" or "DISPATCHED" or "DELIVERED")
            return quoteAmount;
        return null;
    }

    public Task<S4ActionResult> ReviewMedicineOrderAsync(int orderId, MedicineOrderReviewWrite request, S4Caller caller) => Guard(async () =>
    {
        if (caller.PatientId is null) return S4ActionResult.Fail(403, "FORBIDDEN", "Only the patient can review an order.");
        if (request == null || request.Rating is < 1 or > 5)
            return S4ActionResult.Fail(400, "VALIDATION", "Rating must be from 1 to 5.");
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        if (comment is { Length: > 1000 }) return S4ActionResult.Fail(400, "VALIDATION", "Review must be 1000 characters or fewer.");
        var order = await _context.Database.SqlQuery<OrderOwnerRow>($@"
            SELECT MedicineOrderId, PatientId, Status FROM dbo.MedicineOrder WHERE MedicineOrderId = {orderId}").FirstOrDefaultAsync();
        if (order == null) return S4ActionResult.Fail(404, "NOT_FOUND", "Medicine order not found.");
        var mayReview = order.PatientId == caller.PatientId
            || (caller.UserId > 0
                && await PatientHouseholdAccess.UserCanAccessPatientAsync(_context, caller.UserId, order.PatientId));
        if (!mayReview)
            return S4ActionResult.Fail(403, "FORBIDDEN", "This order belongs to another patient.");
        if (!string.Equals(order.Status, "DELIVERED", StringComparison.OrdinalIgnoreCase))
            return S4ActionResult.Fail(409, "CONFLICT", "You can review an order after it is delivered.");
        var existing = await CountAsync($@"
            SELECT COUNT(*) AS Value FROM dbo.MedicineOrderReview WHERE MedicineOrderId = {orderId}");
        if (existing > 0) return S4ActionResult.Fail(409, "CONFLICT", "You have already reviewed this order.");
        var now = DateTime.Now;
        var rating = (byte)request.Rating!.Value;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.MedicineOrderReview (MedicineOrderId, PatientId, Rating, Comment, CreatedAt)
            VALUES ({orderId}, {order.PatientId}, {rating}, {comment}, {now})");
        return S4ActionResult.Ok(new { success = true, data = new { orderId, rating = request.Rating, comment, createdAt = now } });
    });

    private async Task<List<int>> HouseholdPatientIdsAsync(long userId, int selfPatientId)
    {
        var ids = new HashSet<int> { selfPatientId };
        var mapped = await _context.PatientUserMaps.AsNoTracking()
            .Where(m => m.UserId == userId && !m.DeleteStatus)
            .Select(m => m.PatientId)
            .ToListAsync();
        foreach (var id in mapped) ids.Add(id);
        var family = await _context.PatientFamilyMembers.AsNoTracking()
            .Where(f => f.OwnerUserId == userId && !f.DeleteStatus)
            .Select(f => f.MemberPatientId)
            .ToListAsync();
        foreach (var id in family) ids.Add(id);
        var caregiver = await _context.CaregiverAuthorizations.AsNoTracking()
            .Where(c => c.CaregiverUserId == userId && !c.DeleteStatus && c.RevokedAt == null)
            .Select(c => c.PatientId)
            .ToListAsync();
        foreach (var id in caregiver) ids.Add(id);
        return ids.Where(id => id > 0).ToList();
    }

    private async Task<Dictionary<int, PatientOrderReviewRow>> OrderReviewsAsync(IReadOnlyCollection<int> patientIds)
    {
        try
        {
            if (patientIds == null || patientIds.Count == 0)
                return new Dictionary<int, PatientOrderReviewRow>();
            var idList = string.Join(",", patientIds.Where(id => id > 0).Distinct());
            if (string.IsNullOrEmpty(idList))
                return new Dictionary<int, PatientOrderReviewRow>();
            var rows = await _context.Database.SqlQuery<PatientOrderReviewRow>($@"
                SELECT MedicineOrderId, CAST(Rating AS int) AS Rating, Comment, CreatedAt
                FROM dbo.MedicineOrderReview WHERE PatientId IN ({idList})").ToListAsync();
            return rows.GroupBy(r => r.MedicineOrderId).ToDictionary(g => g.Key, g => g.First());
        }
        catch (SqlException ex) when (ex.Number == 208)
        {
            return new Dictionary<int, PatientOrderReviewRow>();
        }
    }

    private sealed class PatientOrderRow
    {
        public int MedicineOrderId { get; set; }
        public int? ErxSnapshotId { get; set; }
        public string? Status { get; set; }
        public decimal? QuoteAmount { get; set; }
        public decimal? OrderDeliveryFee { get; set; }
        public decimal? QuoteDeliveryFee { get; set; }
        public string? PayMode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public int? PharmacyPartnerId { get; set; }
        public string? PharmacyName { get; set; }
        public string? PharmacyArea { get; set; }
        public string? PharmacyMobile { get; set; }
        public string? PharmacyLicenceNumber { get; set; }
        public bool PharmacyLicensed { get; set; }
        public string? DoctorName { get; set; }
        public string? QuoteNote { get; set; }
        public long? PaymentOrderId { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentStatus { get; set; }
        public long? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? PdfPath { get; set; }
        public long? SecureDocumentId { get; set; }
    }

    private sealed class PatientOrderItemRow
    {
        public int MedicineOrderId { get; set; }
        public string? RemedyCode { get; set; }
        public string? RemedyName { get; set; }
    }

    private sealed class PatientOrderEventRow
    {
        public int MedicineOrderId { get; set; }
        public string? Status { get; set; }
        public string? Detail { get; set; }
        public DateTime At { get; set; }
    }

    private sealed class PatientOrderReviewRow
    {
        public int MedicineOrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class OrderOwnerRow
    {
        public int MedicineOrderId { get; set; }
        public int PatientId { get; set; }
        public string? Status { get; set; }
    }
}
