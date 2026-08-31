-- Phase 10: AI Monitoring Dashboard — daily KPI snapshots and enterprise audit log
-- Does NOT modify repertory master tables.

IF OBJECT_ID(N'dbo.AIMonitoringDailySnapshot', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIMonitoringDailySnapshot
    (
        SnapshotId                  BIGINT IDENTITY(1,1) NOT NULL,
        SnapshotDate                DATE NOT NULL,
        EngineVersion               NVARCHAR(20) NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Engine DEFAULT (N'all'),
        SessionsAnalyzed            INT NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Sessions DEFAULT (0),
        FeedbackCount               INT NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Feedback DEFAULT (0),
        PrecisionScore              DECIMAL(5,4) NULL,
        RecallScore                 DECIMAL(5,4) NULL,
        DoctorAcceptanceRate          DECIMAL(5,4) NULL,
        HallucinationCount            INT NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Hallucinations DEFAULT (0),
        HallucinationRate             DECIMAL(5,4) NULL,
        EmbeddingFreshnessHours       DECIMAL(10,2) NULL,
        RubricEmbeddingCoverage       DECIMAL(5,4) NULL,
        ConceptEmbeddingCoverage      DECIMAL(5,4) NULL,
        TranscriptCoverage            DECIMAL(5,4) NULL,
        AverageConfidence             DECIMAL(5,4) NULL,
        PrimaryRubricAccuracy         DECIMAL(5,4) NULL,
        F1Score                       DECIMAL(5,4) NULL,
        MetricsJson                   NVARCHAR(MAX) NULL,
        CalculatedDate                DATETIME2 NOT NULL CONSTRAINT DF_AIMonitoringDailySnapshot_Calculated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIMonitoringDailySnapshot PRIMARY KEY (SnapshotId),
        CONSTRAINT UQ_AIMonitoringDailySnapshot_DateEngine UNIQUE (SnapshotDate, EngineVersion)
    );
    PRINT 'Created AIMonitoringDailySnapshot';
END
GO

IF OBJECT_ID(N'dbo.AIMonitoringDailySnapshot', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMonitoringDailySnapshot_Date' AND object_id = OBJECT_ID(N'dbo.AIMonitoringDailySnapshot'))
BEGIN
    CREATE INDEX IX_AIMonitoringDailySnapshot_Date
        ON dbo.AIMonitoringDailySnapshot (SnapshotDate DESC, EngineVersion);
END
GO

IF OBJECT_ID(N'dbo.AIMonitoringAuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AIMonitoringAuditLog
    (
        AuditLogId          BIGINT IDENTITY(1,1) NOT NULL,
        EventType           NVARCHAR(50) NOT NULL,
        Operation           NVARCHAR(100) NOT NULL,
        ActorUserId         INT NULL,
        CorrelationId       UNIQUEIDENTIFIER NULL,
        DetailsJson         NVARCHAR(MAX) NULL,
        DurationMs          INT NULL,
        Success             BIT NOT NULL CONSTRAINT DF_AIMonitoringAuditLog_Success DEFAULT (1),
        ErrorMessage        NVARCHAR(2000) NULL,
        EnteredDate         DATETIME2 NOT NULL CONSTRAINT DF_AIMonitoringAuditLog_Entered DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AIMonitoringAuditLog PRIMARY KEY (AuditLogId)
    );
    PRINT 'Created AIMonitoringAuditLog';
END
GO

IF OBJECT_ID(N'dbo.AIMonitoringAuditLog', N'U') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AIMonitoringAuditLog_EnteredDate' AND object_id = OBJECT_ID(N'dbo.AIMonitoringAuditLog'))
BEGIN
    CREATE INDEX IX_AIMonitoringAuditLog_EnteredDate
        ON dbo.AIMonitoringAuditLog (EnteredDate DESC, EventType);
END
GO
