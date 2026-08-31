/*
    HomeoCentrum - Add LanguageId to WhatsAppMessageLog for audit/history
*/

IF COL_LENGTH('dbo.WhatsAppMessageLog', 'LanguageId') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog
        ADD LanguageId INT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_WhatsAppMessageLog_LanguageMaster'
)
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog
        ADD CONSTRAINT FK_WhatsAppMessageLog_LanguageMaster
            FOREIGN KEY (LanguageId) REFERENCES dbo.LanguageMaster (languageId);
END
GO
