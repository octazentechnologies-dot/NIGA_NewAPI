/*
================================================================================
Author       : Tufan Powar
Created      : 07-10-2026
Script       : 00_DOC_DoctorDailySchedule_Table.sql
Purpose      : Per-day working hours and slot interval for a doctor.
               Read by API slots (Public/Doctors/{id}/Slots, reception and
               doctor schedule). Script 10 and S3 01 expect this table to exist.
Use          : Run first in S2_Week2, before 01–28. Safe on any environment.
               S3_Week3 01_S3_Week3_Schema.sql adds BreakStartTime / BreakEndTime.
Idempotent   : Yes. Creates only what is missing; never drops or rewrites data.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
BEGIN
    RAISERROR('dbo.Doctor is missing. Restore the base HomeoCentrum schema first.', 16, 1);
    RETURN;
END

IF OBJECT_ID(N'dbo.DoctorDailySchedule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorDailySchedule
    (
        DoctorDailyScheduleId INT IDENTITY(1, 1) NOT NULL PRIMARY KEY CLUSTERED,
        DoctorId              INT          NOT NULL,
        ScheduleDate          DATE         NOT NULL,
        SlotIntervalMinutes   INT          NOT NULL,
        WorkStartTime         TIME(0)      NOT NULL,
        WorkEndTime           TIME(0)      NOT NULL,
        CreatedByUserId       BIGINT       NOT NULL,
        CreatedAt             DATETIME2(0) NOT NULL CONSTRAINT DF_DoctorDailySchedule_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.DoctorDailySchedule.';
END
ELSE
    PRINT 'dbo.DoctorDailySchedule already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DoctorDailySchedule') AND name = N'UQ_DoctorDailySchedule_Doctor_Date')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_DoctorDailySchedule_Doctor_Date
        ON dbo.DoctorDailySchedule (DoctorId, ScheduleDate);
    PRINT 'Created UQ_DoctorDailySchedule_Doctor_Date.';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE parent_object_id = OBJECT_ID(N'dbo.DoctorDailySchedule') AND name = N'FK_DoctorDailySchedule_Doctor')
BEGIN
    ALTER TABLE dbo.DoctorDailySchedule WITH CHECK
        ADD CONSTRAINT FK_DoctorDailySchedule_Doctor FOREIGN KEY (DoctorId) REFERENCES dbo.Doctor (DoctorID);
    PRINT 'Created FK_DoctorDailySchedule_Doctor.';
END

SELECT
    OBJECT_ID(N'dbo.DoctorDailySchedule', N'U') AS TableObjectId,
    (SELECT COUNT(*) FROM dbo.DoctorDailySchedule) AS ScheduleRows;
