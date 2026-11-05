using Homeocentrum.Niga.API.Domain.Data;
using Homeocentrum.Niga.API.Domain.Master;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Helpers;

/// <summary>
/// Ensures a patient is on this doctor's <b>clinic patient roster</b> (minimal CaseEntryDetail)
/// so All Patients / New Appointment dropdown (getAllCases) includes mobile/public bookers.
/// Idempotent. Not related to doctor-to-doctor CareLink second opinions.
/// </summary>
public static class ClinicPatientRoster
{
    public static async Task EnsureCaseAsync(
        NIGACentrumContext context,
        int doctorId,
        int patientId,
        long? doctorUserId = null,
        string? linkedVia = null,
        CancellationToken cancellationToken = default)
    {
        if (doctorId <= 0 || patientId <= 0)
            return;

        var exists = await context.CaseEntryDetails.AsNoTracking()
            .AnyAsync(c =>
                c.PatientId == patientId
                && c.DoctorId == doctorId
                && c.DeleteStatus != true, cancellationToken);
        if (exists)
            return;

        var doctor = await context.Doctors.AsNoTracking()
            .Where(d => d.DoctorId == doctorId && d.DeleteStatus != true)
            .Select(d => new { d.DoctorId, d.UserId })
            .FirstOrDefaultAsync(cancellationToken);
        if (doctor == null)
            return;

        var patientOk = await context.Patients.AsNoTracking()
            .AnyAsync(p => p.PatientId == patientId && p.DeleteStatus != true, cancellationToken);
        if (!patientOk)
            return;

        var now = DateTime.Now;
        var via = string.IsNullOrWhiteSpace(linkedVia) ? "Booking" : linkedVia.Trim();
        if (via.Length > 50) via = via[..50];
        int? userId = doctor.UserId;
        if (doctorUserId is > 0 and <= int.MaxValue)
            userId = (int)doctorUserId.Value;
        context.CaseEntryDetails.Add(new CaseEntryDetail
        {
            PatientId = patientId,
            DoctorId = doctor.DoctorId,
            UserId = userId,
            DateodFirstVisit = now,
            DeleteStatus = false,
            EnteredBy = via,
            EnteredDate = now,
            RefBy = via
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public static void EnsureCase(
        NIGACentrumContext context,
        int doctorId,
        int patientId,
        long? doctorUserId = null,
        string? linkedVia = null)
    {
        EnsureCaseAsync(context, doctorId, patientId, doctorUserId, linkedVia)
            .GetAwaiter()
            .GetResult();
    }
}
