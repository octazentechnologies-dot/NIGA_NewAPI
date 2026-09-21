/*
================================================================================
Author       : Tufan Powar
Created      : 20-09-2026
Script       : 11_FAMILY_RELATION_MASTER.sql
Purpose      : FamilyRelationMaster + RelationId on PatientFamilyMember.
Use          : Dev HomeoCentrum_Dev after 01–10. Idempotent.
================================================================================
*/

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;

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

IF OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientFamilyMember', N'RelationId') IS NULL
BEGIN
    ALTER TABLE dbo.PatientFamilyMember ADD RelationId INT NULL;
    PRINT 'ADDED PatientFamilyMember.RelationId';
END
ELSE
    PRINT 'SKIP PatientFamilyMember.RelationId';

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = N'FK_PatientFamilyMember_FamilyRelationMaster'
      AND parent_object_id = OBJECT_ID(N'dbo.PatientFamilyMember')
)
   AND OBJECT_ID(N'dbo.PatientFamilyMember', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PatientFamilyMember', N'RelationId') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PatientFamilyMember
        ADD CONSTRAINT FK_PatientFamilyMember_FamilyRelationMaster
        FOREIGN KEY (RelationId) REFERENCES dbo.FamilyRelationMaster (RelationId);
    PRINT 'ADDED FK_PatientFamilyMember_FamilyRelationMaster';
END

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
SELECT s.RelationName, s.SortOrder, 0, N'S2-FAMILY', GETUTCDATE()
FROM @Seed s
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.FamilyRelationMaster r
    WHERE r.RelationName = s.RelationName AND ISNULL(r.DeleteStatus, 0) = 0
);

PRINT '11_FAMILY_RELATION_MASTER.sql completed.';
GO
