-- Alternate search terms mapped to SubSectionMaster rubrics

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RubricAlias' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RubricAlias
    (
        RubricAliasId     BIGINT IDENTITY(1,1) NOT NULL,
        SubSectionId      INT NOT NULL,
        AliasText         NVARCHAR(500) NOT NULL,
        NormalizedAlias   NVARCHAR(500) NOT NULL,
        Language          NVARCHAR(10) NOT NULL,
        AliasType         NVARCHAR(50) NOT NULL,
        Weight            DECIMAL(5,4) NOT NULL CONSTRAINT DF_RubricAlias_Weight DEFAULT (1.0),
        Source            NVARCHAR(50) NOT NULL,
        UsageCount        INT NOT NULL CONSTRAINT DF_RubricAlias_UsageCount DEFAULT (0),
        AcceptanceRate    DECIMAL(5,4) NULL,
        IsActive          BIT NOT NULL CONSTRAINT DF_RubricAlias_IsActive DEFAULT (1),
        VersionNo         INT NOT NULL CONSTRAINT DF_RubricAlias_VersionNo DEFAULT (1),
        EnteredBy         INT NULL,
        EnteredDate       DATETIME NOT NULL CONSTRAINT DF_RubricAlias_EnteredDate DEFAULT (GETUTCDATE()),
        ChangedBy         INT NULL,
        ChangedDate       DATETIME NULL,
        CONSTRAINT PK_RubricAlias PRIMARY KEY (RubricAliasId),
        CONSTRAINT FK_RubricAlias_SubSection FOREIGN KEY (SubSectionId)
            REFERENCES dbo.SubSectionMaster (SubSectionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_RubricAlias_Normalized
    ON dbo.RubricAlias (NormalizedAlias)
    WHERE IsActive = 1;
GO

CREATE NONCLUSTERED INDEX IX_RubricAlias_SubSectionId ON dbo.RubricAlias (SubSectionId);
GO
