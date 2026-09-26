/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_02_01.sql
Purpose      : TEL-02.01 — Queue query on E-CONSULT + PaymentStatus + join time.
               Reuses dbo.PatientAppointment (no new TeleQueue table).
               Ensures PaymentStatus + CalledAt (join time) exist.
               Creates dbo.vw_TeleWaitingQueue for today’s E-CONSULT rows with:
                 PaymentStatus, JoinTime, WaitMinutes.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.PatientAppointment exists (APT-09 PaymentStatus, S3 CalledAt).
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

-- TEL-02.01 — do not invent a separate tele queue table.
IF OBJECT_ID(N'dbo.TeleWaitingQueue', N'U') IS NOT NULL
    RAISERROR('dbo.TeleWaitingQueue must not exist. TEL-02.01 reuses PatientAppointment.', 16, 1);
IF OBJECT_ID(N'dbo.TeleQueue', N'U') IS NOT NULL
    RAISERROR('dbo.TeleQueue must not exist. TEL-02.01 reuses PatientAppointment.', 16, 1);
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'Status') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment.Status is required for E-CONSULT queue filter.', 16, 1);
    RETURN;
END
IF COL_LENGTH(N'dbo.PatientAppointment', N'AppointmentDate') IS NULL
   OR COL_LENGTH(N'dbo.PatientAppointment', N'AppointmentTime') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment.AppointmentDate/Time are required for join time.', 16, 1);
    RETURN;
END
GO

-- Payment status (who has paid) — default UNPAID when column is added.
IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentStatus') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PaymentStatus NVARCHAR(30) NULL
        CONSTRAINT DF_PatientAppointment_PaymentStatus_TEL0201 DEFAULT (N'UNPAID');
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentStatus') IS NOT NULL
BEGIN
    UPDATE dbo.PatientAppointment
    SET PaymentStatus = N'UNPAID'
    WHERE PaymentStatus IS NULL
       OR LTRIM(RTRIM(PaymentStatus)) = N'';

    IF NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PatientAppointment')
          AND c.name = N'PaymentStatus'
    )
        ALTER TABLE dbo.PatientAppointment
            ADD CONSTRAINT DF_PatientAppointment_PaymentStatus_TEL0201 DEFAULT (N'UNPAID') FOR PaymentStatus;
END
GO

-- Join time: CalledAt when receptionist/doctor called the patient; else slot start.
IF COL_LENGTH(N'dbo.PatientAppointment', N'CalledAt') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CalledAt DATETIME NULL;
GO

-- Canonical tele waiting-queue query (TEL-02.01). API (TEL-02.02) may mirror this in EF.
CREATE OR ALTER VIEW dbo.vw_TeleWaitingQueue
AS
SELECT
    pa.PatientAppId,
    pa.DoctorId,
    pa.PatientId,
    pa.Status,
    pa.AppointmentDate,
    pa.AppointmentTime,
    pa.ConsultMode,
    pa.IsTele,
    COALESCE(NULLIF(LTRIM(RTRIM(pa.PaymentStatus)), N''), N'UNPAID') AS PaymentStatus,
    COALESCE(
        pa.CalledAt,
        CASE
            WHEN pa.AppointmentDate IS NOT NULL AND pa.AppointmentTime IS NOT NULL
                THEN DATEADD(
                        MILLISECOND,
                        DATEDIFF(MILLISECOND, 0, CAST(pa.AppointmentTime AS DATETIME)),
                        CAST(CAST(pa.AppointmentDate AS DATE) AS DATETIME))
            ELSE NULL
        END
    ) AS JoinTime,
    CASE
        WHEN COALESCE(
                pa.CalledAt,
                CASE
                    WHEN pa.AppointmentDate IS NOT NULL AND pa.AppointmentTime IS NOT NULL
                        THEN DATEADD(
                                MILLISECOND,
                                DATEDIFF(MILLISECOND, 0, CAST(pa.AppointmentTime AS DATETIME)),
                                CAST(CAST(pa.AppointmentDate AS DATE) AS DATETIME))
                    ELSE NULL
                END
             ) IS NULL THEN 0
        ELSE DATEDIFF(
                MINUTE,
                COALESCE(
                    pa.CalledAt,
                    DATEADD(
                        MILLISECOND,
                        DATEDIFF(MILLISECOND, 0, CAST(pa.AppointmentTime AS DATETIME)),
                        CAST(CAST(pa.AppointmentDate AS DATE) AS DATETIME))),
                GETDATE())
    END AS WaitMinutes,
    pa.CalledAt,
    pa.QueuePosition
FROM dbo.PatientAppointment pa
WHERE ISNULL(pa.DeleteStatus, 0) = 0
  AND pa.Status = N'E-CONSULT';
GO

PRINT 'M12_TEL_02_01.sql: vw_TeleWaitingQueue ready (E-CONSULT + PaymentStatus + JoinTime).';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.PatientAppointment')
  AND c.name IN (
        N'PatientAppId', N'DoctorId', N'Status', N'AppointmentDate',
        N'AppointmentTime', N'PaymentStatus', N'CalledAt', N'ConsultMode', N'IsTele'
  )
ORDER BY c.column_id;

SELECT
    CASE WHEN OBJECT_ID(N'dbo.TeleWaitingQueue', N'U') IS NULL THEN N'absent' ELSE N'present' END AS TeleWaitingQueueTable,
    CASE WHEN OBJECT_ID(N'dbo.TeleQueue', N'U') IS NULL THEN N'absent' ELSE N'present' END AS TeleQueueTable,
    CASE WHEN OBJECT_ID(N'dbo.vw_TeleWaitingQueue', N'V') IS NULL THEN N'missing' ELSE N'present' END AS TeleWaitingQueueView,
    CASE WHEN COL_LENGTH(N'dbo.PatientAppointment', N'PaymentStatus') IS NULL THEN N'missing' ELSE N'present' END AS PaymentStatusColumn,
    CASE WHEN COL_LENGTH(N'dbo.PatientAppointment', N'CalledAt') IS NULL THEN N'missing' ELSE N'present' END AS CalledAtColumn;

SELECT TOP 5
    PatientAppId,
    DoctorId,
    Status,
    PaymentStatus,
    JoinTime,
    WaitMinutes,
    AppointmentDate,
    AppointmentTime
FROM dbo.vw_TeleWaitingQueue
WHERE DoctorId = 1010
  AND AppointmentDate >= CAST(CAST(GETDATE() AS DATE) AS DATETIME)
  AND AppointmentDate < DATEADD(DAY, 1, CAST(CAST(GETDATE() AS DATE) AS DATETIME))
ORDER BY AppointmentTime, PatientAppId;
GO
