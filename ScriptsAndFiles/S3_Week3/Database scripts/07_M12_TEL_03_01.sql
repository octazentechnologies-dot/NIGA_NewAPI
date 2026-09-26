/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_03_01.sql
Purpose      : TEL-03.01 — dbo.TeleSession for in-browser video consult sessions.
               Required columns: PatientAppId, RoomId, Status.
               Also: TeleSessionId (PK), DoctorId, PatientId, RecordAllowed,
               StartedAt, EndedAt, CreatedAt (used by TEL-02/TEL-03 APIs).
               Status values in use: Waiting | Active | Ended.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.PatientAppointment exists (soft link by PatientAppId).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.TeleSession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleSession
    (
        TeleSessionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleSession PRIMARY KEY,
        PatientAppId INT NOT NULL,
        DoctorId INT NOT NULL,
        PatientId INT NOT NULL,
        RoomId NVARCHAR(64) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        RecordAllowed BIT NOT NULL CONSTRAINT DF_TeleSession_RecordAllowed DEFAULT (0),
        StartedAt DATETIME NULL,
        EndedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_TeleSession_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.TeleSession', N'PatientAppId') IS NULL
    ALTER TABLE dbo.TeleSession ADD PatientAppId INT NOT NULL
        CONSTRAINT DF_TeleSession_PatientAppId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleSession', N'RoomId') IS NULL
    ALTER TABLE dbo.TeleSession ADD RoomId NVARCHAR(64) NOT NULL
        CONSTRAINT DF_TeleSession_RoomId_Add DEFAULT (N'pending');
IF COL_LENGTH(N'dbo.TeleSession', N'Status') IS NULL
    ALTER TABLE dbo.TeleSession ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_TeleSession_Status_Add DEFAULT (N'Waiting');
IF COL_LENGTH(N'dbo.TeleSession', N'DoctorId') IS NULL
    ALTER TABLE dbo.TeleSession ADD DoctorId INT NOT NULL
        CONSTRAINT DF_TeleSession_DoctorId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleSession', N'PatientId') IS NULL
    ALTER TABLE dbo.TeleSession ADD PatientId INT NOT NULL
        CONSTRAINT DF_TeleSession_PatientId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleSession', N'RecordAllowed') IS NULL
    ALTER TABLE dbo.TeleSession ADD RecordAllowed BIT NOT NULL
        CONSTRAINT DF_TeleSession_RecordAllowed_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleSession', N'StartedAt') IS NULL
    ALTER TABLE dbo.TeleSession ADD StartedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.TeleSession', N'EndedAt') IS NULL
    ALTER TABLE dbo.TeleSession ADD EndedAt DATETIME NULL;
IF COL_LENGTH(N'dbo.TeleSession', N'CreatedAt') IS NULL
    ALTER TABLE dbo.TeleSession ADD CreatedAt DATETIME NOT NULL
        CONSTRAINT DF_TeleSession_CreatedAt_Add DEFAULT (GETDATE());
GO

-- Defaults when table pre-existed without them
IF COL_LENGTH(N'dbo.TeleSession', N'RecordAllowed') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TeleSession')
          AND c.name = N'RecordAllowed'
   )
    ALTER TABLE dbo.TeleSession
        ADD CONSTRAINT DF_TeleSession_RecordAllowed DEFAULT (0) FOR RecordAllowed;
GO

IF COL_LENGTH(N'dbo.TeleSession', N'CreatedAt') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TeleSession')
          AND c.name = N'CreatedAt'
   )
    ALTER TABLE dbo.TeleSession
        ADD CONSTRAINT DF_TeleSession_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TeleSession')
      AND name = N'IX_TeleSession_PatientApp'
)
    CREATE INDEX IX_TeleSession_PatientApp ON dbo.TeleSession (PatientAppId, Status);
GO

-- Hard fail if required TEL-03.01 columns are missing
IF COL_LENGTH(N'dbo.TeleSession', N'PatientAppId') IS NULL
   OR COL_LENGTH(N'dbo.TeleSession', N'RoomId') IS NULL
   OR COL_LENGTH(N'dbo.TeleSession', N'Status') IS NULL
BEGIN
    RAISERROR('TEL-03.01 required columns PatientAppId, RoomId, Status are missing on dbo.TeleSession.', 16, 1);
    RETURN;
END
GO

PRINT 'M12_TEL_03_01.sql: TeleSession ready (PatientAppId, RoomId, Status).';

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
WHERE c.object_id = OBJECT_ID(N'dbo.TeleSession')
ORDER BY c.column_id;

SELECT TOP 5
    TeleSessionId,
    PatientAppId,
    RoomId,
    Status,
    DoctorId,
    CreatedAt
FROM dbo.TeleSession
ORDER BY TeleSessionId DESC;
GO
