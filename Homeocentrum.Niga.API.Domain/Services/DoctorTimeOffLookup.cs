using System.Threading;
using Homeocentrum.Niga.API.Domain.Data;
using Microsoft.EntityFrameworkCore;

namespace Homeocentrum.Niga.API.Domain.Services;

/// <summary>
/// Shared check for an active DoctorTimeOff window covering "now".
/// Used by directory visibility and availability so leave state stays consistent.
/// </summary>
public static class DoctorTimeOffLookup
{
    private static int _doctorTimeOffReady;

    public static async Task<bool> IsOnLeaveAsync(NIGACentrumContext context, int doctorId, CancellationToken cancellationToken = default)
    {
        await EnsureDoctorTimeOffTableAsync(context, cancellationToken);
        var now = DateTime.Now;
        var hit = await context.Database.SqlQuery<TimeOffHitRow>($@"
            SELECT TOP 1 DoctorTimeOffId AS Id
            FROM dbo.DoctorTimeOff
            WHERE DoctorId = {doctorId}
              AND StartsAt <= {now}
              AND EndsAt >= {now}").FirstOrDefaultAsync(cancellationToken);
        return hit != null;
    }

    private static async Task EnsureDoctorTimeOffTableAsync(NIGACentrumContext context, CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _doctorTimeOffReady) == 1)
        {
            return;
        }

        await context.Database.ExecuteSqlRawAsync("""
            IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL
            CREATE TABLE dbo.DoctorTimeOff (
                DoctorTimeOffId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
                DoctorId int NOT NULL,
                StartsAt datetime2(0) NOT NULL,
                EndsAt datetime2(0) NOT NULL,
                Reason nvarchar(500) NULL
            );
            """, cancellationToken);

        Volatile.Write(ref _doctorTimeOffReady, 1);
    }

    private sealed class TimeOffHitRow
    {
        public int Id { get; set; }
    }
}
