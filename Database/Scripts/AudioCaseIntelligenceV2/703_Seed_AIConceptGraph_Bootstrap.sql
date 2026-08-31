-- V3 bootstrap: homeopathic concept → rubric mapping seeds (epilepsy gold patterns)
-- SubSectionId values must exist in SubSectionMaster — adjust IDs per environment if needed.

IF OBJECT_ID(N'dbo.AIConceptMappingBootstrap', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIConceptMappingBootstrap
    (
        ConceptMappingBootstrapId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HomeopathicConceptPattern NVARCHAR(500) NOT NULL,
        SubSectionNamePattern     NVARCHAR(500) NOT NULL,
        Domain                    NVARCHAR(100) NULL,
        PriorityOrder             INT NOT NULL CONSTRAINT DF_AIConceptMappingBootstrap_Priority DEFAULT (1),
        IsActive                  BIT NOT NULL CONSTRAINT DF_AIConceptMappingBootstrap_Active DEFAULT (1),
        EnteredDate               DATETIME2 NOT NULL CONSTRAINT DF_AIConceptMappingBootstrap_Entered DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AIConceptMappingBootstrap WHERE HomeopathicConceptPattern = N'Convulsion Aura')
BEGIN
    INSERT INTO dbo.AIConceptMappingBootstrap (HomeopathicConceptPattern, SubSectionNamePattern, Domain, PriorityOrder) VALUES
    (N'Convulsion Aura',           N'%CONVULS%AURA%',           N'Neurology', 1),
    (N'Fear Before Convulsion',    N'%FEAR%CONVULS%',          N'Neurology', 1),
    (N'Fear Before Convulsion',    N'%FEAR%FIT%',              N'Neurology', 2),
    (N'Motor Weakness Before Convulsion', N'%DROPPING%THING%', N'Neurology', 1),
    (N'Anticipatory Fear of Seizure', N'%FEAR%EPIL%',          N'Neurology', 1),
    (N'Prodromal Shivering',       N'%SHIVER%CONVULS%',        N'Neurology', 2),
    (N'Electric Shock Sensation',  N'%ELECTRIC%SHOCK%',        N'General',   1),
    (N'Palpitation',               N'%PALPITAT%',              N'Cardiac',   1),
    (N'Bursting Headache',         N'%BURST%HEAD%',            N'Head',      1);
END
GO

PRINT '703_Seed_AIConceptGraph_Bootstrap completed.';
GO
