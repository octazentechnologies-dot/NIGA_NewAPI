-- Phase 7: Map SubSectionMaster rubrics to repertory sources (Kent / Complete)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricRepertoryMap' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricRepertoryMap
    (
        RubricRepertoryMapId  BIGINT IDENTITY(1,1) NOT NULL,
        SubSectionId          INT NOT NULL,
        RepertorySourceId     BIGINT NOT NULL,
        SourceRubricKey       NVARCHAR(2000) NULL,
        SourceRubricPath      NVARCHAR(1000) NULL,
        MappingConfidence     DECIMAL(5,4) NOT NULL CONSTRAINT DF_RubricRepertoryMap_Confidence DEFAULT (1.0000),
        IsPrimarySource       BIT NOT NULL CONSTRAINT DF_RubricRepertoryMap_IsPrimary DEFAULT (0),
        IsActive              BIT NOT NULL CONSTRAINT DF_RubricRepertoryMap_IsActive DEFAULT (1),
        EnteredDate           DATETIME NOT NULL CONSTRAINT DF_RubricRepertoryMap_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_RubricRepertoryMap PRIMARY KEY (RubricRepertoryMapId),
        CONSTRAINT FK_RubricRepertoryMap_SubSection FOREIGN KEY (SubSectionId)
            REFERENCES dbo.SubSectionMaster (SubSectionId),
        CONSTRAINT FK_RubricRepertoryMap_Source FOREIGN KEY (RepertorySourceId)
            REFERENCES dbo.RepertorySource (RepertorySourceId),
        CONSTRAINT UQ_RubricRepertoryMap_SubSectionSource UNIQUE (SubSectionId, RepertorySourceId)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RubricRepertoryMap_SubSection'
      AND object_id = OBJECT_ID('dbo.RubricRepertoryMap'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricRepertoryMap_SubSection
        ON dbo.RubricRepertoryMap (SubSectionId)
        WHERE IsActive = 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RubricRepertoryMap_Source'
      AND object_id = OBJECT_ID('dbo.RubricRepertoryMap'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RubricRepertoryMap_Source
        ON dbo.RubricRepertoryMap (RepertorySourceId)
        WHERE IsActive = 1;
END
GO
