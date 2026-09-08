/*
    M01 Foundation Security — SINGLE server script (run once on staging, then production).

    *** REQUIRED FIRST (fixes Login 500 / truncated hash) ***
    Run this on the SAME database Old-API uses (HomeoCentrum_Dev / prod):

        ALTER TABLE dbo.UserMaster ALTER COLUMN UserPassword NVARCHAR(500) NOT NULL;

    Verify:
        SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'UserMaster' AND COLUMN_NAME = 'UserPassword';
        -- must return 500

    Then: existing plaintext passwords stay as-is until next Login (lazy PBKDF2),
    or Admin calls POST /api/Account/MigratePlaintextPasswords on New-API.
*/

-- SEC-01.01 — allow PBKDF2$v1$… hashes (was NVARCHAR(50))
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.UserMaster') AND name = N'UserPassword'
)
BEGIN
    ALTER TABLE dbo.UserMaster ALTER COLUMN UserPassword NVARCHAR(500) NOT NULL;
END
GO

-- SEC-02.01
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PasswordResetToken' AND schema_id = SCHEMA_ID(N'dbo'))
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
    CREATE INDEX IX_PasswordResetToken_TokenHash ON dbo.PasswordResetToken (TokenHash);
    CREATE INDEX IX_PasswordResetToken_UserId ON dbo.PasswordResetToken (UserId);
END
GO

-- SEC-06.01
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ConsentType' AND schema_id = SCHEMA_ID(N'dbo'))
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

    INSERT INTO dbo.ConsentType (Code, Name, Description) VALUES
        (N'Privacy', N'Privacy', N'Privacy / data processing'),
        (N'Booking', N'Booking', N'Appointment booking'),
        (N'TeleRecording', N'TeleRecording', N'Telemedicine recording — Phase 11; do not fork AudioCaseConsentLog'),
        (N'PharmacyShare', N'PharmacyShare', N'Pharmacy data share'),
        (N'Marketing', N'Marketing', N'Marketing communications'),
        (N'Caregiver', N'Caregiver', N'Caregiver access');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'ConsentRecord' AND schema_id = SCHEMA_ID(N'dbo'))
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
        CONSTRAINT PK_ConsentRecord PRIMARY KEY CLUSTERED (ConsentRecordId),
        CONSTRAINT FK_ConsentRecord_ConsentType FOREIGN KEY (ConsentTypeId) REFERENCES dbo.ConsentType (ConsentTypeId)
    );
    CREATE INDEX IX_ConsentRecord_Subject ON dbo.ConsentRecord (SubjectType, SubjectId);
END
GO

-- SEC-07.01
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'OtpChallenge' AND schema_id = SCHEMA_ID(N'dbo'))
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
    CREATE INDEX IX_OtpChallenge_Entity ON dbo.OtpChallenge (EntityType, EntityId, Action);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'OtpAuditLog' AND schema_id = SCHEMA_ID(N'dbo'))
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
    CREATE INDEX IX_OtpAuditLog_At ON dbo.OtpAuditLog (At);
END
GO

-- SEC-08.01
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AuditEvent' AND schema_id = SCHEMA_ID(N'dbo'))
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
    CREATE INDEX IX_AuditEvent_At ON dbo.AuditEvent (At);
    CREATE INDEX IX_AuditEvent_ActorUserId ON dbo.AuditEvent (ActorUserId);
END
GO

-- SEC-09.01
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SecureDocument' AND schema_id = SCHEMA_ID(N'dbo'))
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
    CREATE INDEX IX_SecureDocument_Owner ON dbo.SecureDocument (OwnerType, OwnerId);
END
GO

-- =============================================================================
-- FND-01.02 — RoleMaster seeds (Patient / Account / PharmacyPartner)
-- Adjust column list if your RoleMaster differs. Do not change existing roles.
-- =============================================================================
IF COL_LENGTH('dbo.RoleMaster', 'RoleName') IS NOT NULL
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
GO

-- =============================================================================
-- SEC-01.01 — Existing plaintext passwords
-- T-SQL cannot produce PBKDF2$v1$ hashes compatible with the APIs.
-- After ALTER UserPassword NVARCHAR(500):
--   A) Prefer: each user is upgraded automatically on successful Login (lazy migrate).
--   B) Or call New-API POST /api/Account/MigratePlaintextPasswords as AdminPortal once.
-- Do NOT blank or overwrite existing UserPassword values with manual "encryption".
--
-- RECOVERY if login returns 401 after a bad/manual/truncated hash:
--   1) Confirm column width:
--        SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
--        WHERE TABLE_NAME='UserMaster' AND COLUMN_NAME='UserPassword';  -- must be 500+
--   2) Reset affected users to their KNOWN plaintext password (example):
--        UPDATE dbo.UserMaster SET UserPassword = N'YourKnownPlainPassword'
--        WHERE UserName IN (N'admin', N'doctor1');
--   3) Restart APIs, login once — API writes a full PBKDF2$v1$… hash (~90+ chars).
--   4) Or Admin: POST /api/Account/SetUserPassword { "userName":"...", "newPassword":"..." }
-- =============================================================================

-- =============================================================================
-- SEC-04.03 — Separation of duties (Account vs Admin)
-- Menu/RoleDetails seed is environment-specific (ModuleId / MenuId differ).
-- After Account role exists, link money menus to Account and clinical/platform to Admin
-- using your live MenuMaster IDs. Template (commented):
--
-- DECLARE @AccountRoleId INT = (SELECT TOP 1 RoleId FROM RoleMaster WHERE RoleName = N'Account' AND ISNULL(DeleteStatus,0)=0);
-- DECLARE @AdminRoleId INT = (SELECT TOP 1 RoleId FROM RoleMaster WHERE RoleId = 1 OR RoleName = N'Admin');
-- -- INSERT RoleDetails for Account money menus / Admin clinical menus as needed.
-- =============================================================================

PRINT 'M01_Foundation_Security_Server.sql completed. Next: deploy APIs, then login once per user OR run MigratePlaintextPasswords.';
GO
