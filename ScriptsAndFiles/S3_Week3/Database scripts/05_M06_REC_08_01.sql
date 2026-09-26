/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M06_REC_08_01.sql
Purpose      : REC-08.01 — Waiting queue uses existing dbo.PatientAppointment.
               No new WaitingQueue / ReceptionQueue table.
               Order = Status + AppointmentDate/Time (and PatientAppId tie-break).
               Optional column: QueuePosition INT NULL (manual override later).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.PatientAppointment exists.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment is missing. Stop.', 16, 1);
    RETURN;
END
GO

-- REC-08.01 — do not invent a separate queue table.
IF OBJECT_ID(N'dbo.WaitingQueue', N'U') IS NOT NULL
    RAISERROR('dbo.WaitingQueue must not exist. REC-08.01 reuses PatientAppointment.', 16, 1);
IF OBJECT_ID(N'dbo.ReceptionQueue', N'U') IS NOT NULL
    RAISERROR('dbo.ReceptionQueue must not exist. REC-08.01 reuses PatientAppointment.', 16, 1);
GO

-- Required for status + time order (must already exist on this product).
IF COL_LENGTH(N'dbo.PatientAppointment', N'Status') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment.Status is required for queue order.', 16, 1);
    RETURN;
END
IF COL_LENGTH(N'dbo.PatientAppointment', N'AppointmentTime') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment.AppointmentTime is required for queue order.', 16, 1);
    RETURN;
END
GO

-- Optional QueuePosition for later explicit ordering; nullable so status+time remain enough.
IF COL_LENGTH(N'dbo.PatientAppointment', N'QueuePosition') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD QueuePosition INT NULL;
GO

PRINT 'M06_REC_08_01.sql: no new queue table. Optional QueuePosition on PatientAppointment.';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.PatientAppointment')
  AND c.name IN (
        N'PatientAppId', N'DoctorId', N'Status', N'AppointmentDate',
        N'AppointmentTime', N'PaymentStatus', N'QueuePosition'
  )
ORDER BY c.column_id;

SELECT
    CASE WHEN OBJECT_ID(N'dbo.WaitingQueue', N'U') IS NULL THEN N'absent' ELSE N'present' END AS WaitingQueueTable,
    CASE WHEN OBJECT_ID(N'dbo.ReceptionQueue', N'U') IS NULL THEN N'absent' ELSE N'present' END AS ReceptionQueueTable,
    CASE WHEN COL_LENGTH(N'dbo.PatientAppointment', N'QueuePosition') IS NULL THEN N'missing' ELSE N'present' END AS QueuePositionColumn;

SELECT TOP 5
    PatientAppId,
    DoctorId,
    Status,
    AppointmentDate,
    AppointmentTime,
    PaymentStatus,
    QueuePosition
FROM dbo.PatientAppointment
WHERE ISNULL(DeleteStatus, 0) = 0
ORDER BY AppointmentDate DESC, AppointmentTime, PatientAppId;
GO
