-- Phase 7: Repertory source registry (Kent, Complete, future Boericke/Phatak)

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'RepertorySource' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.RepertorySource
    (
        RepertorySourceId   BIGINT IDENTITY(1,1) NOT NULL,
        SourceCode          NVARCHAR(20) NOT NULL,
        SourceName          NVARCHAR(100) NOT NULL,
        AuthorId            INT NULL,
        PriorityOrder       INT NOT NULL CONSTRAINT DF_RepertorySource_PriorityOrder DEFAULT (100),
        IsActive            BIT NOT NULL CONSTRAINT DF_RepertorySource_IsActive DEFAULT (1),
        EnteredDate         DATETIME NOT NULL CONSTRAINT DF_RepertorySource_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_RepertorySource PRIMARY KEY (RepertorySourceId),
        CONSTRAINT UQ_RepertorySource_SourceCode UNIQUE (SourceCode)
    );
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_RepertorySource_ActivePriority'
      AND object_id = OBJECT_ID('dbo.RepertorySource'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_RepertorySource_ActivePriority
        ON dbo.RepertorySource (IsActive, PriorityOrder)
        INCLUDE (SourceCode, SourceName);
END
GO
