/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_07_01.sql
Purpose      : TEL-07.01 — Recording consent persistence for teleconsult.
               1) dbo.TeleConsentLog — per-role accept/decline events
                  (TeleSessionId, PatientAppId, Accepted, ByRole, At).
               2) ConsentType Code=TeleRecording (active) so ConsentRecord
                  dual-writes reuse SEC-06 ConsentRecord (no fork table).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.ConsentType + dbo.ConsentRecord (S1 SEC-06).
               dbo.TeleSession recommended (soft link by TeleSessionId).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* --- TeleConsentLog (session consent events; decline still inserts a row) --- */
IF OBJECT_ID(N'dbo.TeleConsentLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleConsentLog
    (
        TeleConsentLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleConsentLog PRIMARY KEY,
        TeleSessionId INT NOT NULL,
        PatientAppId INT NOT NULL,
        Accepted BIT NOT NULL,
        ByRole NVARCHAR(30) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_TeleConsentLog_At DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.TeleConsentLog', N'TeleSessionId') IS NULL
    ALTER TABLE dbo.TeleConsentLog ADD TeleSessionId INT NOT NULL
        CONSTRAINT DF_TeleConsentLog_TeleSessionId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleConsentLog', N'PatientAppId') IS NULL
    ALTER TABLE dbo.TeleConsentLog ADD PatientAppId INT NOT NULL
        CONSTRAINT DF_TeleConsentLog_PatientAppId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleConsentLog', N'Accepted') IS NULL
    ALTER TABLE dbo.TeleConsentLog ADD Accepted BIT NOT NULL
        CONSTRAINT DF_TeleConsentLog_Accepted_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleConsentLog', N'ByRole') IS NULL
    ALTER TABLE dbo.TeleConsentLog ADD ByRole NVARCHAR(30) NOT NULL
        CONSTRAINT DF_TeleConsentLog_ByRole_Add DEFAULT (N'Doctor');
IF COL_LENGTH(N'dbo.TeleConsentLog', N'At') IS NULL
    ALTER TABLE dbo.TeleConsentLog ADD At DATETIME NOT NULL
        CONSTRAINT DF_TeleConsentLog_At_Add DEFAULT (GETDATE());
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TeleConsentLog')
      AND name = N'IX_TeleConsentLog_TeleSessionId'
)
    CREATE INDEX IX_TeleConsentLog_TeleSessionId
        ON dbo.TeleConsentLog (TeleSessionId, TeleConsentLogId DESC);
GO

/* --- ConsentType TeleRecording (ConsentRecord.ConsentTypeId points here) --- */
IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NULL
BEGIN
    RAISERROR('dbo.ConsentType is required (S1 SEC-06). Run foundation consent scripts first.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM dbo.ConsentType WHERE Code = N'TeleRecording')
BEGIN
    INSERT INTO dbo.ConsentType (Code, Name, Description, IsActive)
    VALUES (
        N'TeleRecording',
        N'Teleconsultation recording',
        N'Recording is allowed only when both sides accept. Decline still writes ConsentRecord + TeleConsentLog.',
        1
    );
END
ELSE
BEGIN
    UPDATE dbo.ConsentType
    SET IsActive = 1,
        Name = CASE WHEN NULLIF(LTRIM(RTRIM(Name)), N'') IS NULL THEN N'Teleconsultation recording' ELSE Name END,
        Description = COALESCE(
            NULLIF(LTRIM(RTRIM(Description)), N''),
            N'Recording is allowed only when both sides accept. Decline still writes ConsentRecord + TeleConsentLog.'
        )
    WHERE Code = N'TeleRecording';
END
GO

IF OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NULL
BEGIN
    RAISERROR('dbo.ConsentRecord is required (S1 SEC-06). Do not create a second tele consent table.', 16, 1);
    RETURN;
END
GO

PRINT 'M12_TEL_07_01.sql: TeleConsentLog + ConsentType TeleRecording ready (ConsentRecord dual-write).';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.TeleConsentLog')
ORDER BY c.column_id;

SELECT ConsentTypeId, Code, Name, IsActive
FROM dbo.ConsentType
WHERE Code = N'TeleRecording';

SELECT TOP 5
    TeleConsentLogId,
    TeleSessionId,
    PatientAppId,
    Accepted,
    ByRole,
    At
FROM dbo.TeleConsentLog
ORDER BY TeleConsentLogId DESC;

SELECT TOP 5
    cr.ConsentRecordId,
    ct.Code AS ConsentTypeCode,
    cr.SubjectType,
    cr.SubjectId,
    cr.GrantedAt,
    cr.WithdrawnAt,
    cr.Notes
FROM dbo.ConsentRecord cr
INNER JOIN dbo.ConsentType ct ON ct.ConsentTypeId = cr.ConsentTypeId
WHERE ct.Code = N'TeleRecording'
ORDER BY cr.ConsentRecordId DESC;
GO
