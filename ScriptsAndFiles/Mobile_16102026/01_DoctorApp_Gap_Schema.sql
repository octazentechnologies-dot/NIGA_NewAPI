-- DoctorApp gap schema for HomeoCentrum_Dev (idempotent)
-- BE-007 CareLinkInfoRequest, BE-009 AppNotification Type/DeepLink, CareLink Opinion

SET NOCOUNT ON;
SET XACT_ABORT ON;

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
ELSE
    PRINT 'dbo.CareLinkRequest already exists';

IF COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') IS NULL
BEGIN
    ALTER TABLE dbo.CareLinkRequest ADD Opinion nvarchar(1000) NULL;
    PRINT 'Added CareLinkRequest.Opinion';
END
ELSE
    PRINT 'CareLinkRequest.Opinion already exists';

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
ELSE
    PRINT 'dbo.CareLinkInfoRequest already exists';

IF OBJECT_ID(N'dbo.AppNotification', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.AppNotification', N'Type') IS NULL
    BEGIN
        ALTER TABLE dbo.AppNotification ADD Type nvarchar(40) NULL;
        PRINT 'Added AppNotification.Type';
    END
    ELSE
        PRINT 'AppNotification.Type already exists';

    IF COL_LENGTH(N'dbo.AppNotification', N'DeepLink') IS NULL
    BEGIN
        ALTER TABLE dbo.AppNotification ADD DeepLink nvarchar(300) NULL;
        PRINT 'Added AppNotification.DeepLink';
    END
    ELSE
        PRINT 'AppNotification.DeepLink already exists';
END
ELSE
    PRINT 'dbo.AppNotification missing — skipped Type/DeepLink';

-- Verify
SELECT
    OBJECT_ID(N'dbo.CareLinkInfoRequest', N'U') AS CareLinkInfoRequestObjectId,
    COL_LENGTH(N'dbo.CareLinkRequest', N'Opinion') AS CareLinkOpinionLen,
    COL_LENGTH(N'dbo.AppNotification', N'Type') AS AppNotificationTypeLen,
    COL_LENGTH(N'dbo.AppNotification', N'DeepLink') AS AppNotificationDeepLinkLen;

PRINT 'DoctorApp gap schema done.';
GO
