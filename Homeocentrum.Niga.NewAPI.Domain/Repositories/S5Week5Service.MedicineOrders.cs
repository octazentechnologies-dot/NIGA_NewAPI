using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

/// <summary>MED-16.02 / MED-14.02 patient order history with items, pharmacy, doctor and tracking; MED-15.01 order review.</summary>
public partial class S5Week5Service
{
    public Task<S4ActionResult> PatientMedicineHistoryAsync(S4Caller caller) => Guard(async () =>
    {
        if (caller.PatientId is null) return S4ActionResult.Fail(403, "FORBIDDEN", "A patient profile is required.");
        var patientId = caller.PatientId.Value;
        var orders = await _context.Database.SqlQuery<PatientOrderRow>($@"
            SELECT o.MedicineOrderId, o.ErxSnapshotId, o.Status, o.QuoteAmount,
                   COALESCE(NULLIF(o.PayMode, N''), pay.Method) AS PayMode, o.CreatedAt,
                   o.PharmacyPartnerId, pp.Name AS PharmacyName, pp.Area AS PharmacyArea,
                   NULLIF(LTRIM(RTRIM(CONCAT(N'Dr. ', d.FirstName, N' ', d.LastName))), N'Dr.') AS DoctorName,
                   q.Note AS QuoteNote
            FROM dbo.MedicineOrder o
            LEFT JOIN dbo.PharmacyPartner pp ON pp.PharmacyPartnerId = o.PharmacyPartnerId
            LEFT JOIN dbo.ErxSnapshot e ON e.ErxSnapshotId = o.ErxSnapshotId
            LEFT JOIN dbo.Doctor d ON d.DoctorID = e.DoctorId
            OUTER APPLY (SELECT TOP 1 mq.Note FROM dbo.MedicineQuote mq
                         WHERE mq.MedicineOrderId = o.MedicineOrderId ORDER BY mq.At DESC) q
            OUTER APPLY (SELECT TOP 1 NULLIF(po.Method, N'') AS Method FROM dbo.PaymentOrder po
                         WHERE po.MedicineOrderId = o.MedicineOrderId AND po.Stream = N'MEDICINE'
                         ORDER BY po.PaymentOrderId DESC) pay
            WHERE o.PatientId = {patientId}
            ORDER BY o.MedicineOrderId DESC").ToListAsync();
        var items = await _context.Database.SqlQuery<PatientOrderItemRow>($@"
            SELECT i.MedicineOrderId, i.RemedyCode, i.RemedyName
            FROM dbo.MedicineOrderItem i
            JOIN dbo.MedicineOrder o ON o.MedicineOrderId = i.MedicineOrderId
            WHERE o.PatientId = {patientId}
            ORDER BY i.MedicineOrderItemId").ToListAsync();
        var events = await _context.Database.SqlQuery<PatientOrderEventRow>($@"
            SELECT ev.MedicineOrderId, ev.Status, ev.Detail, ev.At
            FROM dbo.MedicineOrderEvent ev
            JOIN dbo.MedicineOrder o ON o.MedicineOrderId = ev.MedicineOrderId
            WHERE o.PatientId = {patientId}
            ORDER BY ev.At").ToListAsync();
        var reviews = await OrderReviewsAsync(patientId);

        var itemsByOrder = items.ToLookup(i => i.MedicineOrderId);
        var eventsByOrder = events.ToLookup(e => e.MedicineOrderId);
        var data = orders.Select(o =>
        {
            reviews.TryGetValue(o.MedicineOrderId, out var review);
            return new
            {
                o.MedicineOrderId,
                o.ErxSnapshotId,
                o.Status,
                o.QuoteAmount,
                o.QuoteNote,
                o.PayMode,
                o.CreatedAt,
                o.PharmacyPartnerId,
                o.PharmacyName,
                o.PharmacyArea,
                o.DoctorName,
                items = itemsByOrder[o.MedicineOrderId].Select(i => new { i.RemedyCode, i.RemedyName }),
                events = eventsByOrder[o.MedicineOrderId].Select(e => new { e.Status, e.Detail, e.At }),
                review = review == null ? null : new { review.Rating, review.Comment, review.CreatedAt },
            };
        });
        return S4ActionResult.Ok(new { success = true, data });
    });

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
        if (order.PatientId != caller.PatientId) return S4ActionResult.Fail(403, "FORBIDDEN", "This order belongs to another patient.");
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

    private async Task<Dictionary<int, PatientOrderReviewRow>> OrderReviewsAsync(int patientId)
    {
        try
        {
            var rows = await _context.Database.SqlQuery<PatientOrderReviewRow>($@"
                SELECT MedicineOrderId, CAST(Rating AS int) AS Rating, Comment, CreatedAt
                FROM dbo.MedicineOrderReview WHERE PatientId = {patientId}").ToListAsync();
            return rows.ToDictionary(r => r.MedicineOrderId);
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
        public string? PayMode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? PharmacyPartnerId { get; set; }
        public string? PharmacyName { get; set; }
        public string? PharmacyArea { get; set; }
        public string? DoctorName { get; set; }
        public string? QuoteNote { get; set; }
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
