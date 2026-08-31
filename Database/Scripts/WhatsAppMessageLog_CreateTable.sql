/*
    HomeoCentrum - WhatsApp Message Log table
    Database: HomeoCentrum_Production
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = N'WhatsAppMessageLog'
      AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.WhatsAppMessageLog
    (
        WhatsAppMessageLogID INT IDENTITY(1, 1) NOT NULL,
        DoctorID INT NULL,
        PatientID INT NULL,
        CampaignID INT NULL,
        TemplateID INT NULL,
        PatientName NVARCHAR(250) NULL,
        MobileNumber NVARCHAR(50) NULL,
        MessageCategory NVARCHAR(50) NULL,
        MessageBody NVARCHAR(MAX) NULL,
        TemplateMessage NVARCHAR(MAX) NULL,
        FinalMessage NVARCHAR(MAX) NULL,
        MetaMessageId NVARCHAR(500) NULL,
        MediaId NVARCHAR(500) NULL,
        IsBulk BIT NOT NULL CONSTRAINT DF_WhatsAppMessageLog_IsBulk DEFAULT (0),
        SendStatus BIT NOT NULL CONSTRAINT DF_WhatsAppMessageLog_SendStatus DEFAULT (0),
        ErrorMessage NVARCHAR(MAX) NULL,
        CreatedDate DATETIME NOT NULL CONSTRAINT DF_WhatsAppMessageLog_CreatedDate DEFAULT (GETDATE()),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_WhatsAppMessageLog_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_WhatsAppMessageLog PRIMARY KEY CLUSTERED (WhatsAppMessageLogID)
    );

    CREATE NONCLUSTERED INDEX IX_WhatsAppMessageLog_DoctorID
        ON dbo.WhatsAppMessageLog (DoctorID);

    CREATE NONCLUSTERED INDEX IX_WhatsAppMessageLog_PatientID
        ON dbo.WhatsAppMessageLog (PatientID);

    CREATE NONCLUSTERED INDEX IX_WhatsAppMessageLog_CampaignID
        ON dbo.WhatsAppMessageLog (CampaignID);

    CREATE NONCLUSTERED INDEX IX_WhatsAppMessageLog_MobileNumber
        ON dbo.WhatsAppMessageLog (MobileNumber);

    CREATE NONCLUSTERED INDEX IX_WhatsAppMessageLog_CreatedDate
        ON dbo.WhatsAppMessageLog (CreatedDate);
END
GO
