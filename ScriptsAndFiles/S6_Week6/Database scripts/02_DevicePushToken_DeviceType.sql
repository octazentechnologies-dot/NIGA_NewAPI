-- FCM device tokens: add DeviceType (Website | Android | iOS) on DevicePushToken.
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DevicePushToken
    (
        DevicePushTokenId BIGINT IDENTITY(1,1) NOT NULL,
        UserId BIGINT NOT NULL,
        Platform NVARCHAR(20) NOT NULL CONSTRAINT DF_DevicePushToken_Platform DEFAULT (N'FCM'),
        DeviceType NVARCHAR(20) NOT NULL CONSTRAINT DF_DevicePushToken_DeviceType DEFAULT (N'Website'),
        Token NVARCHAR(512) NOT NULL,
        DeviceId NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_DevicePushToken_CreatedAt DEFAULT (GETUTCDATE()),
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_DevicePushToken_UpdatedAt DEFAULT (GETUTCDATE()),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_DevicePushToken_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_DevicePushToken PRIMARY KEY CLUSTERED (DevicePushTokenId)
    );
    CREATE INDEX IX_DevicePushToken_UserId_Token ON dbo.DevicePushToken (UserId, Token);
    PRINT 'Created dbo.DevicePushToken';
END
GO

IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NULL
BEGIN
    ALTER TABLE dbo.DevicePushToken
        ADD DeviceType NVARCHAR(20) NOT NULL
            CONSTRAINT DF_DevicePushToken_DeviceType DEFAULT (N'Website');
    PRINT 'Added DevicePushToken.DeviceType';
END
GO

-- Backfill from legacy Platform values when possible.
IF OBJECT_ID(N'dbo.DevicePushToken', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.DevicePushToken', N'DeviceType') IS NOT NULL
BEGIN
    UPDATE dbo.DevicePushToken
    SET DeviceType = N'iOS'
    WHERE DeleteStatus = 0
      AND (Platform LIKE N'%APNs%' OR Platform LIKE N'%APNS%' OR Platform LIKE N'%ios%')
      AND (DeviceType IS NULL OR DeviceType = N'' OR DeviceType = N'Website');

    UPDATE dbo.DevicePushToken
    SET DeviceType = N'Android'
    WHERE DeleteStatus = 0
      AND Platform LIKE N'%android%'
      AND (DeviceType IS NULL OR DeviceType = N'' OR DeviceType = N'Website');
END
GO

PRINT 'DevicePushToken_DeviceType.sql completed.';
GO
