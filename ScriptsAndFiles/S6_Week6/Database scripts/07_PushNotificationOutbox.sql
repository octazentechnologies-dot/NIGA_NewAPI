/*
S6 — FCM push delivery outbox (separate from SMS dbo.NotificationOutbox).
Queues push intent after business commit; PushNotificationOutboxWorker drains asynchronously.
Idempotent on DeliveryId. Inbox (AppNotification) remains a separate table.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.PushNotificationOutbox', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PushNotificationOutbox
    (
        PushNotificationOutboxId BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_PushNotificationOutbox PRIMARY KEY,
        DeliveryId UNIQUEIDENTIFIER NOT NULL,
        UserId BIGINT NOT NULL,
        Action NVARCHAR(80) NOT NULL,
        NotificationType NVARCHAR(40) NULL,
        Title NVARCHAR(200) NOT NULL,
        Body NVARCHAR(2000) NOT NULL,
        DataJson NVARCHAR(MAX) NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_PushOutbox_Status DEFAULT (N'PENDING'),
        AttemptCount INT NOT NULL
            CONSTRAINT DF_PushOutbox_Attempt DEFAULT (0),
        NextAttemptAt DATETIME2 NOT NULL
            CONSTRAINT DF_PushOutbox_NextAttempt DEFAULT (SYSUTCDATETIME()),
        LockedAt DATETIME2 NULL,
        LockedBy NVARCHAR(100) NULL,
        LastError NVARCHAR(1000) NULL,
        ProviderAcceptedCount INT NOT NULL
            CONSTRAINT DF_PushOutbox_Accepted DEFAULT (0),
        TokenCount INT NOT NULL
            CONSTRAINT DF_PushOutbox_TokenCount DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_PushOutbox_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2 NOT NULL
            CONSTRAINT DF_PushOutbox_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_PushNotificationOutbox_DeliveryId UNIQUE (DeliveryId)
    );

    CREATE INDEX IX_PushNotificationOutbox_Drain
        ON dbo.PushNotificationOutbox (Status, NextAttemptAt)
        INCLUDE (UserId, AttemptCount);

    CREATE INDEX IX_PushNotificationOutbox_User_Created
        ON dbo.PushNotificationOutbox (UserId, CreatedAt DESC);

    PRINT 'Created dbo.PushNotificationOutbox';
END
ELSE
    PRINT 'dbo.PushNotificationOutbox already exists';

-- Idempotent index align when table pre-existed without secondary index.
IF OBJECT_ID(N'dbo.PushNotificationOutbox', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_PushNotificationOutbox_User_Created'
          AND object_id = OBJECT_ID(N'dbo.PushNotificationOutbox'))
BEGIN
    CREATE INDEX IX_PushNotificationOutbox_User_Created
        ON dbo.PushNotificationOutbox (UserId, CreatedAt DESC);
    PRINT 'Added IX_PushNotificationOutbox_User_Created';
END

PRINT '07_PushNotificationOutbox.sql completed.';
GO
