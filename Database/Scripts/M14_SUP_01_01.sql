/*
================================================================================
Author       : Tufan Powar
Created      : 24-09-2026
Script       : M14_SUP_01_01.sql
Purpose      : SUP-01.01 — patient support ticket tables:
               dbo.SupportTicket, dbo.SupportTicketMessage, dbo.SupportTicketAttachment.
Use          : HomeoCentrum_Dev first, then UAT. Idempotent.
Prerequisites: App users exist (ReporterUserId / AuthorUserId soft-linked).
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

/* ---------- SupportTicket ---------- */
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
END
GO

IF COL_LENGTH(N'dbo.SupportTicket', N'ReporterUserId') IS NULL
    ALTER TABLE dbo.SupportTicket ADD ReporterUserId BIGINT NOT NULL
        CONSTRAINT DF_SupportTicket_ReporterUserId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.SupportTicket', N'ReporterRole') IS NULL
    ALTER TABLE dbo.SupportTicket ADD ReporterRole NVARCHAR(30) NOT NULL
        CONSTRAINT DF_SupportTicket_ReporterRole_Add DEFAULT (N'Patient');
IF COL_LENGTH(N'dbo.SupportTicket', N'Category') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Category NVARCHAR(40) NOT NULL
        CONSTRAINT DF_SupportTicket_Category_Add DEFAULT (N'other');
IF COL_LENGTH(N'dbo.SupportTicket', N'Subject') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Subject NVARCHAR(200) NOT NULL
        CONSTRAINT DF_SupportTicket_Subject_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.SupportTicket', N'Body') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Body NVARCHAR(4000) NOT NULL
        CONSTRAINT DF_SupportTicket_Body_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.SupportTicket', N'Status') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Status NVARCHAR(20) NOT NULL
        CONSTRAINT DF_SupportTicket_Status_Add DEFAULT (N'OPEN');
IF COL_LENGTH(N'dbo.SupportTicket', N'Priority') IS NULL
    ALTER TABLE dbo.SupportTicket ADD Priority NVARCHAR(20) NOT NULL
        CONSTRAINT DF_SupportTicket_Priority_Add DEFAULT (N'NORMAL');
IF COL_LENGTH(N'dbo.SupportTicket', N'AssigneeUserId') IS NULL
    ALTER TABLE dbo.SupportTicket ADD AssigneeUserId BIGINT NULL;
IF COL_LENGTH(N'dbo.SupportTicket', N'SlaDueAt') IS NULL
    ALTER TABLE dbo.SupportTicket ADD SlaDueAt DATETIME NULL;
IF COL_LENGTH(N'dbo.SupportTicket', N'CreatedAt') IS NULL
    ALTER TABLE dbo.SupportTicket ADD CreatedAt DATETIME NOT NULL
        CONSTRAINT DF_SupportTicket_CreatedAt_Add DEFAULT (GETDATE());
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportTicket')
      AND name = N'IX_SupportTicket_Reporter'
)
    CREATE INDEX IX_SupportTicket_Reporter ON dbo.SupportTicket (ReporterUserId, ReporterRole);
GO

/* ---------- SupportTicketMessage ---------- */
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

IF COL_LENGTH(N'dbo.SupportTicketMessage', N'SupportTicketId') IS NULL
    ALTER TABLE dbo.SupportTicketMessage ADD SupportTicketId INT NOT NULL
        CONSTRAINT DF_SupportTicketMessage_SupportTicketId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.SupportTicketMessage', N'AuthorUserId') IS NULL
    ALTER TABLE dbo.SupportTicketMessage ADD AuthorUserId BIGINT NOT NULL
        CONSTRAINT DF_SupportTicketMessage_AuthorUserId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.SupportTicketMessage', N'AuthorRole') IS NULL
    ALTER TABLE dbo.SupportTicketMessage ADD AuthorRole NVARCHAR(30) NOT NULL
        CONSTRAINT DF_SupportTicketMessage_AuthorRole_Add DEFAULT (N'Patient');
IF COL_LENGTH(N'dbo.SupportTicketMessage', N'Body') IS NULL
    ALTER TABLE dbo.SupportTicketMessage ADD Body NVARCHAR(4000) NOT NULL
        CONSTRAINT DF_SupportTicketMessage_Body_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.SupportTicketMessage', N'At') IS NULL
    ALTER TABLE dbo.SupportTicketMessage ADD At DATETIME NOT NULL
        CONSTRAINT DF_SupportTicketMessage_At_Add DEFAULT (GETDATE());
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportTicketMessage')
      AND name = N'IX_SupportTicketMessage_TicketAt'
)
    CREATE INDEX IX_SupportTicketMessage_TicketAt
        ON dbo.SupportTicketMessage (SupportTicketId, At);
GO

/* ---------- SupportTicketAttachment ---------- */
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

IF COL_LENGTH(N'dbo.SupportTicketAttachment', N'SupportTicketId') IS NULL
    ALTER TABLE dbo.SupportTicketAttachment ADD SupportTicketId INT NOT NULL
        CONSTRAINT DF_SupportTicketAttachment_SupportTicketId_Add DEFAULT (0);
IF COL_LENGTH(N'dbo.SupportTicketAttachment', N'SupportTicketMessageId') IS NULL
    ALTER TABLE dbo.SupportTicketAttachment ADD SupportTicketMessageId INT NULL;
IF COL_LENGTH(N'dbo.SupportTicketAttachment', N'FileName') IS NULL
    ALTER TABLE dbo.SupportTicketAttachment ADD FileName NVARCHAR(260) NOT NULL
        CONSTRAINT DF_SupportTicketAttachment_FileName_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.SupportTicketAttachment', N'StorageKey') IS NULL
    ALTER TABLE dbo.SupportTicketAttachment ADD StorageKey NVARCHAR(200) NOT NULL
        CONSTRAINT DF_SupportTicketAttachment_StorageKey_Add DEFAULT (N'');
IF COL_LENGTH(N'dbo.SupportTicketAttachment', N'At') IS NULL
    ALTER TABLE dbo.SupportTicketAttachment ADD At DATETIME NOT NULL
        CONSTRAINT DF_SupportTicketAttachment_At_Add DEFAULT (GETDATE());
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.SupportTicketAttachment')
      AND name = N'IX_SupportTicketAttachment_Ticket'
)
    CREATE INDEX IX_SupportTicketAttachment_Ticket
        ON dbo.SupportTicketAttachment (SupportTicketId);
GO

IF OBJECT_ID(N'dbo.SupportTicket', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SupportTicketMessage', N'U') IS NULL
   OR OBJECT_ID(N'dbo.SupportTicketAttachment', N'U') IS NULL
BEGIN
    RAISERROR('SUP-01.01 required tables SupportTicket / Message / Attachment are missing.', 16, 1);
    RETURN;
END
GO

PRINT 'M14_SUP_01_01.sql: SupportTicket + Message + Attachment ready.';

SELECT N'SupportTicket' AS TableName, COUNT(*) AS ColumnCount
FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SupportTicket')
UNION ALL
SELECT N'SupportTicketMessage', COUNT(*)
FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SupportTicketMessage')
UNION ALL
SELECT N'SupportTicketAttachment', COUNT(*)
FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.SupportTicketAttachment');

SELECT TOP 3 SupportTicketId, ReporterUserId, Category, Subject, Status, CreatedAt
FROM dbo.SupportTicket
ORDER BY SupportTicketId DESC;
GO
