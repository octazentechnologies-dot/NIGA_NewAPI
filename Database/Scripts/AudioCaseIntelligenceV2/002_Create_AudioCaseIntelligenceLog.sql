-- Per-stage audit log for V2 intelligence pipeline

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AudioCaseIntelligenceLog' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.AudioCaseIntelligenceLog
    (
        IntelligenceLogId    BIGINT IDENTITY(1,1) NOT NULL,
        AudioCaseSessionId   UNIQUEIDENTIFIER NOT NULL,
        CorrelationId        NVARCHAR(32) NULL,
        StageName            NVARCHAR(100) NOT NULL,
        Status               NVARCHAR(30) NOT NULL,
        Message              NVARCHAR(2000) NULL,
        DetailsJson          NVARCHAR(MAX) NULL,
        LatencyMs            INT NULL,
        EngineVersion        NVARCHAR(10) NOT NULL CONSTRAINT DF_AudioCaseIntelligenceLog_EngineVersion DEFAULT ('v2'),
        EnteredDate          DATETIME NOT NULL CONSTRAINT DF_AudioCaseIntelligenceLog_EnteredDate DEFAULT (GETUTCDATE()),
        CONSTRAINT PK_AudioCaseIntelligenceLog PRIMARY KEY (IntelligenceLogId),
        CONSTRAINT FK_AudioCaseIntelligenceLog_Session FOREIGN KEY (AudioCaseSessionId)
            REFERENCES dbo.AudioCaseSession (AudioCaseSessionId)
    );
END
GO

CREATE NONCLUSTERED INDEX IX_AudioCaseIntelligenceLog_SessionId
    ON dbo.AudioCaseIntelligenceLog (AudioCaseSessionId, EnteredDate DESC);
GO
