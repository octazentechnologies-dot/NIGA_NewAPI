/*
    HomeoCentrum - WhatsApp Template Master
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = N'WhatsAppTemplateMaster'
      AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.WhatsAppTemplateMaster
    (
        TemplateId INT IDENTITY(1, 1) NOT NULL,
        TemplateName NVARCHAR(200) NOT NULL,
        TemplateCategory NVARCHAR(50) NOT NULL,
        MetaTemplateName NVARCHAR(200) NULL,
        TemplateBody NVARCHAR(MAX) NOT NULL,
        LanguageId INT NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_WhatsAppTemplateMaster_IsActive DEFAULT (1),
        EnteredBy NVARCHAR(50) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_WhatsAppTemplateMaster_EnteredDate DEFAULT (GETDATE()),
        ChangedBy NVARCHAR(50) NULL,
        ChangedDate DATETIME NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_WhatsAppTemplateMaster_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_WhatsAppTemplateMaster PRIMARY KEY CLUSTERED (TemplateId),
        CONSTRAINT FK_WhatsAppTemplateMaster_LanguageMaster
            FOREIGN KEY (LanguageId) REFERENCES dbo.LanguageMaster (languageId)
    );

    CREATE NONCLUSTERED INDEX IX_WhatsAppTemplateMaster_Category
        ON dbo.WhatsAppTemplateMaster (TemplateCategory, IsActive)
        WHERE DeleteStatus = 0;

    CREATE UNIQUE NONCLUSTERED INDEX UX_WhatsAppTemplateMaster_Name_Category_Language
        ON dbo.WhatsAppTemplateMaster (TemplateName, TemplateCategory, LanguageId)
        WHERE DeleteStatus = 0;

    CREATE NONCLUSTERED INDEX IX_WhatsAppTemplateMaster_Category_Language
        ON dbo.WhatsAppTemplateMaster (TemplateCategory, LanguageId, IsActive)
        WHERE DeleteStatus = 0;
END
GO
