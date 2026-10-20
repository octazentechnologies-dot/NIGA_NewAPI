/*
Purpose: CareLink*, AppNotification Type/DeepLink, InstantConsult PatientAppId/TeleSessionId, FollowUpTask.CompletedAt.
Source: S6_Week6 scripts 03–04. Idempotent.
Risk: Low (additive).
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CareLinkRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CareLinkRequest (
        CareLinkRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FromDoctorId int NOT NULL,
        ToDoctorId int NOT NULL,
        PatientId int NOT NULL,
        Note nvarchar(500) NULL,
        FeeAmount decimal(18,2) NULL,
        Status nvarchar(20) NOT NULL,
        CreatedAt datetime NOT NULL
    );
    PRINT 'Created dbo.CareLinkRequest';
END
GO

IF COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL
BEGIN
    ALTER TABLE dbo.CareLinkRequest ADD Opinion nvarchar(1000) NULL;
    PRINT 'Added CareLinkRequest.Opinion';
END
GO

IF OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CareLinkInfoRequest (
        CareLinkInfoRequestId int IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CareLinkRequestId int NOT NULL,
        FromDoctorId int NOT NULL,
        OptionsJson nvarchar(1000) NULL,
        Note nvarchar(1000) NULL,
        CreatedAt datetime NOT NULL
    );
    PRINT 'Created dbo.CareLinkInfoRequest';
END
GO

IF OBJECT_ID(N'dbo.AppNotification', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL
    BEGIN
        ALTER TABLE dbo.AppNotification ADD Type nvarchar(40) NULL;
        PRINT 'Added AppNotification.Type';
    END
    IF COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL
    BEGIN
        ALTER TABLE dbo.AppNotification ADD DeepLink nvarchar(300) NULL;
        PRINT 'Added AppNotification.DeepLink';
    END
END
GO

IF OBJECT_ID(N'dbo.InstantConsultRequest', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL
        ALTER TABLE dbo.InstantConsultRequest ADD PatientAppId INT NULL;
    IF COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NULL
        ALTER TABLE dbo.InstantConsultRequest ADD TeleSessionId INT NULL;
END
GO

IF OBJECT_ID(N'dbo.FollowUpTask', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FollowUpTask', N'CompletedAt') IS NULL
    ALTER TABLE dbo.FollowUpTask ADD CompletedAt datetime NULL;
GO

PRINT '002_S6_CareLink_AppNotification_InstantConsult completed.';
GO
