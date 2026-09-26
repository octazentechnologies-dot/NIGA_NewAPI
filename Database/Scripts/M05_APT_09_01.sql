/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M05_APT_09_01.sql
Purpose      : APT-09.01 — PatientAppointment.PaymentStatus, PaymentMethod,
               PayAtClinicAllowed. PaymentStatus default UNPAID.
               Does not change existing PAID / PENDING / other values.
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

IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentStatus') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PaymentStatus NVARCHAR(30) NULL
        CONSTRAINT DF_PatientAppointment_PaymentStatus DEFAULT (N'UNPAID');
IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentMethod') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PaymentMethod NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'PayAtClinicAllowed') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PayAtClinicAllowed BIT NULL;
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentStatus') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.PatientAppointment')
          AND c.name = N'PaymentStatus'
   )
    ALTER TABLE dbo.PatientAppointment
        ADD CONSTRAINT DF_PatientAppointment_PaymentStatus DEFAULT (N'UNPAID') FOR PaymentStatus;
GO

UPDATE dbo.PatientAppointment
SET PaymentStatus = N'UNPAID'
WHERE PaymentStatus IS NULL
   OR LTRIM(RTRIM(PaymentStatus)) = N'';
GO

PRINT 'M05_APT_09_01.sql complete';
SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    dc.definition AS DefaultDefinition
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id = c.object_id
   AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID(N'dbo.PatientAppointment')
  AND c.name IN (N'PaymentStatus', N'PaymentMethod', N'PayAtClinicAllowed')
ORDER BY c.column_id;

SELECT
    COUNT(*) AS TotalRows,
    SUM(CASE WHEN PaymentStatus = N'UNPAID' THEN 1 ELSE 0 END) AS UnpaidCount,
    SUM(CASE WHEN PaymentStatus IS NULL OR LTRIM(RTRIM(PaymentStatus)) = N'' THEN 1 ELSE 0 END) AS BlankPaymentStatus
FROM dbo.PatientAppointment;
GO
