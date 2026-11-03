using Homeocentrum.Niga.API.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Helpers;

/// <summary>
/// Seeds <c>PatientDeliveryAddress</c> from the patient chart address filled at registration/create time.
/// </summary>
public static class PatientDeliveryAddressSeeder
{
    private sealed class CountRow
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// When the patient has a usable chart address and no delivery rows yet, insert it as the default.
    /// If delivery rows exist but none is default, mark the newest row as default.
    /// </summary>
    public static async Task EnsureDefaultFromProfileAsync(NIGACentrumContext db, int patientId)
    {
        if (patientId <= 0 || db == null)
            return;

        await EnsureTableAsync(db);

        var existingCount = await db.Database.SqlQuery<CountRow>($@"
            SELECT CAST(COUNT(1) AS int) AS Id
            FROM dbo.PatientDeliveryAddress
            WHERE PatientId = {patientId}").FirstOrDefaultAsync();

        if (existingCount is { Id: > 0 })
        {
            var defaultCount = await db.Database.SqlQuery<CountRow>($@"
                SELECT CAST(COUNT(1) AS int) AS Id
                FROM dbo.PatientDeliveryAddress
                WHERE PatientId = {patientId} AND IsDefault = 1").FirstOrDefaultAsync();
            if (defaultCount is null or { Id: 0 })
            {
                await db.Database.ExecuteSqlInterpolatedAsync($@"
                    UPDATE dbo.PatientDeliveryAddress
                    SET IsDefault = 1
                    WHERE DeliveryAddressId = (
                        SELECT TOP 1 DeliveryAddressId
                        FROM dbo.PatientDeliveryAddress
                        WHERE PatientId = {patientId}
                        ORDER BY DeliveryAddressId ASC
                    )");
            }
            return;
        }

        var profile = await (
            from p in db.Patients.AsNoTracking()
            where p.PatientId == patientId && p.DeleteStatus != true
            join pinRow in db.PinCodeMasters.AsNoTracking() on p.PinCodeId equals pinRow.PinCodeId into pj
            from pinRow in pj.DefaultIfEmpty()
            join cityRow in db.CityMasters.AsNoTracking() on p.CityId equals cityRow.CityId into cj
            from cityRow in cj.DefaultIfEmpty()
            join stateRow in db.StateMasters.AsNoTracking() on p.StateId equals stateRow.StateId into sj
            from stateRow in sj.DefaultIfEmpty()
            select new
            {
                p.AddressLine1,
                p.AddressLine2,
                p.Address,
                p.Landmark,
                p.MobileNo,
                p.PhoneNo,
                PinCode = pinRow != null ? pinRow.PinCode : null,
                CityName = cityRow != null ? cityRow.CityName : null,
                StateName = stateRow != null ? stateRow.StateName : null
            }).FirstOrDefaultAsync();

        if (profile == null)
            return;

        var line1 = FirstNonEmpty(profile.AddressLine1, profile.Address);
        if (string.IsNullOrWhiteSpace(line1))
            return;

        var pincode = DigitsOnly(profile.PinCode);
        if (pincode.Length != 6)
            return;

        var line2 = FirstNonEmpty(profile.AddressLine2, profile.Landmark);
        if (!string.IsNullOrWhiteSpace(profile.AddressLine2)
            && !string.IsNullOrWhiteSpace(profile.Landmark)
            && !string.Equals(profile.AddressLine2.Trim(), profile.Landmark.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            line2 = $"{profile.AddressLine2.Trim()}, {profile.Landmark.Trim()}";
        }

        if (line1.Length > 200) line1 = line1[..200];
        if (line2 != null && line2.Length > 200) line2 = line2[..200];

        var cityName = TrimTo(profile.CityName, 80);
        var stateName = TrimTo(profile.StateName, 80);
        var phone = FirstNonEmpty(profile.MobileNo, profile.PhoneNo);
        if (phone != null && phone.Length > 30) phone = phone[..30];

        await db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO dbo.PatientDeliveryAddress
                (PatientId, Label, Line1, Line2, City, State, Pincode, Phone, IsDefault, CreatedAt)
            VALUES
                ({patientId}, {"Home"}, {line1}, {line2}, {cityName}, {stateName}, {pincode}, {phone}, {true}, {DateTime.Now})");
    }

    private static async Task EnsureTableAsync(NIGACentrumContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.PatientDeliveryAddress', N'U') IS NULL
            CREATE TABLE dbo.PatientDeliveryAddress (
                DeliveryAddressId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                PatientId int NOT NULL,
                Label nvarchar(80) NULL,
                Line1 nvarchar(200) NOT NULL,
                Line2 nvarchar(200) NULL,
                City nvarchar(80) NULL,
                State nvarchar(80) NULL,
                Pincode nvarchar(12) NOT NULL,
                Phone nvarchar(30) NULL,
                IsDefault bit NOT NULL CONSTRAINT DF_Pda_Default_Seed DEFAULT (0),
                CreatedAt datetime NOT NULL
            );
            IF COL_LENGTH(N'dbo.PatientDeliveryAddress', N'Phone') IS NULL
                ALTER TABLE dbo.PatientDeliveryAddress ADD Phone nvarchar(30) NULL;
            """);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private static string DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static string? TrimTo(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
