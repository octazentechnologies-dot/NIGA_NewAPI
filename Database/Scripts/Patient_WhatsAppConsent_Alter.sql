/*
    HomeoCentrum - Patient WhatsApp consent columns
*/

IF COL_LENGTH('dbo.Patient', 'IsWhatsAppOptIn') IS NULL
BEGIN
    ALTER TABLE dbo.Patient
    ADD IsWhatsAppOptIn BIT NOT NULL
        CONSTRAINT DF_Patient_IsWhatsAppOptIn DEFAULT (0);
END
GO

IF COL_LENGTH('dbo.Patient', 'WhatsAppOptInDate') IS NULL
BEGIN
    ALTER TABLE dbo.Patient
    ADD WhatsAppOptInDate DATETIME NULL;
END
GO
