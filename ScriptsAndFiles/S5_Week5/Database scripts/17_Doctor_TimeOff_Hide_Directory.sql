/*
================================================================================
Created      : 27-10-2026
Script       : 17_Doctor_TimeOff_Hide_Directory.sql
Purpose      : When a doctor has ACTIVE leave (covers now), default them to:
                 - IsOnline = 0
                 - DirectoryVisible = 0  (Hidden from Find a Doctor)
               Clinic is closed; they may manually re-enable Online / Public
               Visibility for tele consultation via the profile menu.
Use          : HomeoCentrum_Dev / Stage / Production. Idempotent. Manual run.
App behavior : Also applied at runtime by POST /api/Doctor/TimeOff (active leave).
Depends on   : 16_Doctor_TimeOff_Force_Offline.sql (optional; this script is fuller).
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

DECLARE @now DATETIME2(0) = SYSDATETIME();

UPDATE d
SET
    d.IsOnline = 0,
    d.DirectoryVisible = 0,
    d.ChangedDate = SYSUTCDATETIME()
FROM dbo.Doctor AS d
WHERE d.DeleteStatus = 0
  AND EXISTS (
        SELECT 1
        FROM dbo.DoctorTimeOff AS t
        WHERE t.DoctorId = d.DoctorId
          AND t.StartsAt <= @now
          AND t.EndsAt >= @now
  )
  AND (d.IsOnline = 1 OR d.DirectoryVisible = 1);

DECLARE @updated INT = @@ROWCOUNT;

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
    @updated AS DoctorsDefaultedForActiveLeave,
    (SELECT COUNT(*) FROM dbo.DoctorTimeOff WHERE StartsAt <= @now AND EndsAt >= @now) AS ActiveTimeOffRows;
GO
