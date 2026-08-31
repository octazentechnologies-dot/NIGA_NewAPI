/*
    HomeoCentrum - WhatsApp Campaign
*/

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = N'WhatsAppCampaign'
      AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.WhatsAppCampaign
    (
        CampaignID INT IDENTITY(1, 1) NOT NULL,
        CampaignName NVARCHAR(200) NOT NULL,
        CampaignCategory NVARCHAR(50) NOT NULL,
        DoctorID INT NOT NULL,
        MessageBody NVARCHAR(MAX) NULL,
        ImageUrl NVARCHAR(1000) NULL,
        IsBulk BIT NOT NULL CONSTRAINT DF_WhatsAppCampaign_IsBulk DEFAULT (0),
        EnteredBy NVARCHAR(50) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_WhatsAppCampaign_EnteredDate DEFAULT (GETDATE()),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_WhatsAppCampaign_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_WhatsAppCampaign PRIMARY KEY CLUSTERED (CampaignID)
    );

    CREATE NONCLUSTERED INDEX IX_WhatsAppCampaign_DoctorID
        ON dbo.WhatsAppCampaign (DoctorID);

    CREATE NONCLUSTERED INDEX IX_WhatsAppCampaign_Category
        ON dbo.WhatsAppCampaign (CampaignCategory);
END
GO
