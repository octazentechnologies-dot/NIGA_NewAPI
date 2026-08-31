/*
  HOMEOCENTRUM AI V4 — Phase 1: Enterprise Embedding Infrastructure
  Creates NEW tables only. Does NOT alter repertory master tables.
  Legacy dbo.RubricEmbeddings remains untouched for backward compatibility.
*/
SET NOCOUNT ON;
GO

/* ---------- AIEmbeddingVersion ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingVersion' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingVersion
    (
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AIEmbeddingVersion_Id DEFAULT (NEWSEQUENTIALID()),
        VersionCode          NVARCHAR(50)     NOT NULL,
        ModelProvider        NVARCHAR(50)     NOT NULL,
        ModelName            NVARCHAR(100)    NOT NULL,
        ModelVersion         NVARCHAR(50)     NULL,
        DimensionCount       INT              NOT NULL,
        VectorFormat         NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingVersion_VectorFormat DEFAULT (N'JsonFloatArray'),
        IsActive             BIT              NOT NULL CONSTRAINT DF_AIEmbeddingVersion_IsActive DEFAULT (1),
        IsCurrent            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingVersion_IsCurrent DEFAULT (0),
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingVersion_Status DEFAULT (N'Draft'),
        Description          NVARCHAR(500)    NULL,
        ConfigurationJson    NVARCHAR(MAX)    NULL,
        CreatedByUserId      INT              NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingVersion_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingVersion_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIEmbeddingVersion PRIMARY KEY CLUSTERED (EmbeddingVersionId),
        CONSTRAINT UQ_AIEmbeddingVersion_VersionCode UNIQUE (VersionCode)
    );
END
GO

/* ---------- AIRubricEmbedding ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIRubricEmbedding' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIRubricEmbedding
    (
        RubricEmbeddingId    BIGINT           IDENTITY(1,1) NOT NULL,
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL,
        RubricId             INT              NOT NULL,
        SourceText           NVARCHAR(2000)   NOT NULL,
        TextHash             CHAR(64)         NOT NULL,
        EmbeddingPayloadJson NVARCHAR(MAX)    NULL,
        DimensionCount       INT              NOT NULL,
        RevisionNo           INT              NOT NULL CONSTRAINT DF_AIRubricEmbedding_RevisionNo DEFAULT (1),
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIRubricEmbedding_Status DEFAULT (N'Pending'),
        SourceType           NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIRubricEmbedding_SourceType DEFAULT (N'SubSectionName'),
        LastJobId            UNIQUEIDENTIFIER NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIRubricEmbedding_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIRubricEmbedding_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIRubricEmbedding PRIMARY KEY CLUSTERED (RubricEmbeddingId),
        CONSTRAINT FK_AIRubricEmbedding_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId),
        CONSTRAINT FK_AIRubricEmbedding_SubSection FOREIGN KEY (RubricId)
            REFERENCES dbo.SubSectionMaster (SubSectionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIRubricEmbedding_Version_Rubric_Active' AND object_id = OBJECT_ID(N'dbo.AIRubricEmbedding'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_AIRubricEmbedding_Version_Rubric_Active
        ON dbo.AIRubricEmbedding (EmbeddingVersionId, RubricId)
        WHERE IsDeleted = 0 AND Status IN (N'Pending', N'Active');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIRubricEmbedding_TextHash' AND object_id = OBJECT_ID(N'dbo.AIRubricEmbedding'))
    CREATE NONCLUSTERED INDEX IX_AIRubricEmbedding_TextHash ON dbo.AIRubricEmbedding (TextHash, EmbeddingVersionId);
GO

/* ---------- AIConceptEmbedding ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIConceptEmbedding' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIConceptEmbedding
    (
        ConceptEmbeddingId   BIGINT           IDENTITY(1,1) NOT NULL,
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL,
        ConceptKey           NVARCHAR(200)    NOT NULL,
        ConceptType          NVARCHAR(50)     NOT NULL,
        SourceDomain         NVARCHAR(50)     NULL,
        SourceText             NVARCHAR(2000)   NOT NULL,
        TextHash             CHAR(64)         NOT NULL,
        EmbeddingPayloadJson NVARCHAR(MAX)    NULL,
        DimensionCount       INT              NOT NULL,
        RevisionNo           INT              NOT NULL CONSTRAINT DF_AIConceptEmbedding_RevisionNo DEFAULT (1),
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIConceptEmbedding_Status DEFAULT (N'Pending'),
        LastJobId            UNIQUEIDENTIFIER NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIConceptEmbedding_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIConceptEmbedding_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIConceptEmbedding PRIMARY KEY CLUSTERED (ConceptEmbeddingId),
        CONSTRAINT FK_AIConceptEmbedding_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIConceptEmbedding_Version_Key_Type_Active' AND object_id = OBJECT_ID(N'dbo.AIConceptEmbedding'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_AIConceptEmbedding_Version_Key_Type_Active
        ON dbo.AIConceptEmbedding (EmbeddingVersionId, ConceptKey, ConceptType)
        WHERE IsDeleted = 0 AND Status IN (N'Pending', N'Active');
GO

/* ---------- AIEmbeddingJob ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingJob' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingJob
    (
        JobId                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AIEmbeddingJob_Id DEFAULT (NEWID()),
        EmbeddingVersionId   UNIQUEIDENTIFIER NOT NULL,
        JobType              NVARCHAR(50)     NOT NULL,
        TriggerSource        NVARCHAR(50)     NULL,
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingJob_Status DEFAULT (N'Queued'),
        Priority             INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_Priority DEFAULT (100),
        TotalItems           INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_TotalItems DEFAULT (0),
        ProcessedItems       INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_ProcessedItems DEFAULT (0),
        FailedItems          INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_FailedItems DEFAULT (0),
        SkippedItems         INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_SkippedItems DEFAULT (0),
        RetryCount           INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_RetryCount DEFAULT (0),
        MaxRetries           INT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_MaxRetries DEFAULT (3),
        CorrelationId        NVARCHAR(64)     NULL,
        ErrorSummary         NVARCHAR(2000)   NULL,
        StartedAtUtc         DATETIME2(3)     NULL,
        CompletedAtUtc       DATETIME2(3)     NULL,
        CreatedByUserId      INT              NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingJob_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingJob_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIEmbeddingJob PRIMARY KEY CLUSTERED (JobId),
        CONSTRAINT FK_AIEmbeddingJob_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingJob_Status_Priority' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingJob'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingJob_Status_Priority
        ON dbo.AIEmbeddingJob (Status, Priority, CreatedDate)
        WHERE IsDeleted = 0;
GO

/* ---------- AIEmbeddingQueue ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingQueue' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingQueue
    (
        QueueId              BIGINT           IDENTITY(1,1) NOT NULL,
        JobId                UNIQUEIDENTIFIER NOT NULL,
        ItemType             NVARCHAR(30)     NOT NULL,
        RubricId             INT              NULL,
        ConceptKey           NVARCHAR(200)    NULL,
        ConceptType          NVARCHAR(50)     NULL,
        PayloadJson          NVARCHAR(MAX)    NULL,
        Status               NVARCHAR(30)     NOT NULL CONSTRAINT DF_AIEmbeddingQueue_Status DEFAULT (N'Pending'),
        Priority             INT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_Priority DEFAULT (100),
        AttemptCount         INT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_AttemptCount DEFAULT (0),
        MaxAttempts          INT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_MaxAttempts DEFAULT (3),
        NextRetryAtUtc       DATETIME2(3)     NULL,
        LockedUntilUtc       DATETIME2(3)     NULL,
        LockedBy             NVARCHAR(100)    NULL,
        LastError            NVARCHAR(2000)   NULL,
        CompletedAtUtc       DATETIME2(3)     NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingQueue_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate          DATETIME2(3)     NULL,
        DeletedDate          DATETIME2(3)     NULL,
        IsDeleted            BIT              NOT NULL CONSTRAINT DF_AIEmbeddingQueue_IsDeleted DEFAULT (0),
        CONSTRAINT PK_AIEmbeddingQueue PRIMARY KEY CLUSTERED (QueueId),
        CONSTRAINT FK_AIEmbeddingQueue_Job FOREIGN KEY (JobId)
            REFERENCES dbo.AIEmbeddingJob (JobId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingQueue_Pending' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingQueue'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingQueue_Pending
        ON dbo.AIEmbeddingQueue (Status, Priority, NextRetryAtUtc, CreatedDate)
        WHERE IsDeleted = 0 AND Status IN (N'Pending', N'Processing');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingQueue_JobId' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingQueue'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingQueue_JobId ON dbo.AIEmbeddingQueue (JobId, Status);
GO

/* ---------- AIEmbeddingAudit ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingAudit' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingAudit
    (
        AuditId              BIGINT           IDENTITY(1,1) NOT NULL,
        EntityType           NVARCHAR(50)     NOT NULL,
        EntityId             NVARCHAR(100)    NOT NULL,
        Action               NVARCHAR(50)     NOT NULL,
        OldStatus            NVARCHAR(30)     NULL,
        NewStatus            NVARCHAR(30)     NULL,
        JobId                UNIQUEIDENTIFIER NULL,
        QueueId              BIGINT           NULL,
        EmbeddingVersionId   UNIQUEIDENTIFIER NULL,
        ActorUserId          INT              NULL,
        CorrelationId        NVARCHAR(64)     NULL,
        DetailsJson          NVARCHAR(MAX)    NULL,
        CreatedDate          DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingAudit_CreatedDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIEmbeddingAudit PRIMARY KEY CLUSTERED (AuditId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingAudit_Entity' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingAudit'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingAudit_Entity
        ON dbo.AIEmbeddingAudit (EntityType, EntityId, CreatedDate DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIEmbeddingAudit_Job' AND object_id = OBJECT_ID(N'dbo.AIEmbeddingAudit'))
    CREATE NONCLUSTERED INDEX IX_AIEmbeddingAudit_Job ON dbo.AIEmbeddingAudit (JobId, CreatedDate DESC);
GO

/* ---------- AIEmbeddingStatistics ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'AIEmbeddingStatistics' AND schema_id = SCHEMA_ID(N'dbo'))
BEGIN
    CREATE TABLE dbo.AIEmbeddingStatistics
    (
        StatId                   BIGINT           IDENTITY(1,1) NOT NULL,
        EmbeddingVersionId       UNIQUEIDENTIFIER NOT NULL,
        StatDate                 DATE             NOT NULL,
        RubricEmbeddingCount     INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_RubricCount DEFAULT (0),
        ConceptEmbeddingCount    INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_ConceptCount DEFAULT (0),
        ActiveRubricEmbeddings   INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_ActiveRubric DEFAULT (0),
        ActiveConceptEmbeddings  INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_ActiveConcept DEFAULT (0),
        QueuePendingCount        INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueuePending DEFAULT (0),
        QueueProcessingCount     INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueueProcessing DEFAULT (0),
        QueueFailedCount         INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueueFailed DEFAULT (0),
        QueueDeadLetterCount     INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_QueueDeadLetter DEFAULT (0),
        JobsCompleted            INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_JobsCompleted DEFAULT (0),
        JobsFailed               INT              NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_JobsFailed DEFAULT (0),
        AvgProcessingMs          INT              NULL,
        CreatedDate              DATETIME2(3)     NOT NULL CONSTRAINT DF_AIEmbeddingStatistics_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate              DATETIME2(3)     NULL,
        CONSTRAINT PK_AIEmbeddingStatistics PRIMARY KEY CLUSTERED (StatId),
        CONSTRAINT FK_AIEmbeddingStatistics_Version FOREIGN KEY (EmbeddingVersionId)
            REFERENCES dbo.AIEmbeddingVersion (EmbeddingVersionId),
        CONSTRAINT UQ_AIEmbeddingStatistics_Version_Date UNIQUE (EmbeddingVersionId, StatDate)
    );
END
GO

PRINT 'AI V4 Phase 1 embedding infrastructure tables created (or already exist).';
GO
