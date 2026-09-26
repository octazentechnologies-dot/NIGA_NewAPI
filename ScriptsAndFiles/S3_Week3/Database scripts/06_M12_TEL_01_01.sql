/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_01_01.sql
Purpose      : TEL-01.01 — dbo.TeleAvailability for teleconsult Online/Offline.
               Columns: DoctorId (PK), IsOnline, LastHeartbeat.
               One row per doctor. No duplicate availability table.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.Doctor exists (FK optional — soft link by DoctorId).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.TeleAvailability', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleAvailability
    (
        DoctorId INT NOT NULL CONSTRAINT PK_TeleAvailability PRIMARY KEY,
        IsOnline BIT NOT NULL CONSTRAINT DF_TeleAvailability_IsOnline DEFAULT (0),
        LastHeartbeat DATETIME NULL
    );
END
GO

IF COL_LENGTH(N'dbo.TeleAvailability', N'DoctorId') IS NULL
BEGIN
    RAISERROR('dbo.TeleAvailability.DoctorId is required.', 16, 1);
    RETURN;
END
IF COL_LENGTH(N'dbo.TeleAvailability', N'IsOnline') IS NULL
    ALTER TABLE dbo.TeleAvailability ADD IsOnline BIT NOT NULL
        CONSTRAINT DF_TeleAvailability_IsOnline_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleAvailability', N'LastHeartbeat') IS NULL
    ALTER TABLE dbo.TeleAvailability ADD LastHeartbeat DATETIME NULL;
GO

-- Ensure default on IsOnline when table pre-existed without it.
IF COL_LENGTH(N'dbo.TeleAvailability', N'IsOnline') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TeleAvailability')
          AND c.name = N'IsOnline'
   )
    ALTER TABLE dbo.TeleAvailability
        ADD CONSTRAINT DF_TeleAvailability_IsOnline DEFAULT (0) FOR IsOnline;
GO

PRINT 'M12_TEL_01_01.sql: TeleAvailability ready (Online/Offline + LastHeartbeat).';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable,
    dc.definition AS DefaultDefinition
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id = c.object_id
   AND dc.parent_column_id = c.column_id
WHERE c.object_id = OBJECT_ID(N'dbo.TeleAvailability')
ORDER BY c.column_id;

SELECT TOP 5
    DoctorId,
    IsOnline,
    LastHeartbeat
FROM dbo.TeleAvailability
ORDER BY DoctorId;
GO
