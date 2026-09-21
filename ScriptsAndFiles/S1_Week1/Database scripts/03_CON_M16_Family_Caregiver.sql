/*
================================================================================
Author       : Tufan Powar
Created      : 17-09-2026
Script       : 03_CON_M16_Family_Caregiver.sql
Purpose      : Family/caregiver tables: PatientUserMap, PatientFamilyMember, CaregiverAuthorization.
Use          : Run after script 01. Links Patient-role UserMaster rows to clinical PatientId.
Prerequisites: Shared HomeoCentrum_* database.
Idempotent   : Yes. Creates tables if missing; adds missing columns/indexes if table already exists.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NULL
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
END
GO

IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientUserMap', N'IsPrimary') IS NULL
    ALTER TABLE dbo.PatientUserMap ADD IsPrimary BIT NOT NULL CONSTRAINT DF_PatientUserMap_IsPrimary DEFAULT (1);
IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientUserMap', N'DeleteStatus') IS NULL
    ALTER TABLE dbo.PatientUserMap ADD DeleteStatus BIT NOT NULL CONSTRAINT DF_PatientUserMap_DeleteStatus DEFAULT (0);
IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientUserMap', N'EnteredBy') IS NULL
    ALTER TABLE dbo.PatientUserMap ADD EnteredBy NVARCHAR(100) NULL;
IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientUserMap', N'EnteredDate') IS NULL
    ALTER TABLE dbo.PatientUserMap ADD EnteredDate DATETIME NOT NULL CONSTRAINT DF_PatientUserMap_EnteredDate DEFAULT (GETUTCDATE());
GO

IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PatientUserMap_User_Primary' AND object_id = OBJECT_ID(N'dbo.PatientUserMap'))
    CREATE UNIQUE INDEX UX_PatientUserMap_User_Primary ON dbo.PatientUserMap (UserId) WHERE IsPrimary = 1 AND DeleteStatus = 0;
IF OBJECT_ID(N'dbo.PatientUserMap', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PatientUserMap_PatientId' AND object_id = OBJECT_ID(N'dbo.PatientUserMap'))
    CREATE INDEX IX_PatientUserMap_PatientId ON dbo.PatientUserMap (PatientId);
GO

IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NULL
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
END
GO

IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientFamilyMember', N'Relation') IS NULL
    ALTER TABLE dbo.PatientFamilyMember ADD Relation NVARCHAR(50) NOT NULL CONSTRAINT DF_PatientFamilyMember_Relation DEFAULT (N'other');
IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientFamilyMember', N'ChangedBy') IS NULL
    ALTER TABLE dbo.PatientFamilyMember ADD ChangedBy NVARCHAR(100) NULL;
IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PatientFamilyMember', N'ChangedDate') IS NULL
    ALTER TABLE dbo.PatientFamilyMember ADD ChangedDate DATETIME NULL;
GO

IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PatientFamilyMember_Owner' AND object_id = OBJECT_ID(N'dbo.PatientFamilyMember'))
    CREATE INDEX IX_PatientFamilyMember_Owner ON dbo.PatientFamilyMember (OwnerUserId, OwnerPatientId);
IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PatientFamilyMember_Member' AND object_id = OBJECT_ID(N'dbo.PatientFamilyMember'))
    CREATE INDEX IX_PatientFamilyMember_Member ON dbo.PatientFamilyMember (MemberPatientId);
GO

IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NULL
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
END
GO

IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.CaregiverAuthorization', N'RevokedAt') IS NULL
    ALTER TABLE dbo.CaregiverAuthorization ADD RevokedAt DATETIME NULL;
IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.CaregiverAuthorization', N'Scope') IS NULL
    ALTER TABLE dbo.CaregiverAuthorization ADD Scope NVARCHAR(100) NOT NULL CONSTRAINT DF_CaregiverAuthorization_Scope DEFAULT (N'booking');
IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.CaregiverAuthorization', N'DeleteStatus') IS NULL
    ALTER TABLE dbo.CaregiverAuthorization ADD DeleteStatus BIT NOT NULL CONSTRAINT DF_CaregiverAuthorization_DeleteStatus DEFAULT (0);
GO

IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CaregiverAuthorization_Patient' AND object_id = OBJECT_ID(N'dbo.CaregiverAuthorization'))
    CREATE INDEX IX_CaregiverAuthorization_Patient ON dbo.CaregiverAuthorization (PatientId);
IF OBJECT_ID(N'dbo.CaregiverAuthorization', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CaregiverAuthorization_Caregiver' AND object_id = OBJECT_ID(N'dbo.CaregiverAuthorization'))
    CREATE INDEX IX_CaregiverAuthorization_Caregiver ON dbo.CaregiverAuthorization (CaregiverUserId);
GO

PRINT '03_CON_M16_Family_Caregiver.sql — family/caregiver tables done.';
GO

-- CON-01.02 relation dropdown (also S2 script 11; idempotent here so S1 pack is standalone)
IF OBJECT_ID(N'dbo.FamilyRelationMaster', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyRelationMaster
    (
        RelationId INT IDENTITY(1,1) NOT NULL,
        RelationName NVARCHAR(50) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_FamilyRelationMaster_SortOrder DEFAULT (0),
        DeleteStatus BIT NOT NULL CONSTRAINT DF_FamilyRelationMaster_DeleteStatus DEFAULT (0),
        EnteredBy NVARCHAR(100) NULL,
        EnteredDate DATETIME NOT NULL CONSTRAINT DF_FamilyRelationMaster_EnteredDate DEFAULT (GETUTCDATE()),
        ChangedBy NVARCHAR(100) NULL,
        ChangedDate DATETIME NULL,
        CONSTRAINT PK_FamilyRelationMaster PRIMARY KEY CLUSTERED (RelationId)
    );
    CREATE UNIQUE INDEX UX_FamilyRelationMaster_Name
        ON dbo.FamilyRelationMaster (RelationName)
        WHERE DeleteStatus = 0;
    PRINT 'CREATED FamilyRelationMaster';
END
ELSE
    PRINT 'SKIP FamilyRelationMaster already present';
GO

IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientFamilyMember', N'RelationId') IS NULL
BEGIN
    ALTER TABLE dbo.PatientFamilyMember ADD RelationId INT NULL;
    PRINT 'ADDED PatientFamilyMember.RelationId';
END
ELSE
    PRINT 'SKIP PatientFamilyMember.RelationId';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PatientFamilyMember_FamilyRelationMaster'
      AND parent_object_id = OBJECT_ID(N'dbo.PatientFamilyMember')
)
   AND OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientFamilyMember', N'RelationId') IS NOT NULL
   AND OBJECT_ID(N'dbo.FamilyRelationMaster', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PatientFamilyMember
        ADD CONSTRAINT FK_PatientFamilyMember_FamilyRelationMaster
        FOREIGN KEY (RelationId) REFERENCES dbo.FamilyRelationMaster (RelationId);
    PRINT 'ADDED FK_PatientFamilyMember_FamilyRelationMaster';
END
GO

DECLARE @Seed TABLE (RelationName NVARCHAR(50) PRIMARY KEY, SortOrder INT);
INSERT INTO @Seed (RelationName, SortOrder) VALUES
    (N'Spouse', 10),
    (N'Father', 20),
    (N'Mother', 30),
    (N'Son', 40),
    (N'Daughter', 50),
    (N'Brother', 60),
    (N'Sister', 70),
    (N'Grandfather', 80),
    (N'Grandmother', 90),
    (N'Uncle', 100),
    (N'Aunt', 110),
    (N'Nephew', 120),
    (N'Niece', 130),
    (N'Cousin', 140),
    (N'Friend', 150),
    (N'Other', 160);

INSERT INTO dbo.FamilyRelationMaster (RelationName, SortOrder, DeleteStatus, EnteredBy, EnteredDate)
SELECT s.RelationName, s.SortOrder, 0, N'S1-Week1', '2026-09-18'
FROM @Seed s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.FamilyRelationMaster r
    WHERE r.RelationName = s.RelationName AND ISNULL(r.DeleteStatus, 0) = 0
);
PRINT '03_CON_M16_Family_Caregiver.sql completed.';
GO
