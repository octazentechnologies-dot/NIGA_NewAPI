/*
S4 Week 4 — PAY-04.04 / APT notify outbox.
SMS and WhatsApp rows wait here until Msg91 / WhatsAppMeta keys are filled in New-API appsettings.
Idempotent.
*/
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.NotificationOutbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NotificationOutbox
    (
        NotificationOutboxId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NotificationOutbox PRIMARY KEY,
        Channel NVARCHAR(20) NOT NULL,
        EventType NVARCHAR(40) NOT NULL,
        Destination NVARCHAR(40) NULL,
        Body NVARCHAR(1000) NULL,
        Status NVARCHAR(20) NOT NULL,
        LastError NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_NotificationOutbox_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_NotificationOutbox_Status ON dbo.NotificationOutbox (Status, CreatedAt);
END
PRINT '03_S4_Notification_Outbox.sql completed.';
GO
