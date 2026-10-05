/*
================================================================================
Author       : Tufan Powar
Created      : 01-10-2026
Script       : 01_S5_Week5_Schema.sql
Purpose      : S5 Week 5 tables for SMS, in-app notifications, and email logs.
               WhatsApp delivery columns are added on the existing log.
               Report queries reuse Week 4 payment, follow-up, and medicine tables.
               Does not store SMS, FCM, or Razorpay secrets.
Use          : HomeoCentrum_Dev. Idempotent. Run after S4_Week4 (01 schema, 02 outbox, 03 menus, 04 demo).
               Then this folder in filename order:
               1. 01_S5_Week5_Schema.sql
               2. 02_S5_Week5_Menus.sql
               3. 03_S5_Week5_Demo_Data.sql
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH(N'dbo.Patient', N'SmsOptOut') IS NULL
BEGIN
    ALTER TABLE dbo.Patient ADD SmsOptOut BIT NOT NULL
        CONSTRAINT DF_Patient_SmsOptOut DEFAULT (0);
END
GO

IF OBJECT_ID(N'dbo.SmsTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmsTemplate
    (
        SmsTemplateId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SmsTemplate PRIMARY KEY,
        Code NVARCHAR(40) NOT NULL,
        Body NVARCHAR(500) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SmsTemplate_IsActive DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SmsTemplate_CreatedAt DEFAULT (GETDATE()),
        CONSTRAINT UX_SmsTemplate_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.SmsMessageLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmsMessageLog
    (
        SmsMessageLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SmsMessageLog PRIMARY KEY,
        TemplateCode NVARCHAR(40) NULL,
        Mobile NVARCHAR(20) NOT NULL,
        Body NVARCHAR(500) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        ProviderMessageId NVARCHAR(80) NULL,
        PatientId INT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_SmsMessageLog_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_SmsMessageLog_At ON dbo.SmsMessageLog (At DESC);
END
GO

IF OBJECT_ID(N'dbo.AppNotification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppNotification
    (
        AppNotificationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppNotification PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Title NVARCHAR(160) NOT NULL,
        Body NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_AppNotification_IsRead DEFAULT (0),
        PushStatus NVARCHAR(30) NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_AppNotification_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_AppNotification_User ON dbo.AppNotification (UserId, CreatedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.EmailMessageLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmailMessageLog
    (
        EmailMessageLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_EmailMessageLog PRIMARY KEY,
        ToAddress NVARCHAR(200) NOT NULL,
        Subject NVARCHAR(200) NOT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        PaymentOrderId BIGINT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_EmailMessageLog_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_EmailMessageLog_At ON dbo.EmailMessageLog (At DESC);
END
GO

IF COL_LENGTH(N'dbo.WhatsAppMessageLog', N'DeliveryStatus') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD DeliveryStatus NVARCHAR(30) NULL;
END
GO

IF COL_LENGTH(N'dbo.WhatsAppMessageLog', N'ReceiptAt') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD ReceiptAt DATETIME NULL;
END
GO

IF COL_LENGTH(N'dbo.WhatsAppMessageLog', N'ReceiptPayload') IS NULL
BEGIN
    ALTER TABLE dbo.WhatsAppMessageLog ADD ReceiptPayload NVARCHAR(MAX) NULL;
END
GO

IF OBJECT_ID(N'dbo.DeviceToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceToken
    (
        DeviceTokenId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeviceToken PRIMARY KEY,
        UserId BIGINT NOT NULL,
        Token NVARCHAR(512) NOT NULL,
        Platform NVARCHAR(20) NOT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_DeviceToken_UpdatedAt DEFAULT (GETDATE()),
        CONSTRAINT UX_DeviceToken_User_Token UNIQUE (UserId, Token)
    );
END
GO

IF COL_LENGTH(N'dbo.DeviceToken', N'Token') IS NOT NULL
BEGIN
    ALTER TABLE dbo.DeviceToken ALTER COLUMN Token NVARCHAR(512) NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.DoctorSmsPreference', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorSmsPreference
    (
        DoctorSmsPreferenceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorSmsPreference PRIMARY KEY,
        DoctorId INT NOT NULL,
        TemplateCode NVARCHAR(40) NOT NULL,
        Enabled BIT NOT NULL CONSTRAINT DF_DoctorSmsPreference_Enabled DEFAULT (1),
        CONSTRAINT UX_DoctorSmsPreference_Doctor_Code UNIQUE (DoctorId, TemplateCode)
    );
END
GO
