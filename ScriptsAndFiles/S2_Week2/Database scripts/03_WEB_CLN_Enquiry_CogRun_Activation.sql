/*
================================================================================
Author       : Tufan Powar
Created      : 18-09-2026
Script       : 03_WEB_CLN_Enquiry_CogRun_Activation.sql
Purpose      : WEB-06.01 EnquiryDetails TicketStatus/AssignedTo.
               CLN-13.01 optional CogRun log.
               WEB-10.01 UserMaster activation token + TTL.
Use          : Run after 02.
Prerequisites: dbo.EnquiryDetails, dbo.UserMaster.
Idempotent   : Yes.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- WEB-06.01
IF OBJECT_ID(N'dbo.EnquiryDetails', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.EnquiryDetails', N'TicketStatus') IS NULL
        ALTER TABLE dbo.EnquiryDetails ADD TicketStatus NVARCHAR(30) NULL;
    IF COL_LENGTH(N'dbo.EnquiryDetails', N'AssignedTo') IS NULL
        ALTER TABLE dbo.EnquiryDetails ADD AssignedTo BIGINT NULL;
END
GO

IF OBJECT_ID(N'dbo.EnquiryDetails', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.EnquiryDetails', N'TicketStatus') IS NOT NULL
BEGIN
    UPDATE dbo.EnquiryDetails
    SET TicketStatus = CASE WHEN ISNULL(EnquiryStatus, 1) = 1 THEN N'New' ELSE N'Closed' END
    WHERE TicketStatus IS NULL;
END
GO

-- CLN-13.01 optional support log (algorithm does not require this table)
IF OBJECT_ID(N'dbo.CogRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CogRun
    (
        CogRunId BIGINT IDENTITY(1,1) NOT NULL,
        DoctorId INT NOT NULL,
        PatientId INT NULL,
        InputJson NVARCHAR(MAX) NOT NULL,
        OutputJson NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_CogRun_CreatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_CogRun PRIMARY KEY CLUSTERED (CogRunId)
    );
END
GO

IF OBJECT_ID(N'dbo.CogRun', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CogRun_DoctorId' AND object_id = OBJECT_ID(N'dbo.CogRun'))
    CREATE INDEX IX_CogRun_DoctorId ON dbo.CogRun (DoctorId, CreatedAt DESC);
GO

-- WEB-10.01 activation token TTL
IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.UserMaster', N'ActivationTokenHash') IS NULL
        ALTER TABLE dbo.UserMaster ADD ActivationTokenHash NVARCHAR(128) NULL;
    IF COL_LENGTH(N'dbo.UserMaster', N'ActivationExpiresAt') IS NULL
        ALTER TABLE dbo.UserMaster ADD ActivationExpiresAt DATETIME NULL;
END
GO

IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.UserMaster', N'ActivationTokenHash') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserMaster_ActivationTokenHash_S2' AND object_id = OBJECT_ID(N'dbo.UserMaster'))
    CREATE INDEX IX_UserMaster_ActivationTokenHash_S2 ON dbo.UserMaster (ActivationTokenHash)
        WHERE ActivationTokenHash IS NOT NULL;
GO

PRINT '03_WEB_CLN_Enquiry_CogRun_Activation.sql complete';
GO
