/*
  HOMEOCENTRUM AI V4 — Phase 3: Incremental Embedding Refresh
  Adds sync watermark table. Does NOT alter repertory master tables.
*/
SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingSyncState' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingSyncState
    (
        SyncStateId              BIGINT           IDENTITY(1,1) NOT NULL,
        SyncScope                NVARCHAR(50)     NOT NULL,
        EmbeddingVersionId       UNIQUEIDENTIFIER NULL,
        LastSuccessfulSyncUtc    DATETIME2(3)     NULL,
        LastScanStartedUtc       DATETIME2(3)     NULL,
        LastScanCompletedUtc     DATETIME2(3)     NULL,
        LastDetectedCount        INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastDetectedCount DEFAULT (0),
        LastEnqueuedCount        INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastEnqueuedCount DEFAULT (0),
        LastProcessedCount       INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastProcessedCount DEFAULT (0),
        LastSkippedCount         INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastSkippedCount DEFAULT (0),
        LastFailedCount          INT              NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_LastFailedCount DEFAULT (0),
        LastRunCorrelationId     NVARCHAR(64)     NULL,
        LastJobId                UNIQUEIDENTIFIER NULL,
        LastError                NVARCHAR(2000)   NULL,
        CreatedDate              DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingSyncState_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate              DATETIME2(3)     NULL,
        CONSTRAINT PK_AIEmbeddingSyncState PRIMARY KEY CLUSTERED (SyncStateId),
        CONSTRAINT FK_AIEmbeddingSyncState_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AIEmbeddingSyncState_Scope_Version' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingSyncState'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AIEmbeddingSyncState_Scope_Version
        ON dbo.AIEmbeddingSyncState (SyncScope, EmbeddingVersionId)
        WHERE EmbeddingVersionId IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AIEmbeddingSyncState_Scope_Global' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingSyncState'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_AIEmbeddingSyncState_Scope_Global
        ON dbo.AIEmbeddingSyncState (SyncScope)
        WHERE EmbeddingVersionId IS NULL;
GO
