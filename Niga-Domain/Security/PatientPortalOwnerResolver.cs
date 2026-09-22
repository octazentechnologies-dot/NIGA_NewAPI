using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Niga_Domain.Data;
using Niga_Domain.Master;

namespace Niga_Domain.Security;

/// <summary>
/// CON-01 / CON-02 — clinical Patient the current login should act on.
/// Caregivers resolve to the patient they are authorised for (not a new Patient row).
/// </summary>
public sealed class PatientPortalOwner
{
    public int PatientId { get; init; }
    public string? PatientName { get; init; }
    public long OwnerUserId { get; init; }
    public bool IsActingAsCaregiver { get; init; }
}

public static class PatientPortalOwnerResolver
{
    private static readonly ConcurrentDictionary<long, SemaphoreSlim> CreateGates = new();

    public static async Task<PatientPortalOwner?> ResolveAsync(
        NIGACentrumContext db,
        long userId,
        bool preferActingFor = true,
        bool createIfMissing = true,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return null;

        if (preferActingFor)
        {
            var acting = await FindActingForAsync(db, userId, cancellationToken);
            if (acting != null)
                return acting;
        }

        var existing = await FindMappedOwnerAsync(db, userId, cancellationToken);
        if (existing != null)
            return existing;

        if (!preferActingFor)
        {
            var acting = await FindActingForAsync(db, userId, cancellationToken);
            if (acting != null)
                return acting;
        }

        if (!createIfMissing)
            return null;

        return await CreatePrimaryAsync(db, userId, cancellationToken);
    }

    private static async Task<PatientPortalOwner?> FindActingForAsync(
        NIGACentrumContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var acting = await (
            from c in db.CaregiverAuthorizations.AsNoTracking()
            join p in db.Patients.AsNoTracking() on c.PatientId equals p.PatientId
            where c.CaregiverUserId == userId
                && !c.DeleteStatus
                && c.RevokedAt == null
                && p.DeleteStatus != true
            orderby c.GrantedAt
            select new { p.PatientId, p.PatientName }
        ).FirstOrDefaultAsync(cancellationToken);

        if (acting == null)
            return null;

        var ownerUserId = await db.PatientUserMaps.AsNoTracking()
            .Where(m => m.PatientId == acting.PatientId && !m.DeleteStatus)
            .OrderByDescending(m => m.IsPrimary)
            .Select(m => m.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        return new PatientPortalOwner
        {
            PatientId = acting.PatientId,
            PatientName = acting.PatientName,
            OwnerUserId = ownerUserId > 0 ? ownerUserId : userId,
            IsActingAsCaregiver = true
        };
    }

    private static async Task<PatientPortalOwner?> FindMappedOwnerAsync(
        NIGACentrumContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var map = await db.PatientUserMaps
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.UserId == userId && !m.DeleteStatus && m.IsPrimary, cancellationToken)
            ?? await db.PatientUserMaps
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserId == userId && !m.DeleteStatus, cancellationToken);
        if (map == null)
            return null;

        var linked = await db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PatientId == map.PatientId && p.DeleteStatus != true, cancellationToken);
        return new PatientPortalOwner
        {
            PatientId = map.PatientId,
            PatientName = linked?.PatientName,
            OwnerUserId = userId,
            IsActingAsCaregiver = false
        };
    }

    private static async Task<PatientPortalOwner?> CreatePrimaryAsync(
        NIGACentrumContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var gate = CreateGates.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var raced = await FindMappedOwnerAsync(db, userId, cancellationToken);
            if (raced != null)
                return raced;

            return await InsertPrimaryAsync(db, userId, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<PatientPortalOwner?> InsertPrimaryAsync(
        NIGACentrumContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var user = await db.UserMasters
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.DeleteStatus, cancellationToken);
        if (user == null)
            return null;

        var email = user.EmailId ?? user.UserName;
        Patient? existingPatient = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            existingPatient = await db.Patients
                .FirstOrDefaultAsync(p => p.Email == email && p.DeleteStatus != true, cancellationToken);
        }
        if (existingPatient == null && !string.IsNullOrWhiteSpace(user.MobileNo))
        {
            existingPatient = await db.Patients
                .FirstOrDefaultAsync(p => p.MobileNo == user.MobileNo && p.DeleteStatus != true, cancellationToken);
        }

        int patientId;
        string? name;
        if (existingPatient != null)
        {
            patientId = existingPatient.PatientId;
            name = existingPatient.PatientName;
        }
        else
        {
            name = string.Join(" ", new[] { user.FirstName, user.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s))).Trim();
            if (string.IsNullOrWhiteSpace(name))
                name = user.UserName;

            int? countryId = null;
            int? stateId = null;
            if (user.CountryId.HasValue)
            {
                var cid = user.CountryId.Value;
                if (await db.CountryMasters.AnyAsync(c => c.CountryId == cid && !c.DeleteStatus, cancellationToken))
                    countryId = cid;
            }
            if (user.StateId.HasValue)
            {
                var sid = user.StateId.Value;
                if (await db.StateMasters.AnyAsync(s => s.StateId == sid && !s.DeleteStatus, cancellationToken))
                    stateId = sid;
            }

            var created = new Patient
            {
                PatientName = name,
                Email = email,
                MobileNo = user.MobileNo,
                CountryId = countryId,
                StateId = stateId,
                DeleteStatus = false,
                EnteredBy = userId.ToString(),
                EnteredDate = DateTime.UtcNow
            };
            db.Patients.Add(created);
            await db.SaveChangesAsync(cancellationToken);
            patientId = created.PatientId;
        }

        db.PatientUserMaps.Add(new PatientUserMap
        {
            UserId = userId,
            PatientId = patientId,
            IsPrimary = true,
            DeleteStatus = false,
            EnteredBy = userId.ToString(),
            EnteredDate = DateTime.UtcNow
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsPrimaryMapDuplicate(ex))
        {
            db.ChangeTracker.Clear();
            var existing = await FindMappedOwnerAsync(db, userId, cancellationToken);
            if (existing != null)
                return existing;
            throw;
        }

        return new PatientPortalOwner
        {
            PatientId = patientId,
            PatientName = name,
            OwnerUserId = userId,
            IsActingAsCaregiver = false
        };
    }

    private static bool IsPrimaryMapDuplicate(Exception ex)
    {
        for (var inner = ex; inner != null; inner = inner.InnerException)
        {
            if (inner is SqlException sql && (sql.Number == 2601 || sql.Number == 2627))
                return true;
            if (inner.Message.IndexOf("UX_PatientUserMap_User_Primary", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }
}
