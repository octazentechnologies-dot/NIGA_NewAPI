/*
================================================================================
Created      : 27-10-2026
Script       : 17_Doctor_TimeOff_Public_Discovery.sql
Purpose      : Document + index support for clinic leave vs public discovery.
               Product rules (enforced in API, not by flipping DirectoryVisible):
                 - Active DoctorTimeOff = clinic leave
                 - Public Find a Doctor excludes doctors on active leave
                 - Doctor.DirectoryVisible preference is preserved
                 - Doctor.IsOnline (tele) remains independently controllable
               Earlier script 16 forced IsOnline=0 on leave; that rule is retired.
Use          : HomeoCentrum_Dev / Stage / Production. Idempotent. Manual run.
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

SELECT
    (SELECT COUNT(*) FROM dbo.DoctorTimeOff WHERE StartsAt <= @now AND EndsAt >= @now) AS ActiveClinicLeaveRows,
    (SELECT COUNT(*) FROM dbo.Doctor WHERE DeleteStatus = 0 AND DirectoryVisible = 1) AS DirectoryVisibleDoctors;
GO
