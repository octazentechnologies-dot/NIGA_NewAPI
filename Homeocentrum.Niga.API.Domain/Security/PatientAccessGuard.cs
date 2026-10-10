using System.Security.Claims;
using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Security;

public interface IPatientAccessGuard
{
    /// <summary>
    /// True when the caller may read or change this patient: global admin, the clinic doctor (or its reception),
    /// a doctor with an appointment for the patient, or the patient's own / family login.
    /// </summary>
    Task<bool> CanAccessPatientAsync(ClaimsPrincipal user, int patientId, int? resourceDoctorId = null);

    /// <summary>Null for global admins (no filter), otherwise the patient ids the caller's clinic may see.</summary>
    Task<HashSet<int>?> VisiblePatientIdsAsync(ClaimsPrincipal user);
}

public sealed class PatientAccessGuard : IPatientAccessGuard
{
    private readonly NIGACentrumContext _context;

    public PatientAccessGuard(NIGACentrumContext context) => _context = context;

    public async Task<bool> CanAccessPatientAsync(ClaimsPrincipal user, int patientId, int? resourceDoctorId = null)
    {
        if (patientId <= 0 || user?.Identity?.IsAuthenticated != true)
            return false;
        if (DoctorOwnership.IsGlobalAdminPortalUser(user))
            return true;

        resourceDoctorId ??= await _context.CaseEntryDetails.AsNoTracking()
            .Where(c => c.PatientId == patientId && c.DeleteStatus != true)
            .Select(c => (int?)c.DoctorId)
            .FirstOrDefaultAsync();
        if (resourceDoctorId.HasValue && DoctorOwnership.EnsureDoctorOwns(user, resourceDoctorId.Value))
            return true;

        var jwtDoctor = DoctorOwnership.GetDoctorId(user);
        if (jwtDoctor.HasValue)
        {
            return await _context.PatientAppointments.AsNoTracking().AnyAsync(a =>
                a.PatientId == patientId && a.DoctorId == jwtDoctor.Value && a.DeleteStatus != true);
        }

        long userId;
        try { userId = user.GetUserId(); }
        catch { return false; }
        return await _context.PatientUserMaps.AsNoTracking().AnyAsync(m =>
                   m.UserId == userId && m.PatientId == patientId && !m.DeleteStatus)
               || await _context.PatientFamilyMembers.AsNoTracking().AnyAsync(f =>
                   f.OwnerUserId == userId && f.MemberPatientId == patientId && !f.DeleteStatus);
    }

    public async Task<HashSet<int>?> VisiblePatientIdsAsync(ClaimsPrincipal user)
    {
        if (DoctorOwnership.IsGlobalAdminPortalUser(user))
            return null;
        var jwtDoctor = DoctorOwnership.GetDoctorId(user);
        if (!jwtDoctor.HasValue)
            return new HashSet<int>();
        var fromCases = await _context.CaseEntryDetails.AsNoTracking()
            .Where(c => c.DoctorId == jwtDoctor.Value && c.DeleteStatus != true)
            .Select(c => c.PatientId)
            .ToListAsync();
        var fromAppointments = await _context.PatientAppointments.AsNoTracking()
            .Where(a => a.DoctorId == jwtDoctor.Value && a.DeleteStatus != true)
            .Select(a => a.PatientId)
            .ToListAsync();
        var ids = new HashSet<int>(fromCases);
        ids.UnionWith(fromAppointments);
        return ids;
    }
}
