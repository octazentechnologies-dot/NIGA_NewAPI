/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_06_01.sql
Purpose      : TEL-06.01 — dbo.TeleSessionEvent for waiting-room session status events.
               Columns: TeleSessionId, Code, Detail, At (+ TeleSessionEventId PK).
               Codes written by API: Created, Started, Ended, Token, RejoinToken,
               plus join-failure codes (TEL-09).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.TeleSession (TEL-03.01). Soft link by TeleSessionId.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.TeleSessionEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleSessionEvent
    (
        TeleSessionEventId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleSessionEvent PRIMARY KEY,
        TeleSessionId INT NOT NULL,
        Code NVARCHAR(40) NOT NULL,
        Detail NVARCHAR(500) NULL,
        At DATETIME NOT NULL CONSTRAINT DF_TeleSessionEvent_At DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.TeleSessionEvent', N'TeleSessionId') IS NULL
    ALTER TABLE dbo.TeleSessionEvent ADD TeleSessionId INT NOT NULL
        CONSTRAINT DF_TeleSessionEvent_TeleSessionId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.TeleSessionEvent', N'Code') IS NULL
    ALTER TABLE dbo.TeleSessionEvent ADD Code NVARCHAR(40) NOT NULL
        CONSTRAINT DF_TeleSessionEvent_Code_Add DEFAULT (N'Unknown');
IF COL_LENGTH(N'dbo.TeleSessionEvent', N'Detail') IS NULL
    ALTER TABLE dbo.TeleSessionEvent ADD Detail NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.TeleSessionEvent', N'At') IS NULL
    ALTER TABLE dbo.TeleSessionEvent ADD At DATETIME NOT NULL
        CONSTRAINT DF_TeleSessionEvent_At_Add DEFAULT (GETDATE());
GO

IF COL_LENGTH(N'dbo.TeleSessionEvent', N'At') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.TeleSessionEvent')
          AND c.name = N'At'
   )
    ALTER TABLE dbo.TeleSessionEvent
        ADD CONSTRAINT DF_TeleSessionEvent_At DEFAULT (GETDATE()) FOR At;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TeleSessionEvent')
      AND name = N'IX_TeleSessionEvent_SessionAt'
)
    CREATE INDEX IX_TeleSessionEvent_SessionAt
        ON dbo.TeleSessionEvent (TeleSessionId, At DESC);
GO

IF COL_LENGTH(N'dbo.TeleSessionEvent', N'TeleSessionId') IS NULL
   OR COL_LENGTH(N'dbo.TeleSessionEvent', N'Code') IS NULL
   OR COL_LENGTH(N'dbo.TeleSessionEvent', N'At') IS NULL
BEGIN
    RAISERROR('TEL-06.01 required columns TeleSessionId, Code, At are missing on dbo.TeleSessionEvent.', 16, 1);
    RETURN;
END
GO

PRINT 'M12_TEL_06_01.sql: TeleSessionEvent ready (session status events).';

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
WHERE c.object_id = OBJECT_ID(N'dbo.TeleSessionEvent')
ORDER BY c.column_id;

SELECT TOP 10
    TeleSessionEventId,
    TeleSessionId,
    Code,
    Detail,
    At
FROM dbo.TeleSessionEvent
ORDER BY TeleSessionEventId DESC;
GO
