/*
================================================================================
Author       : Tufan Powar
Created      : 17-09-2026
Script       : 01_SEC_M01_Foundation_Security_Server.sql
Purpose      : Foundation security schema for New-API (password width, OTP, consent,
               audit, secure documents) and RoleMaster seeds (Patient / Account / PharmacyPartner).
Use          : Run first on HomeoCentrum_*. Fixes truncated UserPassword hashes that cause Login 500.
Prerequisites: dbo.UserMaster and dbo.RoleMaster already exist.
Idempotent   : Yes. Creates missing tables; adds missing columns/indexes; inserts missing seed rows.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- SEC-01.01 — UserPassword must hold PBKDF2$v1$ hashes (~90+ chars). Skip if table/column missing.
IF OBJECT_ID(N'dbo.UserMaster', N'U') IS NULL
BEGIN
    RAISERROR('dbo.UserMaster is missing. This script only widens UserPassword on an existing table.', 16, 1);
    RETURN;
END
GO

IF COL_LENGTH(N'dbo.UserMaster', N'UserPassword') IS NULL
BEGIN
    ALTER TABLE dbo.UserMaster ADD UserPassword NVARCHAR(500) NOT NULL CONSTRAINT DF_UserMaster_UserPassword_S1 DEFAULT (N'');
END
GO

IF COL_LENGTH(N'dbo.UserMaster', N'UserPassword') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'dbo'
          AND TABLE_NAME = N'UserMaster'
          AND COLUMN_NAME = N'UserPassword'
          AND (CHARACTER_MAXIMUM_LENGTH IS NULL OR CHARACTER_MAXIMUM_LENGTH < 500)
            AND DATA_TYPE LIKE N'n%char'
   )
BEGIN
    ALTER TABLE dbo.UserMaster ALTER COLUMN UserPassword NVARCHAR(500) NOT NULL;
END
GO

-- ---------------------------------------------------------------------------
-- PasswordResetToken
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetToken
    (
        PasswordResetTokenId BIGINT IDENTITY(1,1) NOT NULL,
        UserId BIGINT NOT NULL,
        TokenHash NVARCHAR(128) NOT NULL,
        ExpiresAt DATETIME NOT NULL,
        UsedAt DATETIME NULL,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_PasswordResetToken_CreatedAt DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_PasswordResetToken PRIMARY KEY CLUSTERED (PasswordResetTokenId)
    );
END
GO

IF OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PasswordResetToken', N'UsedAt') IS NULL
    ALTER TABLE dbo.PasswordResetToken ADD UsedAt DATETIME NULL;
IF OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PasswordResetToken', N'CreatedAt') IS NULL
    ALTER TABLE dbo.PasswordResetToken ADD CreatedAt DATETIME NOT NULL CONSTRAINT DF_PasswordResetToken_CreatedAt DEFAULT (GETUTCDATE());
IF OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetToken_TokenHash' AND object_id = OBJECT_ID(N'dbo.PasswordResetToken'))
    CREATE INDEX IX_PasswordResetToken_TokenHash ON dbo.PasswordResetToken (TokenHash);
IF OBJECT_ID(N'dbo.PasswordResetToken', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetToken_UserId' AND object_id = OBJECT_ID(N'dbo.PasswordResetToken'))
    CREATE INDEX IX_PasswordResetToken_UserId ON dbo.PasswordResetToken (UserId);
GO

-- ---------------------------------------------------------------------------
-- ConsentType + seed codes
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsentType
    (
        ConsentTypeId INT IDENTITY(1,1) NOT NULL,
        Code NVARCHAR(50) NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ConsentType_IsActive DEFAULT (1),
        CONSTRAINT PK_ConsentType PRIMARY KEY CLUSTERED (ConsentTypeId),
        CONSTRAINT UX_ConsentType_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ConsentType', N'Description') IS NULL
    ALTER TABLE dbo.ConsentType ADD Description NVARCHAR(500) NULL;
IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ConsentType', N'IsActive') IS NULL
    ALTER TABLE dbo.ConsentType ADD IsActive BIT NOT NULL CONSTRAINT DF_ConsentType_IsActive DEFAULT (1);
GO

IF OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ConsentType', N'Code') IS NOT NULL
BEGIN
    INSERT INTO dbo.ConsentType (Code, Name, Description)
    SELECT v.Code, v.Name, v.Description
    FROM (VALUES
        (N'Privacy', N'Privacy', N'Privacy / data processing'),
        (N'Booking', N'Booking', N'Appointment booking'),
        (N'TeleRecording', N'TeleRecording', N'Telemedicine recording — Phase 11; do not fork AudioCaseConsentLog'),
        (N'PharmacyShare', N'PharmacyShare', N'Pharmacy data share'),
        (N'Marketing', N'Marketing', N'Marketing communications'),
        (N'Caregiver', N'Caregiver', N'Caregiver access')
    ) v(Code, Name, Description)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.ConsentType c WHERE c.Code = v.Code);
END
GO

-- ---------------------------------------------------------------------------
-- ConsentRecord
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsentRecord
    (
        ConsentRecordId BIGINT IDENTITY(1,1) NOT NULL,
        ConsentTypeId INT NOT NULL,
        SubjectType NVARCHAR(50) NOT NULL,
        SubjectId BIGINT NOT NULL,
        GrantedByUserId BIGINT NULL,
        GrantedAt DATETIME NOT NULL,
        WithdrawnAt DATETIME NULL,
        IpAddress NVARCHAR(64) NULL,
        UserAgent NVARCHAR(500) NULL,
        Notes NVARCHAR(500) NULL,
        CONSTRAINT PK_ConsentRecord PRIMARY KEY CLUSTERED (ConsentRecordId)
    );
END
GO

IF OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ConsentRecord', N'WithdrawnAt') IS NULL
    ALTER TABLE dbo.ConsentRecord ADD WithdrawnAt DATETIME NULL;
IF OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ConsentRecord', N'Notes') IS NULL
    ALTER TABLE dbo.ConsentRecord ADD Notes NVARCHAR(500) NULL;
IF OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ConsentRecord_Subject' AND object_id = OBJECT_ID(N'dbo.ConsentRecord'))
    CREATE INDEX IX_ConsentRecord_Subject ON dbo.ConsentRecord (SubjectType, SubjectId);
IF OBJECT_ID(N'dbo.ConsentRecord', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ConsentType', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ConsentRecord_ConsentType')
    ALTER TABLE dbo.ConsentRecord ADD CONSTRAINT FK_ConsentRecord_ConsentType
        FOREIGN KEY (ConsentTypeId) REFERENCES dbo.ConsentType (ConsentTypeId);
GO

-- ---------------------------------------------------------------------------
-- OTP
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OtpChallenge
    (
        OtpChallengeId BIGINT IDENTITY(1,1) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId NVARCHAR(100) NOT NULL,
        DestinationMasked NVARCHAR(100) NOT NULL,
        OtpHash NVARCHAR(128) NOT NULL,
        ExpiresAt DATETIME NOT NULL,
        AttemptCount INT NOT NULL CONSTRAINT DF_OtpChallenge_AttemptCount DEFAULT (0),
        LockedUntil DATETIME NULL,
        CreatedAt DATETIME NOT NULL,
        VerifiedAt DATETIME NULL,
        CONSTRAINT PK_OtpChallenge PRIMARY KEY CLUSTERED (OtpChallengeId)
    );
END
GO

IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.OtpChallenge', N'LockedUntil') IS NULL
    ALTER TABLE dbo.OtpChallenge ADD LockedUntil DATETIME NULL;
IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.OtpChallenge', N'VerifiedAt') IS NULL
    ALTER TABLE dbo.OtpChallenge ADD VerifiedAt DATETIME NULL;
IF OBJECT_ID(N'dbo.OtpChallenge', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OtpChallenge_Entity' AND object_id = OBJECT_ID(N'dbo.OtpChallenge'))
    CREATE INDEX IX_OtpChallenge_Entity ON dbo.OtpChallenge (EntityType, EntityId, Action);
GO

IF OBJECT_ID(N'dbo.OtpAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OtpAuditLog
    (
        OtpAuditLogId BIGINT IDENTITY(1,1) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId NVARCHAR(100) NOT NULL,
        ToMasked NVARCHAR(100) NOT NULL,
        Success BIT NOT NULL,
        At DATETIME NOT NULL,
        ActorUserId BIGINT NULL,
        CONSTRAINT PK_OtpAuditLog PRIMARY KEY CLUSTERED (OtpAuditLogId)
    );
END
GO

IF OBJECT_ID(N'dbo.OtpAuditLog', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_OtpAuditLog_At' AND object_id = OBJECT_ID(N'dbo.OtpAuditLog'))
    CREATE INDEX IX_OtpAuditLog_At ON dbo.OtpAuditLog (At);
GO

-- ---------------------------------------------------------------------------
-- AuditEvent / SecureDocument
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.AuditEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditEvent
    (
        AuditEventId BIGINT IDENTITY(1,1) NOT NULL,
        ActorUserId BIGINT NULL,
        Role NVARCHAR(50) NULL,
        Action NVARCHAR(100) NOT NULL,
        Entity NVARCHAR(100) NOT NULL,
        OldJson NVARCHAR(MAX) NULL,
        NewJson NVARCHAR(MAX) NULL,
        At DATETIME NOT NULL,
        CorrelationId NVARCHAR(64) NULL,
        CONSTRAINT PK_AuditEvent PRIMARY KEY CLUSTERED (AuditEventId)
    );
END
GO

IF OBJECT_ID(N'dbo.AuditEvent', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.AuditEvent', N'CorrelationId') IS NULL
    ALTER TABLE dbo.AuditEvent ADD CorrelationId NVARCHAR(64) NULL;
IF OBJECT_ID(N'dbo.AuditEvent', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEvent_At' AND object_id = OBJECT_ID(N'dbo.AuditEvent'))
    CREATE INDEX IX_AuditEvent_At ON dbo.AuditEvent (At);
IF OBJECT_ID(N'dbo.AuditEvent', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditEvent_ActorUserId' AND object_id = OBJECT_ID(N'dbo.AuditEvent'))
    CREATE INDEX IX_AuditEvent_ActorUserId ON dbo.AuditEvent (ActorUserId);
GO

IF OBJECT_ID(N'dbo.SecureDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SecureDocument
    (
        SecureDocumentId BIGINT IDENTITY(1,1) NOT NULL,
        OwnerType NVARCHAR(50) NOT NULL,
        OwnerId BIGINT NOT NULL,
        BlobPath NVARCHAR(1000) NOT NULL,
        FileName NVARCHAR(255) NULL,
        Mime NVARCHAR(100) NULL,
        Hash NVARCHAR(128) NULL,
        CreatedBy BIGINT NULL,
        CreatedAt DATETIME NOT NULL,
        CONSTRAINT PK_SecureDocument PRIMARY KEY CLUSTERED (SecureDocumentId)
    );
END
GO

IF OBJECT_ID(N'dbo.SecureDocument', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.SecureDocument', N'Hash') IS NULL
    ALTER TABLE dbo.SecureDocument ADD Hash NVARCHAR(128) NULL;
IF OBJECT_ID(N'dbo.SecureDocument', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SecureDocument_Owner' AND object_id = OBJECT_ID(N'dbo.SecureDocument'))
    CREATE INDEX IX_SecureDocument_Owner ON dbo.SecureDocument (OwnerType, OwnerId);
GO

-- ---------------------------------------------------------------------------
-- FND-01.02 RoleMaster seeds
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.RoleMaster', N'U') IS NULL OR COL_LENGTH(N'dbo.RoleMaster', N'RoleName') IS NULL
BEGIN
    RAISERROR('dbo.RoleMaster.RoleName is missing. Cannot seed Patient/Account/PharmacyPartner.', 16, 1);
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.RoleMaster', N'FirmIds') IS NOT NULL
       AND COL_LENGTH(N'dbo.RoleMaster', N'DeleteStatus') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.RoleMaster WHERE RoleName = N'Patient')
            INSERT INTO dbo.RoleMaster (RoleName, FirmIds, DeleteStatus, EnteredBy, EnteredDate)
            VALUES (N'Patient', N'0', 0, N'M01', GETUTCDATE());
        IF NOT EXISTS (SELECT 1 FROM dbo.RoleMaster WHERE RoleName = N'Account')
            INSERT INTO dbo.RoleMaster (RoleName, FirmIds, DeleteStatus, EnteredBy, EnteredDate)
            VALUES (N'Account', N'0', 0, N'M01', GETUTCDATE());
        IF NOT EXISTS (SELECT 1 FROM dbo.RoleMaster WHERE RoleName = N'PharmacyPartner')
            INSERT INTO dbo.RoleMaster (RoleName, FirmIds, DeleteStatus, EnteredBy, EnteredDate)
            VALUES (N'PharmacyPartner', N'0', 0, N'M01', GETUTCDATE());
    END
    ELSE
    BEGIN
        RAISERROR('RoleMaster exists but FirmIds/DeleteStatus columns are missing. Seed skipped.', 10, 1);
    END
END
GO

PRINT '01_SEC_M01_Foundation_Security_Server.sql completed.';
GO
