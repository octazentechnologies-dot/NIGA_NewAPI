/*
================================================================================
Created      : 27-10-2026
Script       : 06_S5_Doctor_Reminder_Fire.sql
Purpose      : Fire / snooze lifecycle for doctor web reminders.
               Adds FiredAt, SnoozedUntil, LastNotifiedAt used by:
                 POST /api/Doctor/Reminders/{id}/actions
               API also ensures these columns at runtime (idempotent).
Use          : HomeoCentrum_Dev / Stage / Production. Idempotent. Manual run.
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.DoctorReminder', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.DoctorReminder', N'FiredAt') IS NULL
        ALTER TABLE dbo.DoctorReminder ADD FiredAt DATETIME2(0) NULL;

    IF COL_LENGTH(N'dbo.DoctorReminder', N'SnoozedUntil') IS NULL
        ALTER TABLE dbo.DoctorReminder ADD SnoozedUntil DATETIME2(0) NULL;

    IF COL_LENGTH(N'dbo.DoctorReminder', N'LastNotifiedAt') IS NULL
        ALTER TABLE dbo.DoctorReminder ADD LastNotifiedAt DATETIME2(0) NULL;
END
GO

SELECT
    COL_LENGTH(N'dbo.DoctorReminder', N'FiredAt') AS FiredAtCol,
    COL_LENGTH(N'dbo.DoctorReminder', N'SnoozedUntil') AS SnoozedUntilCol,
    COL_LENGTH(N'dbo.DoctorReminder', N'LastNotifiedAt') AS LastNotifiedAtCol;
GO
