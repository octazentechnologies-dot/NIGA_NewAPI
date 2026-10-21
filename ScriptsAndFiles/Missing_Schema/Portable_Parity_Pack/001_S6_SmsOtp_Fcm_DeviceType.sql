/*
Purpose: SmsOtpVerify, FirebaseNotificationLog, DevicePushToken.DeviceType (S6 01–02).
Source: S6_Week6/Database scripts/01 + 02. Idempotent.
Risk: Low (additive).
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.SmsOtpVerify', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SmsOtpVerify (
        SmsOtpVerifyId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_SmsOtpVerify PRIMARY KEY,
        Purpose nvarchar(50) NOT NULL,
        MobileDigits nvarchar(20) NOT NULL,
        DestinationMasked nvarchar(100) NOT NULL,
        OtpHash nvarchar(128) NOT NULL,
        Provider nvarchar(30) NULL,
        TwilioMessageSid nvarchar(64) NULL,
        ExpiresAt datetime NOT NULL,
        AttemptCount int NOT NULL CONSTRAINT DF_SmsOtpVerify_Attempt DEFAULT (0),
        LockedUntil datetime NULL,
        CreatedAt datetime NOT NULL,
        VerifiedAt datetime NULL,
        OtpChallengeId bigint NULL
    );
    CREATE INDEX IX_SmsOtpVerify_Mobile_Purpose
        ON dbo.SmsOtpVerify (MobileDigits, Purpose, VerifiedAt, SmsOtpVerifyId DESC);
    PRINT 'Created dbo.SmsOtpVerify';
END
ELSE
    PRINT 'dbo.SmsOtpVerify already exists';
GO

IF OBJECT_ID(N'dbo.FirebaseNotificationLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FirebaseNotificationLog (
        FirebaseNotificationLogId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_FirebaseNotificationLog PRIMARY KEY,
        UserId bigint NOT NULL,
        Action nvarchar(80) NOT NULL,
        Title nvarchar(200) NOT NULL,
        Body nvarchar(2000) NOT NULL,
        TokenCount int NOT NULL CONSTRAINT DF_FcmLog_TokenCount DEFAULT (0),
        SentCount int NOT NULL CONSTRAINT DF_FcmLog_SentCount DEFAULT (0),
        Status nvarchar(20) NOT NULL,
        Detail nvarchar(1000) NULL,
        CreatedAt datetime NOT NULL
    );
    CREATE INDEX IX_FirebaseNotificationLog_User_Created
        ON dbo.FirebaseNotificationLog (UserId, CreatedAt DESC);
    PRINT 'Created dbo.FirebaseNotificationLog';
END
ELSE
    PRINT 'dbo.FirebaseNotificationLog already exists';
GO

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

PRINT '001_S6_SmsOtp_Fcm_DeviceType completed.';
GO
