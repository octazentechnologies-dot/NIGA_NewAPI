/*
================================================================================
Created      : 27-10-2026
Script       : 16_Doctor_TimeOff_Force_Offline.sql
Purpose      : Keep Doctor.IsOnline in sync with active leave (DoctorTimeOff).
               - Ensures dbo.DoctorTimeOff exists (idempotent).
               - Sets IsOnline = 0 for doctors whose leave covers "now".
               - Optionally clears TeleAvailability.IsOnline when that table exists.
Use          : HomeoCentrum_Dev / Stage / Production. Idempotent. Manual run
               when promoting environments (same folder as other S5 scripts).
App behavior : Also enforced at runtime by:
                 POST /api/Doctor/TimeOff  (SaveTimeOffAsync)
                 GET/PUT /api/Availability/Me
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorTimeOff
    (
        DoctorTimeOffId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorTimeOff PRIMARY KEY,
        DoctorId        INT            NOT NULL,
        StartsAt        DATETIME2(0)   NOT NULL,
        EndsAt          DATETIME2(0)   NOT NULL,
        Reason          NVARCHAR(500)  NULL
    );
END
GO

IF OBJECT_ID(N'dbo.DoctorTimeOff', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_DoctorTimeOff_Doctor_Range'
          AND object_id = OBJECT_ID(N'dbo.DoctorTimeOff')
   )
BEGIN
    CREATE INDEX IX_DoctorTimeOff_Doctor_Range
        ON dbo.DoctorTimeOff (DoctorId, StartsAt, EndsAt);
END
GO

DECLARE @now DATETIME2(0) = SYSDATETIME();

UPDATE d
SET
    d.IsOnline = 0,
    d.ChangedDate = SYSUTCDATETIME()
FROM dbo.Doctor AS d
WHERE d.DeleteStatus = 0
  AND d.IsOnline = 1
  AND EXISTS (
        SELECT 1
        FROM dbo.DoctorTimeOff AS t
        WHERE t.DoctorId = d.DoctorId
          AND t.StartsAt <= @now
          AND t.EndsAt >= @now
  );

DECLARE @offlineDoctors INT = @@ROWCOUNT;

IF OBJECT_ID(N'dbo.TeleAvailability', N'U') IS NOT NULL
BEGIN
    UPDATE ta
    SET
        ta.IsOnline = 0,
        ta.LastHeartbeat = @now
    FROM dbo.TeleAvailability AS ta
    WHERE ta.IsOnline = 1
      AND EXISTS (
            SELECT 1
            FROM dbo.DoctorTimeOff AS t
            WHERE t.DoctorId = ta.DoctorId
              AND t.StartsAt <= @now
              AND t.EndsAt >= @now
      );
END

SELECT
    @offlineDoctors AS DoctorsForcedOffline,
    (SELECT COUNT(*) FROM dbo.DoctorTimeOff WHERE StartsAt <= @now AND EndsAt >= @now) AS ActiveTimeOffRows;
GO
