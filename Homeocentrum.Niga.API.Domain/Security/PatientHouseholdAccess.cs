using Homeocentrum.Niga.API.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Security;

/// <summary>
/// Patient-login access to another patient profile: own map, family household (owner), or active caregiver.
/// Used so the main family account can operate on members' visits, eRx, check-in, and medicine flows.
/// </summary>
public static class PatientHouseholdAccess
{
    public static async Task<bool> UserCanAccessPatientAsync(
        NIGACentrumContext context,
        long userId,
        int patientId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0 || patientId <= 0 || context == null)
            return false;

        if (await context.PatientUserMaps.AsNoTracking().AnyAsync(
                m => m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus,
                cancellationToken))
            return true;

        if (await context.PatientFamilyMembers.AsNoTracking().AnyAsync(
                f => f.OwnerUserId == userId && f.MemberPatientId == patientId && !f.DeleteStatus,
                cancellationToken))
            return true;

        if (await context.CaregiverAuthorizations.AsNoTracking().AnyAsync(
                c => c.CaregiverUserId == userId
                     && c.PatientId == patientId
                     && !c.DeleteStatus
                     && c.RevokedAt == null,
                cancellationToken))
            return true;

        return false;
    }
}
