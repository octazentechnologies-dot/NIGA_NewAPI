-- M16 CON-01 / CON-02 — Family members + Caregiver authorization (New-API / shared DB).
-- Patient clinical rows have no UserId; PatientUserMap links Patient-role logins to a primary PatientId.

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PatientUserMap' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.PatientUserMap
    (
        PatientUserMapId BIGINT IDENTITY(1,1) NOT NULL,
        UserId BIGINT NOT NULL,
        PatientId INT NOT NULL,
        IsPrimary BIT NOT NULL CONSTRAINT DF_PatientUserMap_IsPrimary DEFAULT (1),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_PatientUserMap_DeleteStatus DEFAULT (0),
        EnteredBy NVARCHAR(100) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_PatientUserMap_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_PatientUserMap PRIMARY KEY CLUSTERED (PatientUserMapId)
    );
    CREATE UNIQUE INDEX UX_PatientUserMap_User_Primary
        ON dbo.PatientUserMap (UserId) WHERE IsPrimary = 1 AND DeleteStatus = 0;
    CREATE INDEX IX_PatientUserMap_PatientId ON dbo.PatientUserMap (PatientId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PatientFamilyMember' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.PatientFamilyMember
    (
        FamilyMemberId BIGINT IDENTITY(1,1) NOT NULL,
        OwnerUserId BIGINT NOT NULL,
        OwnerPatientId INT NOT NULL,
        MemberPatientId INT NOT NULL,
        Relation NVARCHAR(50) NOT NULL,
        DeleteStatus BIT NOT NULL CONSTRAINT DF_PatientFamilyMember_DeleteStatus DEFAULT (0),
        EnteredBy NVARCHAR(100) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_PatientFamilyMember_EnteredDate DEFAULT (GETUTCDATE()),
        ChangedBy NVARCHAR(100) NULL,
        ChangedDate DATETIME NULL,
        CONSTRAINT PK_PatientFamilyMember PRIMARY KEY CLUSTERED (FamilyMemberId)
    );
    CREATE INDEX IX_PatientFamilyMember_Owner ON dbo.PatientFamilyMember (OwnerUserId, OwnerPatientId);
    CREATE INDEX IX_PatientFamilyMember_Member ON dbo.PatientFamilyMember (MemberPatientId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'CaregiverAuthorization' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.CaregiverAuthorization
    (
        CaregiverAuthorizationId BIGINT IDENTITY(1,1) NOT NULL,
        PatientId INT NOT NULL,
        CaregiverUserId BIGINT NOT NULL,
        GrantedByUserId BIGINT NULL,
        GrantedAt DATETIME NOT NULL CONSTRAINT DF_CaregiverAuthorization_GrantedAt DEFAULT (GETUTCDATE()),
        RevokedAt DATETIME NULL,
        Scope NVARCHAR(100) NOT NULL CONSTRAINT DF_CaregiverAuthorization_Scope DEFAULT (N'booking'),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_CaregiverAuthorization_DeleteStatus DEFAULT (0),
        CONSTRAINT PK_CaregiverAuthorization PRIMARY KEY CLUSTERED (CaregiverAuthorizationId)
    );
    CREATE INDEX IX_CaregiverAuthorization_Patient ON dbo.CaregiverAuthorization (PatientId);
    CREATE INDEX IX_CaregiverAuthorization_Caregiver ON dbo.CaregiverAuthorization (CaregiverUserId);
END
GO

PRINT 'M16_Family_Caregiver.sql completed.';
GO
