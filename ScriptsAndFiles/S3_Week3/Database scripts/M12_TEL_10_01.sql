/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_10_01.sql
Purpose      : TEL-10.01 — dbo.TeleChatMessage for case-linked tele consult chat.
               Required columns: SessionId, PatientAppId, Body, At.
               Also: TeleChatMessageId (PK), SenderRole (doctor/patient author).
               Messages are tied to TeleSession (+ PatientAppId), not a loose thread.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.TeleSession (TEL-03.01). Soft link by SessionId = TeleSessionId.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.TeleChatMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleChatMessage
    (
        TeleChatMessageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleChatMessage PRIMARY KEY,
        SessionId INT NOT NULL,
        PatientAppId INT NOT NULL,
        SenderRole NVARCHAR(30) NOT NULL,
        Body NVARCHAR(2000) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_TeleChatMessage_At DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.TeleChatMessage', N'SessionId') IS NULL
    ALTER TABLE dbo.TeleChatMessage ADD SessionId INT NOT NULL
        CONSTRAINT DF_TeleChatMessage_SessionId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleChatMessage', N'PatientAppId') IS NULL
    ALTER TABLE dbo.TeleChatMessage ADD PatientAppId INT NOT NULL
        CONSTRAINT DF_TeleChatMessage_PatientAppId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleChatMessage', N'Body') IS NULL
    ALTER TABLE dbo.TeleChatMessage ADD Body NVARCHAR(2000) NOT NULL
        CONSTRAINT DF_TeleChatMessage_Body_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.TeleChatMessage', N'At') IS NULL
    ALTER TABLE dbo.TeleChatMessage ADD At DATETIME NOT NULL
        CONSTRAINT DF_TeleChatMessage_At_Add DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.TeleChatMessage', N'SenderRole') IS NULL
    ALTER TABLE dbo.TeleChatMessage ADD SenderRole NVARCHAR(30) NOT NULL
        CONSTRAINT DF_TeleChatMessage_SenderRole_Add DEFAULT (N'Doctor');
GO

IF COL_LENGTH(N'dbo.TeleChatMessage', N'At') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TeleChatMessage')
          AND c.name = N'At'
   )
    ALTER TABLE dbo.TeleChatMessage
        ADD CONSTRAINT DF_TeleChatMessage_At DEFAULT (GETDATE()) FOR At;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TeleChatMessage')
      AND name = N'IX_TeleChatMessage_SessionAt'
)
    CREATE INDEX IX_TeleChatMessage_SessionAt
        ON dbo.TeleChatMessage (SessionId, At);
GO

IF COL_LENGTH(N'dbo.TeleChatMessage', N'SessionId') IS NULL
   OR COL_LENGTH(N'dbo.TeleChatMessage', N'PatientAppId') IS NULL
   OR COL_LENGTH(N'dbo.TeleChatMessage', N'Body') IS NULL
   OR COL_LENGTH(N'dbo.TeleChatMessage', N'At') IS NULL
BEGIN
    RAISERROR('TEL-10.01 required columns SessionId, PatientAppId, Body, At are missing on dbo.TeleChatMessage.', 16, 1);
    RETURN;
END
GO

PRINT 'M12_TEL_10_01.sql: TeleChatMessage ready (SessionId, PatientAppId, Body, At).';

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
WHERE c.object_id = OBJECT_ID(N'dbo.TeleChatMessage')
ORDER BY c.column_id;

SELECT TOP 5
    TeleChatMessageId,
    SessionId,
    PatientAppId,
    SenderRole,
    LEFT(Body, 80) AS BodyPreview,
    At
FROM dbo.TeleChatMessage
ORDER BY TeleChatMessageId DESC;
GO
