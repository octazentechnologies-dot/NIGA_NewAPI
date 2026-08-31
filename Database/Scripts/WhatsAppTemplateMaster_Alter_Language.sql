/*
    HomeoCentrum - Add LanguageId to WhatsAppTemplateMaster
    Run after WhatsAppTemplateMaster_CreateTable.sql
*/

IF COL_LENGTH('dbo.WhatsAppTemplateMaster', 'LanguageId') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppTemplateMaster
        ADD LanguageId INT NULL;
END
GO

-- Assign English to existing rows (lookup by languageName; fallback to languageId = 1)
DECLARE @EnglishLanguageId INT =
(
    SELECT TOP 1 languageId
    FROM dbo.LanguageMaster
    WHERE (IsDeleted = 0 OR IsDeleted IS NULL)
      AND UPPER(LTRIM(RTRIM(languageName))) IN (N'ENGLISH', N'EN')
    ORDER BY languageId
);

IF @EnglishLanguageId IS NULL
    SET @EnglishLanguageId = 1;

UPDATE dbo.WhatsAppTemplateMaster
SET LanguageId = @EnglishLanguageId
WHERE LanguageId IS NULL;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_WhatsAppTemplateMaster_LanguageMaster'
)
BEGIN
    ALTER TABLE dbo.WhatsAppTemplateMaster
        ADD CONSTRAINT FK_WhatsAppTemplateMaster_LanguageMaster
            FOREIGN KEY (LanguageId) REFERENCES dbo.LanguageMaster (languageId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_WhatsAppTemplateMaster_Name_Category_Language'
      AND object_id = OBJECT_ID(N'dbo.WhatsAppTemplateMaster')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_WhatsAppTemplateMaster_Name_Category_Language
        ON dbo.WhatsAppTemplateMaster (TemplateName, TemplateCategory, LanguageId)
        WHERE DeleteStatus = 0;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_WhatsAppTemplateMaster_Category_Language'
      AND object_id = OBJECT_ID(N'dbo.WhatsAppTemplateMaster')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_WhatsAppTemplateMaster_Category_Language
        ON dbo.WhatsAppTemplateMaster (TemplateCategory, LanguageId, IsActive)
        WHERE DeleteStatus = 0;
END
GO
