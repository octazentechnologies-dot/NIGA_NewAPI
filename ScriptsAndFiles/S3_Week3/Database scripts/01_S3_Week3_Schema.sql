/*
================================================================================
Author       : Tufan Powar
Created      : 22-09-2026
Script       : 01_S3_Week3_Schema.sql
Purpose      : S3 Week 3 schema. Does not rewrite existing VisitType / ConsultMode.
               Does not touch Razorpay tables. No SMS / WhatsApp config.
Use          : HomeoCentrum_Dev. Idempotent. Run before the New-API restart.
================================================================================
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.PatientAppointment', N'U') IS NULL
BEGIN
    RAISERROR('dbo.PatientAppointment is missing.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.PatientAppointment', N'CancelReasonCode') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CancelReasonCode NVARCHAR(40) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'CancelReasonText') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CancelReasonText NVARCHAR(500) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'CancelledBy') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CancelledBy BIGINT NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'CancelledAt') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CancelledAt DATETIME NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'PaymentMethod') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PaymentMethod NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'PayAtClinicAllowed') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD PayAtClinicAllowed BIT NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'QueuePosition') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD QueuePosition INT NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'BookingChannel') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD BookingChannel NVARCHAR(30) NULL;
IF COL_LENGTH(N'dbo.PatientAppointment', N'CalledAt') IS NULL
    ALTER TABLE dbo.PatientAppointment ADD CalledAt DATETIME NULL;
GO

IF COL_LENGTH(N'dbo.DoctorDailySchedule', N'BreakStartTime') IS NULL
    ALTER TABLE dbo.DoctorDailySchedule ADD BreakStartTime TIME(0) NULL;
IF COL_LENGTH(N'dbo.DoctorDailySchedule', N'BreakEndTime') IS NULL
    ALTER TABLE dbo.DoctorDailySchedule ADD BreakEndTime TIME(0) NULL;
GO

IF COL_LENGTH(N'dbo.DoctorReceptionStaff', N'IsActive') IS NULL
    ALTER TABLE dbo.DoctorReceptionStaff ADD IsActive BIT NOT NULL CONSTRAINT DF_DoctorReceptionStaff_IsActive_S3 DEFAULT (1);
GO

IF COL_LENGTH(N'dbo.CaseEntryChiefComplaint', N'CreatedByRole') IS NULL
    ALTER TABLE dbo.CaseEntryChiefComplaint ADD CreatedByRole NVARCHAR(30) NULL;
GO

IF OBJECT_ID(N'dbo.AppointmentChangeLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppointmentChangeLog
    (
        AppointmentChangeLogId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppointmentChangeLog PRIMARY KEY,
        PatientAppId INT NOT NULL,
        Action NVARCHAR(30) NOT NULL,
        OldValue NVARCHAR(400) NULL,
        NewValue NVARCHAR(400) NULL,
        ByUserId BIGINT NOT NULL,
        ByRole NVARCHAR(50) NULL,
        Reason NVARCHAR(500) NULL,
        At DATETIME NOT NULL CONSTRAINT DF_AppointmentChangeLog_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_AppointmentChangeLog_PatientAppId ON dbo.AppointmentChangeLog (PatientAppId, At DESC);
END
GO

IF OBJECT_ID(N'dbo.BookingWaitlist', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BookingWaitlist
    (
        BookingWaitlistId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingWaitlist PRIMARY KEY,
        DoctorId INT NOT NULL,
        PatientId INT NULL,
        RequestedDate DATE NOT NULL,
        ConsultMode NVARCHAR(30) NOT NULL,
        ContactName NVARCHAR(200) NOT NULL,
        ContactMobile NVARCHAR(30) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_BookingWaitlist_Status DEFAULT (N'JOINED'),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_BookingWaitlist_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_BookingWaitlist_DoctorDate ON dbo.BookingWaitlist (DoctorId, RequestedDate, Status);
END
GO

IF OBJECT_ID(N'dbo.ReceptionCasePaper', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ReceptionCasePaper
    (
        ReceptionCasePaperId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReceptionCasePaper PRIMARY KEY,
        PatientAppId INT NULL,
        PatientId INT NOT NULL,
        DoctorId INT NOT NULL,
        CaseId INT NULL,
        ChiefComplaint NVARCHAR(1000) NOT NULL,
        CreatedByRole NVARCHAR(30) NOT NULL,
        CreatedByUserId BIGINT NOT NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_ReceptionCasePaper_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_ReceptionCasePaper_DoctorPatient ON dbo.ReceptionCasePaper (DoctorId, PatientId, CreatedAt DESC);
END
GO

IF OBJECT_ID(N'dbo.TeleAvailability', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleAvailability
    (
        DoctorId INT NOT NULL CONSTRAINT PK_TeleAvailability PRIMARY KEY,
        IsOnline BIT NOT NULL CONSTRAINT DF_TeleAvailability_IsOnline DEFAULT (0),
        LastHeartbeat DATETIME NULL
    );
END
GO

IF OBJECT_ID(N'dbo.TeleSession', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleSession
    (
        TeleSessionId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleSession PRIMARY KEY,
        PatientAppId INT NOT NULL,
        DoctorId INT NOT NULL,
        PatientId INT NOT NULL,
        RoomId NVARCHAR(64) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        RecordAllowed BIT NOT NULL CONSTRAINT DF_TeleSession_RecordAllowed DEFAULT (0),
        StartedAt DATETIME NULL,
        EndedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_TeleSession_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_TeleSession_PatientApp ON dbo.TeleSession (PatientAppId, Status);
END
GO

IF OBJECT_ID(N'dbo.TeleSessionEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleSessionEvent
    (
        TeleSessionEventId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleSessionEvent PRIMARY KEY,
        TeleSessionId INT NOT NULL,
        Code NVARCHAR(40) NOT NULL,
        Detail NVARCHAR(500) NULL,
        At DATETIME NOT NULL CONSTRAINT DF_TeleSessionEvent_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.TeleConsentLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleConsentLog
    (
        TeleConsentLogId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleConsentLog PRIMARY KEY,
        TeleSessionId INT NOT NULL,
        PatientAppId INT NOT NULL,
        Accepted BIT NOT NULL,
        ByRole NVARCHAR(30) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_TeleConsentLog_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.TeleChatMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TeleChatMessage
    (
        TeleChatMessageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TeleChatMessage PRIMARY KEY,
        SessionId INT NOT NULL,
        PatientAppId INT NOT NULL,
        SenderRole NVARCHAR(30) NOT NULL,
        Body NVARCHAR(2000) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_TeleChatMessage_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.ConsultationSummary', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsultationSummary
    (
        ConsultationSummaryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ConsultationSummary PRIMARY KEY,
        PatientAppId INT NOT NULL,
        DoctorId INT NOT NULL,
        Text NVARCHAR(4000) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_ConsultationSummary_At DEFAULT (GETDATE())
    );
    CREATE INDEX IX_ConsultationSummary_PatientApp ON dbo.ConsultationSummary (PatientAppId, At DESC);
END
GO

IF OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InstantConsultRequest
    (
        InstantConsultRequestId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InstantConsultRequest PRIMARY KEY,
        PatientId INT NULL,
        ContactName NVARCHAR(200) NOT NULL,
        ContactMobile NVARCHAR(30) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_InstantConsultRequest_Status DEFAULT (N'OPEN'),
        QueuePosition INT NOT NULL CONSTRAINT DF_InstantConsultRequest_Queue DEFAULT (1),
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_InstantConsultRequest_CreatedAt DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.DoctorOffer', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorOffer
    (
        DoctorOfferId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DoctorOffer PRIMARY KEY,
        InstantConsultRequestId INT NOT NULL,
        DoctorId INT NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_DoctorOffer_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.SupportTicket', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTicket
    (
        SupportTicketId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupportTicket PRIMARY KEY,
        ReporterUserId BIGINT NOT NULL,
        ReporterRole NVARCHAR(30) NOT NULL,
        Category NVARCHAR(40) NOT NULL,
        Subject NVARCHAR(200) NOT NULL,
        Body NVARCHAR(4000) NOT NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_SupportTicket_Status DEFAULT (N'OPEN'),
        Priority NVARCHAR(20) NOT NULL CONSTRAINT DF_SupportTicket_Priority DEFAULT (N'NORMAL'),
        AssigneeUserId BIGINT NULL,
        SlaDueAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_SupportTicket_CreatedAt DEFAULT (GETDATE())
    );
    CREATE INDEX IX_SupportTicket_Reporter ON dbo.SupportTicket (ReporterUserId, ReporterRole);
END
GO

IF OBJECT_ID(N'dbo.SupportTicketMessage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTicketMessage
    (
        SupportTicketMessageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupportTicketMessage PRIMARY KEY,
        SupportTicketId INT NOT NULL,
        AuthorUserId BIGINT NOT NULL,
        AuthorRole NVARCHAR(30) NOT NULL,
        Body NVARCHAR(4000) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_SupportTicketMessage_At DEFAULT (GETDATE())
    );
END
GO

IF OBJECT_ID(N'dbo.SupportTicketAttachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SupportTicketAttachment
    (
        SupportTicketAttachmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SupportTicketAttachment PRIMARY KEY,
        SupportTicketId INT NOT NULL,
        SupportTicketMessageId INT NULL,
        FileName NVARCHAR(260) NOT NULL,
        StorageKey NVARCHAR(200) NOT NULL,
        At DATETIME NOT NULL CONSTRAINT DF_SupportTicketAttachment_At DEFAULT (GETDATE())
    );
END
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
    CREATE UNIQUE INDEX UX_HelpArticle_Slug ON dbo.HelpArticle (Slug);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N'booking-a-visit')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (N'Booking a visit', N'booking-a-visit', N'Choose a doctor, an open slot, and in-clinic or tele. A full day can take a waitlist join. Joining does not reserve the slot.', 1);
IF NOT EXISTS (SELECT 1 FROM dbo.HelpArticle WHERE Slug = N'draft-hidden-article')
    INSERT INTO dbo.HelpArticle (Title, Slug, Body, IsPublished)
    VALUES (N'Draft hidden article', N'draft-hidden-article', N'This article stays hidden until it is published.', 0);
GO

IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.ConsentType WHERE Code = N'TeleRecording')
    INSERT INTO dbo.ConsentType (Code, Name, Description, IsActive)
    VALUES (N'TeleRecording', N'Teleconsultation recording', N'Recording is allowed only when both sides accept.', 1);
GO

PRINT 'S3 Week 3 schema ready. Existing VisitType values were not rewritten.';
GO
