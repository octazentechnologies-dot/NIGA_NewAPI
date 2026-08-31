/*
    HomeoCentrum AI Engine V3 — Task 3: Repertory-grounded sensation/causation ontology
    Database: HomeoCentrum_Production
    Manual SSMS deploy only. Safe to re-run (IF NOT EXISTS).
    Does NOT modify SectionMaster / SubSectionMaster.
*/

IF NOT EXISTS (
    SELECT 1 FROM sys.tables
    WHERE name = N'AISensationOntology' AND schema_id = SCHEMA_ID(N'dbo')
)
BEGIN
    CREATE TABLE dbo.AISensationOntology
    (
        OntologyId BIGINT IDENTITY(1, 1) NOT NULL,
        Pattern NVARCHAR(200) NOT NULL,
        SubSectionId INT NOT NULL,
        SubSectionName NVARCHAR(500) NULL,
        SensationCategory NVARCHAR(80) NOT NULL,
        ExtractedAt DATETIME NOT NULL CONSTRAINT DF_AISensationOntology_ExtractedAt DEFAULT (GETUTCDATE()),
        IsActive BIT NOT NULL CONSTRAINT DF_AISensationOntology_IsActive DEFAULT (1),
        CONSTRAINT PK_AISensationOntology PRIMARY KEY CLUSTERED (OntologyId)
    );

    CREATE NONCLUSTERED INDEX IX_AISensationOntology_Pattern
        ON dbo.AISensationOntology (Pattern, IsActive)
        INCLUDE (SubSectionId, SensationCategory, SubSectionName);

    CREATE NONCLUSTERED INDEX IX_AISensationOntology_SubSectionId
        ON dbo.AISensationOntology (SubSectionId)
        WHERE IsActive = 1;
END
GO

IF COL_LENGTH('dbo.AIMetaphorResolution', 'GroundedInOntology') IS NULL
    ALTER TABLE dbo.AIMetaphorResolution ADD GroundedInOntology BIT NOT NULL
        CONSTRAINT DF_AIMetaphorResolution_GroundedInOntology DEFAULT (0);
GO

IF COL_LENGTH('dbo.AIMetaphorResolution', 'OntologyId') IS NULL
    ALTER TABLE dbo.AIMetaphorResolution ADD OntologyId BIGINT NULL;
GO

IF COL_LENGTH('dbo.AIMetaphorResolution', 'IsMetaphor') IS NULL
    ALTER TABLE dbo.AIMetaphorResolution ADD IsMetaphor BIT NOT NULL
        CONSTRAINT DF_AIMetaphorResolution_IsMetaphor DEFAULT (0);
GO
