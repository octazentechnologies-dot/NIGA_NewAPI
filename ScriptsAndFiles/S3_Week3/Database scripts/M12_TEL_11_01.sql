/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M12_TEL_11_01.sql
Purpose      : TEL-11.01 — dbo.ConsultationSummary for post-call patient summary.
               Required columns: PatientAppId, Text, At.
               Also: ConsultationSummaryId (PK), DoctorId (owning doctor; TEL-11.02).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: dbo.PatientAppointment exists (soft link by PatientAppId).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.ConsultationSummary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationSummary
    (
        ConsultationSummaryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsultationSummary PRIMARY KEY,
        PatientAppId INT NOT NULL,
        DoctorId INT NOT NULL CONSTRAINT DF_ConsultationSummary_DoctorId DEFAULT (0),
        Text NVARCHAR(4000) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_ConsultationSummary_At DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.ConsultationSummary', N'PatientAppId') IS NULL
    ALTER TABLE dbo.ConsultationSummary ADD PatientAppId INT NOT NULL
        CONSTRAINT DF_ConsultationSummary_PatientAppId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.ConsultationSummary', N'Text') IS NULL
    ALTER TABLE dbo.ConsultationSummary ADD Text NVARCHAR(4000) NOT NULL
        CONSTRAINT DF_ConsultationSummary_Text_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.ConsultationSummary', N'At') IS NULL
    ALTER TABLE dbo.ConsultationSummary ADD At DATETIME NOT NULL
        CONSTRAINT DF_ConsultationSummary_At_Add DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.ConsultationSummary', N'DoctorId') IS NULL
    ALTER TABLE dbo.ConsultationSummary ADD DoctorId INT NOT NULL
        CONSTRAINT DF_ConsultationSummary_DoctorId_Add DEFAULT (0);
GO

-- Default on At when table pre-existed without it.
IF COL_LENGTH(N'dbo.ConsultationSummary', N'At') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.ConsultationSummary')
          AND c.name = N'At'
   )
    ALTER TABLE dbo.ConsultationSummary
        ADD CONSTRAINT DF_ConsultationSummary_At DEFAULT (GETDATE()) FOR At;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ConsultationSummary')
      AND name = N'IX_ConsultationSummary_PatientApp'
)
    CREATE INDEX IX_ConsultationSummary_PatientApp
        ON dbo.ConsultationSummary (PatientAppId, At DESC);
GO

PRINT 'M12_TEL_11_01.sql: ConsultationSummary ready (PatientAppId, Text, At).';

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.is_nullable AS IsNullable,
    c.max_length AS MaxLength
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.ConsultationSummary')
ORDER BY c.column_id;

SELECT
    CASE WHEN COL_LENGTH(N'dbo.ConsultationSummary', N'PatientAppId') IS NULL THEN N'missing' ELSE N'present' END AS PatientAppIdColumn,
    CASE WHEN COL_LENGTH(N'dbo.ConsultationSummary', N'Text') IS NULL THEN N'missing' ELSE N'present' END AS TextColumn,
    CASE WHEN COL_LENGTH(N'dbo.ConsultationSummary', N'At') IS NULL THEN N'missing' ELSE N'present' END AS AtColumn,
    CASE WHEN COL_LENGTH(N'dbo.ConsultationSummary', N'DoctorId') IS NULL THEN N'missing' ELSE N'present' END AS DoctorIdColumn;

SELECT TOP 5
    ConsultationSummaryId,
    PatientAppId,
    DoctorId,
    LEFT(Text, 80) AS TextPreview,
    At
FROM dbo.ConsultationSummary
ORDER BY ConsultationSummaryId DESC;
GO
