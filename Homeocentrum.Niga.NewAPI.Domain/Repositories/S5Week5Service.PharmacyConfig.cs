using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Homeocentrum.Niga.NewAPI.Domain.Interfaces;

namespace Homeocentrum.Niga.NewAPI.Domain.Repositories;

/// <summary>MED-05.03 pharmacy configuration: hours, working days, service areas, delivery charges, capacity.</summary>
public partial class S5Week5Service
{
    private static readonly string[] WeekDays = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

    public Task<S4ActionResult> GetPharmacyConfigAsync(int pharmacyPartnerId, S4Caller caller) => Guard(async () =>
    {
        var denied = await PharmacyAccessAsync(pharmacyPartnerId, caller);
        if (denied != null) return denied;
        var config = await _context.Database.SqlQuery<PharmacyConfigRow>($@"
            SELECT PharmacyPartnerId, OpenTime, CloseTime, WorkingDays, DeliveryCharge, FreeDeliveryAbove, Capacity, UpdatedAt
            FROM dbo.PharmacyConfig WHERE PharmacyPartnerId = {pharmacyPartnerId}").FirstOrDefaultAsync();
        var rules = await _context.Database.SqlQuery<RoutingRow>($@"
            SELECT Area, OpenTime, CloseTime, Capacity
            FROM dbo.SellerRoutingRule WHERE PharmacyPartnerId = {pharmacyPartnerId}
            ORDER BY SellerRoutingRuleId").ToListAsync();
        var areas = rules.Select(r => r.Area?.Trim()).Where(a => !string.IsNullOrEmpty(a))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var firstRule = rules.FirstOrDefault(r => r.OpenTime.HasValue && r.CloseTime.HasValue);
        var open = config?.OpenTime ?? firstRule?.OpenTime;
        var close = config?.CloseTime ?? firstRule?.CloseTime;
        return S4ActionResult.Ok(new
        {
            success = true,
            data = new
            {
                pharmacyPartnerId,
                configured = config != null || rules.Count > 0,
                openTime = open?.ToString(@"hh\:mm"),
                closeTime = close?.ToString(@"hh\:mm"),
                days = config == null ? Array.Empty<string>() : SplitDays(config.WorkingDays),
                areas,
                deliveryCharge = config?.DeliveryCharge,
                freeAbove = config?.FreeDeliveryAbove,
                capacity = config?.Capacity ?? firstRule?.Capacity,
                updatedAt = config?.UpdatedAt,
            }
        });
    });

    public Task<S4ActionResult> SavePharmacyConfigAsync(int pharmacyPartnerId, PharmacyConfigWrite request, S4Caller caller) => Guard(async () =>
    {
        var denied = await PharmacyAccessAsync(pharmacyPartnerId, caller);
        if (denied != null) return denied;
        if (request == null) return S4ActionResult.Fail(400, "VALIDATION", "Configuration is required.");
        if (!TimeSpan.TryParseExact(request.OpenTime ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var open)
            || !TimeSpan.TryParseExact(request.CloseTime ?? "", @"hh\:mm", CultureInfo.InvariantCulture, out var close))
            return S4ActionResult.Fail(400, "VALIDATION", "Opening and closing time must be HH:mm.");
        if (close <= open) return S4ActionResult.Fail(400, "VALIDATION", "Closing time must be after opening time.");
        var days = WeekDays.Where(d => (request.Days ?? new List<string>()).Any(x => string.Equals(x?.Trim(), d, StringComparison.OrdinalIgnoreCase))).ToList();
        if (days.Count == 0) return S4ActionResult.Fail(400, "VALIDATION", "Select at least one working day.");
        var areas = (request.Areas ?? new List<string>()).Select(a => a?.Trim()).Where(a => !string.IsNullOrEmpty(a))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (areas.Count == 0) return S4ActionResult.Fail(400, "VALIDATION", "Add at least one service area.");
        if (areas.Count > 50 || areas.Any(a => a!.Length > 100))
            return S4ActionResult.Fail(400, "VALIDATION", "Up to 50 service areas of 100 characters each.");
        if (request.DeliveryCharge is null or < 0 or > 100000)
            return S4ActionResult.Fail(400, "VALIDATION", "Enter a valid delivery charge (0 for free).");
        if (request.FreeAbove is < 0 or > 1000000)
            return S4ActionResult.Fail(400, "VALIDATION", "Free-delivery amount is not valid.");
        if (request.Capacity is null or < 1 or > 500)
            return S4ActionResult.Fail(400, "VALIDATION", "Daily capacity must be from 1 to 500 orders.");

        var workingDays = string.Join(",", days);
        var now = DateTime.Now;
        await using var tx = await _context.Database.BeginTransactionAsync();
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            MERGE dbo.PharmacyConfig AS t
            USING (SELECT {pharmacyPartnerId} AS PharmacyPartnerId) AS s ON t.PharmacyPartnerId = s.PharmacyPartnerId
            WHEN MATCHED THEN UPDATE SET OpenTime = {open}, CloseTime = {close}, WorkingDays = {workingDays},
                 DeliveryCharge = {request.DeliveryCharge}, FreeDeliveryAbove = {request.FreeAbove}, Capacity = {request.Capacity},
                 UpdatedAt = {now}, UpdatedByUserId = {caller.UserId}
            WHEN NOT MATCHED THEN INSERT (PharmacyPartnerId, OpenTime, CloseTime, WorkingDays, DeliveryCharge, FreeDeliveryAbove, Capacity, UpdatedAt, UpdatedByUserId)
                 VALUES ({pharmacyPartnerId}, {open}, {close}, {workingDays}, {request.DeliveryCharge}, {request.FreeAbove}, {request.Capacity}, {now}, {caller.UserId});");
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
            DELETE FROM dbo.SellerRoutingRule WHERE PharmacyPartnerId = {pharmacyPartnerId}");
        foreach (var area in areas)
        {
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO dbo.SellerRoutingRule (PharmacyPartnerId, Area, OpenTime, CloseTime, Capacity)
                VALUES ({pharmacyPartnerId}, {area}, {open}, {close}, {request.Capacity})");
        }
        await tx.CommitAsync();
        return S4ActionResult.Ok(new { success = true, pharmacyPartnerId, updatedAt = now });
    });

    private async Task<S4ActionResult?> PharmacyAccessAsync(int pharmacyPartnerId, S4Caller caller)
    {
        if (pharmacyPartnerId <= 0) return S4ActionResult.Fail(400, "VALIDATION", "Pharmacy id is required.");
        var owner = await _context.Database.SqlQuery<PartnerOwnerRow>($@"
            SELECT PharmacyPartnerId, UserId FROM dbo.PharmacyPartner WHERE PharmacyPartnerId = {pharmacyPartnerId}").FirstOrDefaultAsync();
        if (owner == null) return S4ActionResult.Fail(404, "NOT_FOUND", "Pharmacy not found.");
        if (caller.IsAdmin) return null;
        if (caller.IsPharmacy && owner.UserId == caller.UserId) return null;
        return S4ActionResult.Fail(403, "FORBIDDEN", "This pharmacy belongs to another account.");
    }

    private static string[] SplitDays(string? value) =>
        (value ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private sealed class PharmacyConfigRow
    {
        public int PharmacyPartnerId { get; set; }
        public TimeSpan OpenTime { get; set; }
        public TimeSpan CloseTime { get; set; }
        public string? WorkingDays { get; set; }
        public decimal DeliveryCharge { get; set; }
        public decimal? FreeDeliveryAbove { get; set; }
        public int Capacity { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class RoutingRow
    {
        public string? Area { get; set; }
        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
        public int Capacity { get; set; }
    }

    private sealed class PartnerOwnerRow
    {
        public int PharmacyPartnerId { get; set; }
        public long? UserId { get; set; }
    }
}
