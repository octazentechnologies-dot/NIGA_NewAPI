-- SmsOtpVerify + FirebaseNotificationLog (FCM send history). Idempotent.
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

PRINT 'SmsOtpVerify_And_FirebaseNotificationLog.sql completed.';
GO
