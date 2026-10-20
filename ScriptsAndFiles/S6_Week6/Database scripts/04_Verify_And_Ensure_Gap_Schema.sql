-- Idempotent ensure + verify for DoctorApp 16102026 gaps on HomeoCentrum_Dev
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
END

IF COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL
    ALTER TABLE dbo.CareLinkRequest ADD Opinion nvarchar(1000) NULL;

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
END

IF OBJECT_ID(N'dbo.AppNotification', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL
        ALTER TABLE dbo.AppNotification ADD Type nvarchar(40) NULL;
    IF COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL
        ALTER TABLE dbo.AppNotification ADD DeepLink nvarchar(300) NULL;
END

IF COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD PatientAppId INT NULL;
IF COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NULL
    ALTER TABLE dbo.InstantConsultRequest ADD TeleSessionId INT NULL;

-- FollowUpTask.CompletedAt used by Complete/Reopen
IF OBJECT_ID(N'dbo.FollowUpTask', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.FollowUpTask', N'CompletedAt') IS NULL
    ALTER TABLE dbo.FollowUpTask ADD CompletedAt datetime NULL;

SELECT
    CASE WHEN OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS CareLinkInfoRequest,
    CASE WHEN COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS CareLinkOpinion,
    CASE WHEN COL_LENGTH(N'dbo.AppNotification', N'Type') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS NotificationType,
    CASE WHEN COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS NotificationDeepLink,
    CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'PatientAppId') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS InstantPatientAppId,
    CASE WHEN COL_LENGTH(N'dbo.InstantConsultRequest', N'TeleSessionId') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS InstantTeleSessionId,
    CASE WHEN COL_LENGTH(N'dbo.FollowUpTask', N'CompletedAt') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS FollowUpCompletedAt;

PRINT 'Ensure + verify complete.';
GO
