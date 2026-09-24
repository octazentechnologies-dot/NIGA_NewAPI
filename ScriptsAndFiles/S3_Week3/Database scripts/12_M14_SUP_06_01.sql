/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M14_SUP_06_01.sql
Purpose      : SUP-06.01 — dbo.HelpArticle for patient help centre (self-service).
               Dedicated help table — do NOT reuse BlogDetails / blog category=help.
               Columns: Title, Slug, Body, IsPublished, CreatedAt (+ PK).
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.HelpArticle', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HelpArticle
    (
        HelpArticleId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HelpArticle PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Slug NVARCHAR(200) NOT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        IsPublished BIT NOT NULL CONSTRAINT DF_HelpArticle_IsPublished DEFAULT (0),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_HelpArticle_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF COL_LENGTH(N'dbo.HelpArticle', N'Title') IS NULL
    ALTER TABLE dbo.HelpArticle ADD Title NVARCHAR(200) NOT NULL
        CONSTRAINT DF_HelpArticle_Title_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.HelpArticle', N'Slug') IS NULL
    ALTER TABLE dbo.HelpArticle ADD Slug NVARCHAR(200) NOT NULL
        CONSTRAINT DF_HelpArticle_Slug_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.HelpArticle', N'Body') IS NULL
    ALTER TABLE dbo.HelpArticle ADD Body NVARCHAR(MAX) NOT NULL
        CONSTRAINT DF_HelpArticle_Body_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.HelpArticle', N'IsPublished') IS NULL
    ALTER TABLE dbo.HelpArticle ADD IsPublished BIT NOT NULL
        CONSTRAINT DF_HelpArticle_IsPublished_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.HelpArticle', N'CreatedAt') IS NULL
    ALTER TABLE dbo.HelpArticle ADD CreatedAt DATETIME NOT NULL
        CONSTRAINT DF_HelpArticle_CreatedAt_Add DEFAULT (GETDATE());
GO

IF COL_LENGTH(N'dbo.HelpArticle', N'IsPublished') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.HelpArticle')
          AND c.name = N'IsPublished'
   )
    ALTER TABLE dbo.HelpArticle
        ADD CONSTRAINT DF_HelpArticle_IsPublished DEFAULT (0) FOR IsPublished;
GO

IF COL_LENGTH(N'dbo.HelpArticle', N'CreatedAt') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1
        FROM sys.default_constraints dc
        INNER JOIN sys.columns c
            ON c.default_object_id = dc.object_id
           AND c.object_id = dc.parent_object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.HelpArticle')
          AND c.name = N'CreatedAt'
   )
    ALTER TABLE dbo.HelpArticle
        ADD CONSTRAINT DF_HelpArticle_CreatedAt DEFAULT (GETDATE()) FOR CreatedAt;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.HelpArticle')
      AND name = N'UX_HelpArticle_Slug'
)
    CREATE UNIQUE INDEX UX_HelpArticle_Slug ON dbo.HelpArticle (Slug);
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.HelpArticle')
      AND name = N'IX_HelpArticle_Published'
)
    CREATE INDEX IX_HelpArticle_Published ON dbo.HelpArticle (IsPublished, Slug);
GO

-- Seed one published + one draft (SUP-06.04 unpublished hidden) if empty of these slugs
IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N'booking-a-visit')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (
        N'Booking a visit',
        N'booking-a-visit',
        N'Choose a doctor, an open slot, and in-clinic or tele. A full day can take a waitlist join. Joining does not reserve the slot.',
        1
    );

IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N'draft-hidden-article')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (
        N'Draft hidden article',
        N'draft-hidden-article',
        N'This article stays hidden until it is published.',
        0
    );
GO

IF OBJECT_ID(N'dbo.HelpArticle', N'U') IS NULL
   OR COL_LENGTH(N'dbo.HelpArticle', N'Slug') IS NULL
   OR COL_LENGTH(N'dbo.HelpArticle', N'IsPublished') IS NULL
BEGIN
    RAISERROR('SUP-06.01 HelpArticle (Slug, IsPublished) is missing.', 16, 1);
    RETURN;
END
GO

PRINT 'M14_SUP_06_01.sql: HelpArticle ready (dedicated help centre table; not blog).';

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
WHERE c.object_id = OBJECT_ID(N'dbo.HelpArticle')
ORDER BY c.column_id;

SELECT HelpArticleId, Title, Slug, IsPublished
FROM dbo.HelpArticle
ORDER BY HelpArticleId;
GO
