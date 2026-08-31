/*
    HomeoCentrum - WhatsApp Message Log enhancements
*/

IF COL_LENGTH('dbo.WhatsAppMessageLog', 'CampaignID') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD CampaignID INT NULL;
END
GO

IF COL_LENGTH('dbo.WhatsAppMessageLog', 'TemplateID') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD TemplateID INT NULL;
END
GO

IF COL_LENGTH('dbo.WhatsAppMessageLog', 'MessageCategory') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD MessageCategory NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('dbo.WhatsAppMessageLog', 'FinalMessage') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD FinalMessage NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_WhatsAppMessageLog_CampaignID'
      AND object_id = OBJECT_ID(N'dbo.WhatsAppMessageLog')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_WhatsAppMessageLog_CampaignID
        ON dbo.WhatsAppMessageLog (CampaignID);
END
GO
